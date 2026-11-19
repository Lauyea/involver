using System;
using System.Threading.Tasks;

using DataAccess.Data;
using DataAccess.Services.Recommendations;

using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvolverRecommendationWork
{
    /// <summary>
    /// Issue #238：每小時預先算好會員的推薦，寫入 UserRecommendations。首頁只讀這張表。
    /// 每次執行都讀取目前啟用的推薦設定版本。
    /// </summary>
    public class InvolverRecommendationWork
    {
        private readonly ApplicationDbContext _context;

        public InvolverRecommendationWork(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 每小時第 15 分執行，避開整點的每日、每週、每月排程
        /// </summary>
        /// <param name="myTimer">計時器資訊</param>
        /// <param name="log">記錄器</param>
        [Function("InvolverRecommendationWork")]
        public async Task Run([TimerTrigger("0 15 * * * *")] TimerInfo myTimer, ILogger log)
        {
            // For use ef core to excute raw SQL
            await _context.Database.ExecuteSqlRawAsync("USE [Involver]");
            _context.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));

            var result = await new RecommendationEngine(_context).RunAsync(DateTime.Now);
            log.LogInformation(
                $"Recommendations computed with setting version {result.SettingVersion?.ToString() ?? "default"}: {result.Rows} rows for {result.Users} users.");
        }
    }
}
