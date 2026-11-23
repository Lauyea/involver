using System;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;

using Involver.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class ViewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ViewsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Views/GetArticleViews/5
        [HttpGet("GetArticleViews/{id}")]
        public async Task<IActionResult> GetArticleViews(int id)
        {
            var daysAgo = DateTime.Now.Date.AddDays(Parameters.ViewRecordDays);

            var views = await _context.Views
                .Where(v => v.ArticleId == id && v.CreateTime >= daysAgo)
                .GroupBy(v => v.CreateTime.Date)
                .Select(g => new { Date = g.Key, Views = g.Count() })
                .OrderBy(g => g.Date)
                .ToListAsync();

            return Ok(views);
        }

        // GET: api/Views/GetNovelViews/5
        [HttpGet("GetNovelViews/{id}")]
        public async Task<IActionResult> GetNovelViews(int id)
        {
            var daysAgo = DateTime.Now.Date.AddDays(Parameters.ViewRecordDays);

            var views = await _context.Views
                .Where(v => v.NovelId == id && v.CreateTime >= daysAgo)
                .GroupBy(v => v.CreateTime.Date)
                .Select(g => new { Date = g.Key, Views = g.Count() })
                .OrderBy(g => g.Date)
                .ToListAsync();

            return Ok(views);
        }

        // POST: api/Views/GetBatchArticleViews
        [HttpPost("GetBatchArticleViews")]
        [IgnoreAntiforgeryToken] // 唯讀查詢，只是用 POST 帶 ID 陣列；列表頁的訪客不一定有 token
        public async Task<IActionResult> GetBatchArticleViews([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return BadRequest();
            }

            var daysAgo = DateTime.Now.Date.AddDays(Parameters.ViewRecordDays);

            var views = await _context.Views
                .Where(v => v.ArticleId.HasValue && ids.Contains(v.ArticleId.Value) && v.CreateTime >= daysAgo)
                .GroupBy(v => new { v.ArticleId, v.CreateTime.Date })
                .Select(g => new { g.Key.ArticleId, Date = g.Key.Date, Views = g.Count() })
                .ToListAsync();

            var result = views
                .GroupBy(v => v.ArticleId.Value)
                .ToDictionary(g => g.Key, g => g.Select(v => new { v.Date, v.Views }).OrderBy(v => v.Date));

            return Ok(result);
        }

        // POST: api/Views/GetBatchNovelViews
        [HttpPost("GetBatchNovelViews")]
        [IgnoreAntiforgeryToken] // 唯讀查詢，只是用 POST 帶 ID 陣列；列表頁的訪客不一定有 token
        public async Task<IActionResult> GetBatchNovelViews([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return BadRequest();
            }

            var daysAgo = DateTime.Now.Date.AddDays(Parameters.ViewRecordDays);

            var views = await _context.Views
                .Where(v => v.NovelId.HasValue && ids.Contains(v.NovelId.Value) && v.CreateTime >= daysAgo)
                .GroupBy(v => new { v.NovelId, v.CreateTime.Date })
                .Select(g => new { g.Key.NovelId, Date = g.Key.Date, Views = g.Count() })
                .ToListAsync();

            var result = views
                .GroupBy(v => v.NovelId.Value)
                .ToDictionary(g => g.Key, g => g.Select(v => new { v.Date, v.Views }).OrderBy(v => v.Date));

            return Ok(result);
        }

        /// <summary>
        /// 觀看紀錄彈窗的資料：過去 30 天的每日觀看數與累積觀看數（取代原本的 /StatisticalData/PartialViewRecord）
        /// </summary>
        /// <param name="type">novel 或 article</param>
        /// <param name="id">作品或文章 ID</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>標題、日期、每日與累積觀看數</returns>
        // GET: api/Views/Daily?type=novel&id=5
        [HttpGet("Daily")]
        public async Task<IActionResult> GetDailyViewsAsync([FromQuery] string type, [FromQuery] int id, CancellationToken cancellationToken)
        {
            var today = DateTime.Now.Date;
            var days = -Parameters.ViewRecordDays;
            var start = today.AddDays(1 - days);

            string title;
            long totalViews;
            IQueryable<DataAccess.Models.View> views;

            if (string.Equals(type, "novel", StringComparison.OrdinalIgnoreCase))
            {
                var novel = await _context.Novels
                    .Where(n => n.NovelID == id)
                    .Select(n => new { n.Title, n.TotalViews })
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cancellationToken);
                if (novel == null)
                {
                    return NotFound();
                }

                (title, totalViews) = (novel.Title, novel.TotalViews);
                views = _context.Views.Where(v => v.NovelId == id);
            }
            else if (string.Equals(type, "article", StringComparison.OrdinalIgnoreCase))
            {
                var article = await _context.Articles
                    .Where(a => a.ArticleID == id)
                    .Select(a => new { a.Title, a.TotalViews })
                    .AsNoTracking()
                    .FirstOrDefaultAsync(cancellationToken);
                if (article == null)
                {
                    return NotFound();
                }

                (title, totalViews) = (article.Title, article.TotalViews);
                views = _context.Views.Where(v => v.ArticleId == id);
            }
            else
            {
                return BadRequest(new { message = "type 必須是 novel 或 article。" });
            }

            var counts = await views
                .Where(v => v.CreateTime >= start)
                .GroupBy(v => v.CreateTime.Date)
                .Select(g => new { Date = g.Key, Count = g.Count() })
                .AsNoTracking()
                .ToDictionaryAsync(g => g.Date, g => g.Count, cancellationToken);

            var dates = Enumerable.Range(0, days).Select(i => start.AddDays(i)).ToList();
            var daily = dates.Select(d => counts.GetValueOrDefault(d)).ToArray();

            // 由目前的總觀看數往回推每天結束時的累積數
            var cumulative = new long[daily.Length];
            if (cumulative.Length > 0)
            {
                cumulative[^1] = totalViews;
                for (var i = cumulative.Length - 2; i >= 0; i--)
                {
                    cumulative[i] = cumulative[i + 1] - daily[i + 1];
                }
            }

            return Ok(new
            {
                title,
                labels = dates.Select(d => d.ToString("MM/dd")),
                daily,
                cumulative,
            });
        }
    }
}