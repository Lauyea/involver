using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using InvolverRoutineWork.Data;
using InvolverRoutineWork.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using Data.Models.Database;

namespace InvolverDailyWork
{
    public class InvolverDailyWork
    {
        private readonly DatabaseContext _context;

        public InvolverDailyWork(DatabaseContext context)
        {
            _context = context;
        }

        [FunctionName("InvolverDailyWork")]
        public async Task Run([TimerTrigger("0 0 0 * * *")]TimerInfo myTimer, ILogger log)

        {
            var novels = await _context.Novels.ToListAsync();

            SetViewRecord(novels);

            var article = await _context.Articles.ToListAsync();

            SetViewRecord(article);

            await _context.SaveChangesAsync();

            // For use ef core to excute raw SQL
            await _context.Database.ExecuteSqlRawAsync("USE [Involver]");

            var text = "UPDATE [dbo].[Missions] " +
                        "SET [DailyLogin] = 'False';";

            int rows = await _context.Database.ExecuteSqlRawAsync(text);

            log.LogInformation($"Update missions, {rows} rows were updated");

            text = "DELETE FROM [dbo].[ViewIps]";

            rows = await _context.Database.ExecuteSqlRawAsync(text);

            log.LogInformation($"Delete view IPs, {rows} rows were updated");
        }

        private static void SetViewRecord(List<Novel> novels)
        {
            foreach (var item in novels)
            {
                var views = item.DailyView;

                ViewRecord viewRecord = new()
                {
                    Date = DateTime.Now.AddDays(-1).Date,
                    ViewCount = views
                };

                List<ViewRecord> records = null;

                try
                {
                    records = JsonSerializer.Deserialize<List<ViewRecord>>(item.ViewRecordJson);
                }
                catch
                {
                    records = new List<ViewRecord>();
                }

                if (records == null)
                {
                    records = new List<ViewRecord>();
                }

                var halfMonthAgo = DateTime.Now.AddDays(-15);

                records = records.Where(r => r.Date > halfMonthAgo).ToList();

                records.Add(viewRecord);

                string recordJson = JsonSerializer.Serialize(records);

                item.ViewRecordJson = recordJson;

                item.DailyView = 0;
            }
        }

        private static void SetViewRecord(List<Article> articles)
        {
            foreach (var item in articles)
            {
                var views = item.DailyView;

                ViewRecord viewRecord = new()
                {
                    Date = DateTime.Now.AddDays(-1).Date,
                    ViewCount = views
                };

                List<ViewRecord> records = null;

                try
                {
                    records = JsonSerializer.Deserialize<List<ViewRecord>>(item.ViewRecordJson);
                }
                catch
                {
                    records = new List<ViewRecord>();
                }

                if (records == null)
                {
                    records = new List<ViewRecord>();
                }

                var halfMonthAgo = DateTime.Now.AddDays(-15);

                records = records.Where(r => r.Date > halfMonthAgo).ToList();

                records.Add(viewRecord);

                string recordJson = JsonSerializer.Serialize(records);

                item.ViewRecordJson = recordJson;

                item.DailyView = 0;
            }
        }
    }
}
