using DataAccess.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using Involver.Common;

namespace Involver.Controllers
{
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
        public async Task<IActionResult> GetBatchArticleViews([FromBody] int[] ids)
        {
            if (ids == null || !ids.Any())
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
        public async Task<IActionResult> GetBatchNovelViews([FromBody] int[] ids)
        {
            if (ids == null || !ids.Any())
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
    }
}
