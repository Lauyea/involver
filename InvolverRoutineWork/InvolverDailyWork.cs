using System;
using System.Threading.Tasks;

using DataAccess.Data;

using InvolverRoutineWork.Common;

using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InvolverDailyWork
{
    public class InvolverDailyWork
    {
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

            // Reset Daily Views for Articles and Novels
            var resetViewsSql = "UPDATE [dbo].[Articles] SET [DailyView] = 0; UPDATE [dbo].[Novels] SET [DailyView] = 0;";
            int resetViewsRows = await _context.Database.ExecuteSqlRawAsync(resetViewsSql);
            log.LogInformation($"Reset daily views, {resetViewsRows} rows were updated (includes both articles and novels).");

            // Reset Daily Login Mission
            var resetMissionSql = "UPDATE [dbo].[Missions] SET [DailyLogin] = 'False';";
            int missionRows = await _context.Database.ExecuteSqlRawAsync(resetMissionSql);
            log.LogInformation($"Update missions, {missionRows} rows were updated");

            // Delete view records older than 30 days to keep the table clean
            var daysAgo = DateTime.Now.AddDays(Parameters.ViewRecordDays);
            var deleteViewsSql = $"DELETE FROM [dbo].[Views] WHERE [CreateTime] < '{daysAgo:yyyy-MM-dd HH:mm:ss}'";
            int deletedViewRows = await _context.Database.ExecuteSqlRawAsync(deleteViewsSql);
            log.LogInformation($"Delete old view records, {deletedViewRows} rows were deleted.");
        }
    }
}