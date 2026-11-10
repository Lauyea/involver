using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using DataAccess.Data;

using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvolverMonthlyWork
{
    /// <summary>
    /// 每月排程：重置月累計的 In 幣與參與值，並清除一個月前的通知。
    /// </summary>
    public class InvolverMonthlyWork
    {
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

            await RunStepAsync("Reset profile monthly coins", failedSteps, log, async () =>
            {
                int rows = await _context.Database.ExecuteSqlRawAsync("UPDATE [dbo].[Profiles] SET [MonthlyCoins] = 0;");
                log.LogInformation($"Update profile monthly coins, {rows} rows were updated");
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
