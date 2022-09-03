using System;
using System.Data.SqlClient;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace InvolverWeeklyWork
{
    public static class InvolverWeeklyWork
    {
        [FunctionName("InvolverWeeklyWork")]
        public static async Task Run([TimerTrigger("0 0 0 * * 1")] TimerInfo myTimer, ILogger log)
        {
            // Get the connection string from app settings and use it to create a connection.
            var str = Environment.GetEnvironmentVariable("sqldb_connection");
            using (SqlConnection conn = new SqlConnection(str))
            {
                conn.Open();
                var UseTable = "USE [Involver];";
                using (SqlCommand cmd = new SqlCommand(UseTable, conn))
                {
                    var rows = await cmd.ExecuteNonQueryAsync();
                }

                var text = "UPDATE [dbo].[Missions] " +
                        "SET [WatchArticle] = 'False'" +
                        ",[Vote] = 'False'" +
                        ",[LeaveComment] = 'False'" +
                        ",[ViewAnnouncement] = 'False'" +
                        ",[ShareCreation] = 'False'" +
                        ",[BeAgreed] = 'False'" +
                        ",[CompleteOtherMissions] = 'False';";

                using (SqlCommand cmd = new SqlCommand(text, conn))
                {
                    // Execute the command and log the # rows affected.
                    var rows = await cmd.ExecuteNonQueryAsync();
                    log.LogInformation($"{rows} rows were updated");
                }
            }
        }
    }
}
