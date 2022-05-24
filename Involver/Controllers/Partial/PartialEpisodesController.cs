using Involver.Common;
using Involver.Data;
using Involver.Models.NovelModel;
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

            var paginatedEpisodes = await PaginatedList<Episode>.CreateAsync(
                episodes, pageIndex ?? 1, Parameters.EpisodePageSize);

            return PartialView("~/Pages/Episodes/PartialEpisodes.cshtml", paginatedEpisodes);
        }
    }
}
