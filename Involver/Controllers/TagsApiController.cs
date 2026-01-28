using DataAccess.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers
{
    [Route("api/tags")]
    [ApiController]
    public class TagsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TagsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// 搜尋標籤
        /// </summary>
        /// <param name="type">標籤類型，可以是 "novel" 或 "article"</param>
        /// <returns>符合類型的標籤列表</returns>
        [HttpGet("search")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false)]
        public async Task<IActionResult> SearchTags([FromQuery] string type)
        {
            IEnumerable<string> tags;

            if (type.Equals("novel", StringComparison.OrdinalIgnoreCase))
            {
                // 取得所有小說標籤
                tags = await _context.NovelTags
                    .AsNoTracking()
                    .Select(t => t.Name)
                    .Distinct()
                    .ToListAsync();
            }
            else if (type.Equals("article", StringComparison.OrdinalIgnoreCase))
            {
                // 取得所有文章標籤
                tags = await _context.ArticleTags
                    .AsNoTracking()
                    .Select(t => t.Name)
                    .Distinct()
                    .ToListAsync();
            }
            else
            {
                // 如果類型不符，回傳空列表
                tags = Enumerable.Empty<string>();
            }

            // Tagify 需要 { "value": "tagname" } 格式的物件
            return Ok(tags.Select(t => new { value = t }));
        }
    }
}
