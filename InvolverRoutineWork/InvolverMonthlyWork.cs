using System;
using System.Data.SqlClient;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace InvolverMonthlyWork
{
    public static class InvolverMonthlyWork
    {
        [FunctionName("InvolverMonthlyWork")]
        public static async Task Run([TimerTrigger("0 0 0 1 * *")] TimerInfo myTimer, ILogger log)
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

                var text = "UPDATE [dbo].[Article] " +
                        "SET [MonthlyCoins] = 0;";

                using (SqlCommand cmd = new SqlCommand(text, conn))
                {
                    // Execute the command and log the # rows affected.
                    var rows = await cmd.ExecuteNonQueryAsync();
                    log.LogInformation($"{rows} rows were updated");
                }

                text = "UPDATE [dbo].[Involving] " +
                        "SET [MonthlyValue] = 0;";

                using (SqlCommand cmd = new SqlCommand(text, conn))
                {
                    // Execute the command and log the # rows affected.
                    var rows = await cmd.ExecuteNonQueryAsync();
                    log.LogInformation($"{rows} rows were updated");
                }

                text = "UPDATE [dbo].[Novel] " +
                        "SET [MonthlyCoins] = 0;";

                using (SqlCommand cmd = new SqlCommand(text, conn))
                {
                    // Execute the command and log the # rows affected.
                    var rows = await cmd.ExecuteNonQueryAsync();
                    log.LogInformation($"{rows} rows were updated");
                }

                text = "UPDATE [dbo].[Profile] " +
                        "SET [MonthlyCoins] = 0;";

                using (SqlCommand cmd = new SqlCommand(text, conn))
                {
                    // Execute the command and log the # rows affected.
                    var rows = await cmd.ExecuteNonQueryAsync();
                    log.LogInformation($"{rows} rows were updated");
                }

                text = @"DECLARE @monthAgo dateTime
                        SET @monthAgo = DATEADD(month, -1, GETDATE())
                        DELETE FROM [Involver].[dbo].[Notifications] WHERE [CreatedDate] < @monthAgo";

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
