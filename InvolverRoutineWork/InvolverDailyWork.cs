using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DataAccess.Data;
using DataAccess.Services;

using InvolverRoutineWork.Common;

using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvolverDailyWork
{
    public class InvolverDailyWork
    {
        /// <summary>
        /// 每批刪除的觀看紀錄筆數。分批刪除可避免單一 DELETE 鎖太多資料列或超過指令逾時。
        /// </summary>
        private const int DeleteBatchSize = 5000;

        private readonly ApplicationDbContext _context;

        public InvolverDailyWork(ApplicationDbContext context)
        {
            _context = context;
        }

        [Function("InvolverDailyWork")]
        public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer, ILogger log)
        {
            // For use ef core to excute raw SQL
            await _context.Database.ExecuteSqlRawAsync("USE [Involver]");

            // 刪除大量舊資料可能超過預設 30 秒
            _context.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            // 每個步驟各自執行，其中一步失敗不會讓後面的步驟被跳過
            var failedSteps = new List<string>();

            // 快照的瀏覽數取自 DailyView，必須在歸零之前寫入
            const string snapshotStep = "Write daily snapshot";
            await RunStepAsync(snapshotStep, failedSteps, log, async () =>
            {
                var date = SnapshotWindow.PreviousDay(DateTime.Now);
                var result = await new DailySnapshotService(_context).WriteSnapshotAsync(date);
                log.LogInformation($"Write daily snapshot of {date:yyyy-MM-dd}, {result.ContentCount} content rows were inserted.");
            });

            if (failedSteps.Contains(snapshotStep))
            {
                // 快照失敗時不歸零：今天的瀏覽數會併入明天，但不會遺失
                failedSteps.Add("Reset daily views (skipped)");
                log.LogWarning("Daily snapshot failed, daily views are not reset.");
            }
            else
            {
                await RunStepAsync("Reset daily views", failedSteps, log, async () =>
                {
                    var resetViewsSql = "UPDATE [dbo].[Articles] SET [DailyView] = 0; UPDATE [dbo].[Novels] SET [DailyView] = 0;";
                    int resetViewsRows = await _context.Database.ExecuteSqlRawAsync(resetViewsSql);
                    log.LogInformation($"Reset daily views, {resetViewsRows} rows were updated (includes both articles and novels).");
                });
            }

            await RunStepAsync("Reset daily login mission", failedSteps, log, async () =>
            {
                var resetMissionSql = "UPDATE [dbo].[Missions] SET [DailyLogin] = 'False';";
                int missionRows = await _context.Database.ExecuteSqlRawAsync(resetMissionSql);
                log.LogInformation($"Update missions, {missionRows} rows were updated");
            });

            await RunStepAsync("Delete old view records", failedSteps, log, async () =>
            {
                int deletedViewRows = await DeleteOldViewRecordsAsync(DateTime.Now.AddDays(Parameters.ViewRecordDays));
                log.LogInformation($"Delete old view records, {deletedViewRows} rows were deleted.");
            });

            await RunStepAsync("Delete old recommendation impressions", failedSteps, log, async () =>
            {
                var cutoff = DateTime.UtcNow.AddDays(-DataAccess.Common.Parameters.RecommendationImpressionRetentionDays);
                int deletedImpressionRows = await DeleteOldImpressionsAsync(cutoff);
                log.LogInformation($"Delete old recommendation impressions, {deletedImpressionRows} rows were deleted.");
            });

            if (failedSteps.Count > 0)
            {
                // 讓這次執行在 Azure 上標記為失敗，才看得到問題
                throw new InvalidOperationException($"InvolverDailyWork failed steps: {string.Join(", ", failedSteps)}");
            }
        }

        /// <summary>
        /// 分批刪除早於指定時間的觀看紀錄，直到沒有可刪的資料為止。
        /// </summary>
        /// <param name="cutoff">刪除早於此時間的紀錄</param>
        /// <returns>總共刪除的筆數</returns>
        private async Task<int> DeleteOldViewRecordsAsync(DateTime cutoff)
        {
            int totalDeleted = 0;
            int deleted;

            do
            {
                // 插值字串會轉成 SQL 參數，不會有 SQL injection 或日期格式問題
                deleted = await _context.Database.ExecuteSqlAsync(
                    $"DELETE TOP ({DeleteBatchSize}) FROM [dbo].[Views] WHERE [CreateTime] < {cutoff}");
                totalDeleted += deleted;
            }
            while (deleted == DeleteBatchSize);

            return totalDeleted;
        }

        /// <summary>
        /// 分批刪除早於指定時間的 Feed 曝光紀錄，直到沒有可刪的資料為止。
        /// </summary>
        /// <param name="cutoff">刪除早於此時間（UTC）的紀錄</param>
        /// <returns>總共刪除的筆數</returns>
        private async Task<int> DeleteOldImpressionsAsync(DateTime cutoff)
        {
            int totalDeleted = 0;
            int deleted;

            do
            {
                deleted = await _context.Database.ExecuteSqlAsync(
                    $"DELETE TOP ({DeleteBatchSize}) FROM [dbo].[RecommendationImpressions] WHERE [ShownAt] < {cutoff}");
                totalDeleted += deleted;
            }
            while (deleted == DeleteBatchSize);

            return totalDeleted;
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
                log.LogError(ex, $"InvolverDailyWork step failed: {stepName}");
            }
        }
    }
}
