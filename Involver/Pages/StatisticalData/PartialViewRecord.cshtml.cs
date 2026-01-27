using System.Text.Json;

using DataAccess.Common;
using DataAccess.Data;

using Involver.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.StatisticalData
{
    [AllowAnonymous]
    public class PartialViewRecordModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PartialViewRecordModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public string DateArrJson { get; set; }
        public string ViewCountArrJson { get; set; }
        public string CumulativeViewCountArrJson { get; set; }
        public string Title { get; set; }

        public async Task<IActionResult> OnGetViewRecordAsync(string type, int id)
        {
            var daysAgo = DateTime.UtcNow.AddDays(Parameters.ViewRecordDays);
            List<DailyView> dailyViews;
            long totalViews = 0;

            if (type == "novel")
            {
                var novel = await _context.Novels.FindAsync(id);
                if (novel == null) return NotFound();
                Title = novel.Title;
                totalViews = novel.TotalViews;

                dailyViews = await _context.Views
                    .Where(v => v.NovelId == id && v.CreateTime >= daysAgo)
                    .GroupBy(v => v.CreateTime.Date)
                    .Select(g => new DailyView { Date = g.Key, Count = g.Count() })
                    .OrderBy(dv => dv.Date)
                    .ToListAsync();
            }
            else if (type == "article")
            {
                var article = await _context.Articles.FindAsync(id);
                if (article == null) return NotFound();
                Title = article.Title;
                totalViews = article.TotalViews;

                dailyViews = await _context.Views
                    .Where(v => v.ArticleId == id && v.CreateTime >= daysAgo)
                    .GroupBy(v => v.CreateTime.Date)
                    .Select(g => new DailyView { Date = g.Key, Count = g.Count() })
                    .OrderBy(dv => dv.Date)
                    .ToListAsync();
            }
            else
            {
                return BadRequest("Invalid type specified.");
            }

            var allDates = Enumerable.Range(0, 30)
                .Select(i => DateTime.UtcNow.AddDays((Parameters.ViewRecordDays + 1) + i).Date);

            var viewsWithAllDates = from date in allDates
                                    join view in dailyViews on date equals view.Date into gj
                                    from subView in gj.DefaultIfEmpty()
                                    select new
                                    {
                                        Date = date,
                                        Count = subView?.Count ?? 0
                                    };

            var dateArr = viewsWithAllDates.Select(r => r.Date.ToString("MM/dd")).ToArray();
            DateArrJson = JsonSerializer.Serialize(dateArr);

            var viewCountArr = viewsWithAllDates.Select(r => r.Count).ToArray();
            ViewCountArrJson = JsonSerializer.Serialize(viewCountArr);

            var cumulativeViewCountArr = new long[viewCountArr.Length];
            if (cumulativeViewCountArr.Length > 0)
            {
                cumulativeViewCountArr[cumulativeViewCountArr.Length - 1] = totalViews;
                for (int i = cumulativeViewCountArr.Length - 2; i >= 0; i--)
                {
                    cumulativeViewCountArr[i] = cumulativeViewCountArr[i + 1] - viewCountArr[i + 1];
                }
            }
            CumulativeViewCountArrJson = JsonSerializer.Serialize(cumulativeViewCountArr);

            return Page();
        }

        private class DailyView
        {
            public DateTime Date { get; set; }
            public int Count { get; set; }
        }
    }
}