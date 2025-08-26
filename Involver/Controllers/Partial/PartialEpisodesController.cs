using Involver.Common;
using DataAccess.Data;
using DataAccess.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Involver.Controllers.Partial
{
    [AllowAnonymous]
    [Route("[controller]/[action]")]
    [ApiController]
    public class PartialEpisodesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PartialEpisodesController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> Get(int novelId, int? pageIndex)
        {
            IQueryable<Episode> episodes = from e in _context.Episodes
                                           select e;
            episodes = episodes
                .Where(e => e.NovelID == novelId)
                .OrderByDescending(e => e.EpisodeID);

            // 不顯示軟刪除的資料
            episodes = episodes.Where(e => e.IsDeleted == false);

            var paginatedEpisodes = await PaginatedList<Episode>.CreateAsync(
                episodes, pageIndex ?? 1, Parameters.EpisodePageSize);

            return PartialView("~/Pages/Episodes/PartialEpisodes.cshtml", paginatedEpisodes);
        }
    }
}
