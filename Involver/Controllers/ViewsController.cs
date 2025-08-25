using DataAccess.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;

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
            var sevenDaysAgo = DateTime.Now.Date.AddDays(-7);

            var views = await _context.Views
                .Where(v => v.ArticleId == id && v.CreateTime >= sevenDaysAgo)
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
            var sevenDaysAgo = DateTime.Now.Date.AddDays(-7);

            var views = await _context.Views
                .Where(v => v.NovelId == id && v.CreateTime >= sevenDaysAgo)
                .GroupBy(v => v.CreateTime.Date)
                .Select(g => new { Date = g.Key, Views = g.Count() })
                .OrderBy(g => g.Date)
                .ToListAsync();

            return Ok(views);
        }
    }
}
