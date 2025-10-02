using DataAccess.Data;
using DataAccess.Models.NovelModel;

using Involver.Common;
using Involver.Data;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Views.Shared.Components.EpisodeList
{
    public class EpisodeListViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public EpisodeListViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int novelId, int pageIndex, int pageSize)
        {
            var episodes = _context.Episodes
                .Where(e => e.NovelID == novelId && e.IsDeleted == false)
                .OrderByDescending(e => e.EpisodeID);

            var paginatedEpisodes = await PaginatedList<Episode>.CreateAsync(episodes.AsNoTracking(), pageIndex, pageSize);

            return View(paginatedEpisodes);
        }
    }
}
