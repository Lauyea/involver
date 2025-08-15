using DataAccess.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace InvolverWeeklyWork
{
    public class InvolverWeeklyWork
    {
        private readonly ApplicationDbContext _context;

        public InvolverWeeklyWork(ApplicationDbContext context)
        {
            _context = context;
        }

        [Function("InvolverWeeklyWork")]
        public async Task Run([TimerTrigger("0 0 0 * * 1")] TimerInfo myTimer, ILogger log)
        {
            // For use ef core to excute raw SQL
            await _context.Database.ExecuteSqlRawAsync("USE [Involver]");

            var text = "UPDATE [dbo].[Missions] " +
                        "SET [WatchArticle] = 'False'" +
                        ",[Vote] = 'False'" +
                        ",[LeaveComment] = 'False'" +
                        ",[ViewAnnouncement] = 'False'" +
                        ",[ShareCreation] = 'False'" +
                        ",[BeAgreed] = 'False'" +
                        ",[CompleteOtherMissions] = 'False';";

            int rows = await _context.Database.ExecuteSqlRawAsync(text);

            log.LogInformation($"Reset weekly missions, {rows} rows were updated");
        }
    }
}
