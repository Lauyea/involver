using DataAccess.Data;
using DataAccess.Models.NovelModel;

using Involver.Common;
using Involver.Data;
using Involver.Models.NovelModel;
using Involver.Models.ViewModels;
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

        /// <summary>
        /// 取得小說某一頁的章節列表（只查列表需要的欄位，不載入章節全文）
        /// </summary>
        /// <param name="novelId">小說 ID</param>
        /// <param name="pageIndex">頁碼</param>
        /// <param name="pageSize">每頁筆數</param>
        /// <returns>章節列表 View</returns>
        public async Task<IViewComponentResult> InvokeAsync(int novelId, int pageIndex, int pageSize)
        {
            var episodes = _context.Episodes
                .Where(e => e.NovelID == novelId && e.IsDeleted == false)
                .OrderByDescending(e => e.EpisodeID)
                .Select(e => new EpisodeListItemViewModel
                {
                    EpisodeID = e.EpisodeID,
                    Title = e.Title,
                    UpdateTime = e.UpdateTime,
                    Views = e.Views,
                })
                .AsNoTracking();

            var paginatedEpisodes = await PaginatedList<EpisodeListItemViewModel>.CreateAsync(episodes, pageIndex, pageSize);

            return View(paginatedEpisodes);
        }
    }
}
