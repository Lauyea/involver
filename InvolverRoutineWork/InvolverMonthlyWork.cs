using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DataAccess.Data;
using DataAccess.Models.StatisticalData;

using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvolverMonthlyWork
{
    /// <summary>
    /// 每月排程：彙總上個月的 In 幣月結、結算創作者月收入、重置月累計的 In 幣與參與值，並清除一個月前的通知。
    /// </summary>
    public class InvolverMonthlyWork
    {
        /// <summary>
        /// 台灣時間與 UTC 的時差。台灣沒有日光節約時間，固定 +8 小時
        /// </summary>
        private const int TaiwanUtcOffsetHours = 8;

        private readonly ApplicationDbContext _context;

        /// <summary>
        /// 建立每月排程。
        /// </summary>
        /// <param name="context">資料庫內容</param>
        public InvolverMonthlyWork(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 每月 1 日 00:00 執行。每個步驟各自執行，其中一步失敗不會跳過後面的步驟；
        /// 最後若有任何步驟失敗則丟出例外，讓 Azure 將此次執行標記為失敗。
        /// </summary>
        /// <param name="myTimer">計時器資訊</param>
        /// <param name="log">記錄器</param>
        [Function("InvolverMonthlyWork")]
        public async Task Run([TimerTrigger("0 0 0 1 * *")] TimerInfo myTimer, ILogger log)
        {
            // For use ef core to excute raw SQL
            await _context.Database.ExecuteSqlRawAsync("USE [Involver]");

            // 更新整張表可能超過預設 30 秒
            _context.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            var failedSteps = new List<string>();

            // 先彙總上個月的流水帳，再結算創作者收入；結算紀錄的時間落在本月，不影響上個月的月結
            await RunStepAsync("Summarize coin monthly balances", failedSteps, log, async () =>
            {
                var (month, startUtc, endUtc) = GetPreviousTaiwanMonth(DateTime.UtcNow);
                int rows = await SummarizeMonthlyBalancesAsync(month, startUtc, endUtc);
                log.LogInformation($"Summarize coin monthly balances of {month:yyyy-MM}, {rows} rows were inserted");
            });

            await RunStepAsync("Reset article monthly coins", failedSteps, log, async () =>
            {
                int rows = await _context.Database.ExecuteSqlRawAsync("UPDATE [dbo].[Articles] SET [MonthlyCoins] = 0;");
                log.LogInformation($"Update article monthly coins, {rows} rows were updated");
            });

            await RunStepAsync("Reset involving monthly value", failedSteps, log, async () =>
            {
                int rows = await _context.Database.ExecuteSqlRawAsync("UPDATE [dbo].[Involvings] SET [MonthlyValue] = 0;");
                log.LogInformation($"Update involving monthly value, {rows} rows were updated");
            });

            await RunStepAsync("Reset novel monthly coins", failedSteps, log, async () =>
            {
                int rows = await _context.Database.ExecuteSqlRawAsync("UPDATE [dbo].[Novels] SET [MonthlyCoins] = 0;");
                log.LogInformation($"Update novel monthly coins, {rows} rows were updated");
            });

            await RunStepAsync("Settle profile monthly coins", failedSteps, log, async () =>
            {
                int rows = await SettleCreatorIncomeAsync();
                log.LogInformation($"Settle profile monthly coins, {rows} rows were updated");
            });

            await RunStepAsync("Delete old notifications", failedSteps, log, async () =>
            {
                int rows = await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM [dbo].[Notifications] WHERE [CreatedDate] < DATEADD(month, -1, GETDATE());");
                log.LogInformation($"Delete notifications that pass a month ago, {rows} rows were deleted");
            });

            if (failedSteps.Count > 0)
            {
                // 讓這次執行在 Azure 上標記為失敗，才看得到問題
                throw new InvalidOperationException($"InvolverMonthlyWork failed steps: {string.Join(", ", failedSteps)}");
            }
        }

        /// <summary>
        /// 取得「上個月」的台灣時間月份，以及對應的 UTC 時間範圍。
        /// 不論排程以 UTC 或台灣時間在每月 1 日 00:00 觸發，算出的都是剛結束的那個月。
        /// </summary>
        /// <param name="utcNow">目前的 UTC 時間</param>
        /// <returns>月份（該月 1 日）、起始時間（含）、結束時間（不含），時間皆為 UTC</returns>
        public static (DateOnly Month, DateTime StartUtc, DateTime EndUtc) GetPreviousTaiwanMonth(DateTime utcNow)
        {
            DateTime taiwanNow = utcNow.AddHours(TaiwanUtcOffsetHours);
            var thisMonth = new DateTime(taiwanNow.Year, taiwanNow.Month, 1);
            DateTime previousMonth = thisMonth.AddMonths(-1);

            return (
                DateOnly.FromDateTime(previousMonth),
                previousMonth.AddHours(-TaiwanUtcOffsetHours),
                thisMonth.AddHours(-TaiwanUtcOffsetHours));
        }

        /// <summary>
        /// 從流水帳彙總指定月份每位使用者、每個幣別的月結。會先刪除該月既有的月結，重跑結果相同。
        /// 期末餘額取當月最後一筆紀錄的交易後餘額，期初餘額再由期末減去當月淨異動推回。
        /// </summary>
        /// <param name="month">月份（該月 1 日）</param>
        /// <param name="startUtc">起始時間（含，UTC）</param>
        /// <param name="endUtc">結束時間（不含，UTC）</param>
        /// <returns>寫入的月結筆數</returns>
        private async Task<int> SummarizeMonthlyBalancesAsync(DateOnly month, DateTime startUtc, DateTime endUtc)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            await _context.Database.ExecuteSqlInterpolatedAsync(
                $"DELETE FROM [dbo].[CoinMonthlyBalances] WHERE [Month] = {month};");

            int rows = await _context.Database.ExecuteSqlInterpolatedAsync($"""
                WITH [Monthly] AS (
                    SELECT [ProfileId], [CoinType], [Amount], [BalanceAfter],
                        ROW_NUMBER() OVER (PARTITION BY [ProfileId], [CoinType] ORDER BY [Id] DESC) AS [Rn]
                    FROM [dbo].[CoinTransactions]
                    WHERE [CreatedAt] >= {startUtc} AND [CreatedAt] < {endUtc}
                )
                INSERT INTO [dbo].[CoinMonthlyBalances]
                    ([Month], [ProfileId], [CoinType], [OpeningBalance], [Credits], [Debits], [ClosingBalance], [TransactionCount], [CreatedAt])
                SELECT {month}, [ProfileId], [CoinType],
                    MAX(CASE WHEN [Rn] = 1 THEN [BalanceAfter] END) - SUM([Amount]),
                    SUM(CASE WHEN [Amount] > 0 THEN [Amount] ELSE 0 END),
                    SUM(CASE WHEN [Amount] < 0 THEN -[Amount] ELSE 0 END),
                    MAX(CASE WHEN [Rn] = 1 THEN [BalanceAfter] END),
                    COUNT(*),
                    SYSUTCDATETIME()
                FROM [Monthly]
                GROUP BY [ProfileId], [CoinType];
                """);

            await transaction.CommitAsync();
            return rows;
        }

        /// <summary>
        /// 結算創作者月收入：把 Profiles.MonthlyCoins 歸零，並為每位有收入的創作者寫入一筆月結歸零的流水帳。
        /// 歸零與寫入在同一個 SQL 批次與交易中完成，歸零當下才進來的收入不會被漏記。
        /// </summary>
        /// <returns>歸零的使用者數</returns>
        private async Task<int> SettleCreatorIncomeAsync()
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();

            var correlationId = Guid.NewGuid();
            byte coinType = (byte)CoinType.CreatorIncome;
            short transactionType = (short)CoinTransactionType.MonthlySettlement;

            // CoinTransactions 有外鍵，不能直接當 OUTPUT INTO 的目標，先輸出到資料表變數
            await _context.Database.ExecuteSqlInterpolatedAsync($"""
                DECLARE @Settled TABLE ([ProfileId] nvarchar(450) NOT NULL, [Amount] money NOT NULL);

                UPDATE [dbo].[Profiles]
                SET [MonthlyCoins] = 0
                OUTPUT deleted.[ProfileID], deleted.[MonthlyCoins] INTO @Settled
                WHERE [MonthlyCoins] <> 0;

                INSERT INTO [dbo].[CoinTransactions]
                    ([ProfileId], [CoinType], [TransactionType], [Amount], [BalanceAfter], [CorrelationId], [Note], [CreatedAt])
                SELECT [ProfileId], {coinType}, {transactionType}, -[Amount], 0, {correlationId}, N'創作者月收入月結歸零', SYSUTCDATETIME()
                FROM @Settled;
                """);

            int settled = await _context.CoinTransactions
                .CountAsync(t => t.CorrelationId == correlationId);

            await transaction.CommitAsync();
            return settled;
        }

        /// <summary>
        /// 執行單一步驟，失敗時記錄錯誤並記下步驟名稱，不中斷後續步驟。
        /// </summary>
        /// <param name="stepName">步驟名稱</param>
        /// <param name="failedSteps">失敗步驟清單</param>
        /// <param name="log">記錄器</param>
        /// <param name="step">步驟內容</param>
        private static async Task RunStepAsync(string stepName, List<string> failedSteps, ILogger log, Func<Task> step)
        {
            try
            {
                await step();
            }
            catch (Exception ex)
            {
                failedSteps.Add(stepName);
                log.LogError(ex, $"InvolverMonthlyWork step failed: {stepName}");
            }
        }
    }
}
