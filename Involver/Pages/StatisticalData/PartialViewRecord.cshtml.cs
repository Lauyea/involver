using Involver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace Involver.Pages.StatisticalData
{
    [AllowAnonymous]
    public class PartialViewRecordModel : PageModel
    {
        public string DateArrJson { get; set; }

        public string ViewCountArrJson { get; set; }

        public void OnGet(string json)
        {
            List<ViewRecord> records;

            try
            {
                records = JsonSerializer.Deserialize<List<ViewRecord>>(json);
            }
            catch
            {
                records = new();
            }

            records = records.OrderBy(r => r.Date).ToList();

            var dateArr = records.Select(r => r.Date.ToString("MM/dd")).ToArray();

            DateArrJson = JsonSerializer.Serialize(dateArr);

            var viewCountArr = records.Select(r => r.ViewCount.ToString()).ToArray();

            ViewCountArrJson = JsonSerializer.Serialize(viewCountArr);
        }
    }
}
