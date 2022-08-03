using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace InvolverDailyWork
{
    public static class InvolverDailyWork
    {
        [FunctionName("InvolverDailyWork")]
        public static async Task Run([TimerTrigger("0 0 0 * * *")]TimerInfo myTimer, ILogger log)
        {
            // Get the connection string from app settings and use it to create a connection.
            var str = Environment.GetEnvironmentVariable("sqldb_connection");
            using (SqlConnection conn = new SqlConnection(str))
            {
                conn.Open();
                var UseTable = "USE [InvolverDatabase];";
                using (SqlCommand cmd = new SqlCommand(UseTable, conn))
                {
                    var rows = await cmd.ExecuteNonQueryAsync();
                }

                var text = "UPDATE [dbo].[Missions] " +
                        "SET [DailyLogin] = 'False';";

                using (SqlCommand cmd = new SqlCommand(text, conn))
                {
                    // Execute the command and log the # rows affected.
                    var rows = await cmd.ExecuteNonQueryAsync();
                    log.LogInformation($"{rows} rows were updated");
                }

                text = "DELETE FROM [dbo].[ViewIps]";

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
