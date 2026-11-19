using Involver.Models.ViewModels.Feed;
using Involver.Services.Recommendations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Feed
{
    /// <summary>
    /// 熱門創作：近 7 天不重複讀者最多的作品（排除已刪除、封鎖）。與首頁 Feed 的「熱門」區塊相同
    /// </summary>
    /// <param name="homeFeedService">首頁 Feed</param>
    /// <param name="userManager">帳號管理</param>
    [AllowAnonymous]
    public class TrendingCreationsModel(
        IHomeFeedService homeFeedService,
        UserManager<DataAccess.Data.InvolverUser> userManager) : PageModel
    {
        /// <summary>
        /// 目前使用者 ProfileID；未登入為 null
        /// </summary>
        public string UserProfileID { get; private set; }

        /// <summary>
        /// 熱門作品
        /// </summary>
        public List<ContentCardViewModel> Novels { get; private set; } = [];

        /// <summary>
        /// 載入熱門作品
        /// </summary>
        /// <param name="cancellationToken">取消權杖</param>
        public async Task OnGetAsync(CancellationToken cancellationToken)
        {
            UserProfileID = userManager.GetUserId(User);
            Novels = await homeFeedService.GetTrendingAsync(HomeFeedService.TrendingPageCount, cancellationToken);
        }
    }
}
