using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using InvolverRoutineWork.Data;
using Microsoft.EntityFrameworkCore;

namespace InvolverWeeklyWork
{
    public class InvolverWeeklyWork
    {
        private readonly DatabaseContext _context;

        public InvolverWeeklyWork(DatabaseContext context)
        {
            _context = context;
        }

        [FunctionName("InvolverWeeklyWork")]
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
