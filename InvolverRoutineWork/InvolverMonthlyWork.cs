using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using InvolverRoutineWork.Data;
using Microsoft.EntityFrameworkCore;

namespace InvolverMonthlyWork
{
    public class InvolverMonthlyWork
    {
        private readonly DatabaseContext _context;

        public InvolverMonthlyWork(DatabaseContext context)
        {
            _context = context;
        }

        [FunctionName("InvolverMonthlyWork")]
        public async Task Run([TimerTrigger("0 0 0 1 * *")] TimerInfo myTimer, ILogger log)
        {
            // For use ef core to excute raw SQL
            await _context.Database.ExecuteSqlRawAsync("USE [Involver]");

            var text = "UPDATE [dbo].[Article] " +
                        "SET [MonthlyCoins] = 0;";

            int rows = await _context.Database.ExecuteSqlRawAsync(text);

            log.LogInformation($"Update article monthly coins, {rows} rows were updated");

            text = "UPDATE [dbo].[Involving] " +
                        "SET [MonthlyValue] = 0;";

            rows = await _context.Database.ExecuteSqlRawAsync(text);

            log.LogInformation($"Update involving monthly value, {rows} rows were updated");

            text = "UPDATE [dbo].[Novel] " +
                        "SET [MonthlyCoins] = 0;";

            rows = await _context.Database.ExecuteSqlRawAsync(text);

            log.LogInformation($"Update novel monthly coins, {rows} rows were updated");

            text = "UPDATE [dbo].[Profile] " +
                        "SET [MonthlyCoins] = 0;";

            rows = await _context.Database.ExecuteSqlRawAsync(text);

            log.LogInformation($"Update profile monthly coins, {rows} rows were updated");

            text = @"DECLARE @monthAgo dateTime
                        SET @monthAgo = DATEADD(month, -1, GETDATE())
                        DELETE FROM [Involver].[dbo].[Notifications] WHERE [CreatedDate] < @monthAgo";

            rows = await _context.Database.ExecuteSqlRawAsync(text);

            log.LogInformation($"Delete notifications that pass a month ago, {rows} rows were updated");
        }
    }
}
