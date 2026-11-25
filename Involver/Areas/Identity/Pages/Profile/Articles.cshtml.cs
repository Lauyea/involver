using DataAccess.Data;
using DataAccess.Models.Recommendation;

using Involver.Common;
using Involver.Models.ViewModels.Feed;
using Involver.Services;
using Involver.Services.Profiles;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile;

/// <summary>
/// 個人檔案的作品分頁：發表的一般文章，以作品卡片顯示，捲到底載入更多
/// </summary>
[AllowAnonymous]
public class ArticlesModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService,
    IProfileReadingService readingService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    /// <summary>
    /// 個人檔案
    /// </summary>
    public DataAccess.Models.Profile Profile { get; set; }

    /// <summary>
    /// 第一頁的文章卡片
    /// </summary>
    public List<ContentCardViewModel> Cards { get; set; } = [];

    /// <summary>
    /// 瀏覽者是否為本人（空狀態顯示「開始創作」）
    /// </summary>
    public bool IsOwner { get; set; }

    /// <summary>
    /// 顯示第一頁
    /// </summary>
    /// <param name="id">ProfileID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>頁面；找不到會員時 404</returns>
    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        Profile = await Context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id, cancellationToken);
        if (Profile == null)
        {
            return NotFound();
        }

        IsOwner = UserManager.GetUserId(User) == id;
        Cards = await readingService.GetWorksAsync(id, RecommendationContentType.Article, 1, cancellationToken);
        return Page();
    }

    /// <summary>
    /// 捲到底時載入下一頁
    /// </summary>
    /// <param name="id">ProfileID</param>
    /// <param name="pageIndex">頁碼</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>卡片 HTML</returns>
    public async Task<IActionResult> OnGetLoadMoreAsync(string id, int pageIndex, CancellationToken cancellationToken)
    {
        var cards = string.IsNullOrEmpty(id)
            ? []
            : await readingService.GetWorksAsync(id, RecommendationContentType.Article, pageIndex, cancellationToken);
        return Partial("_WorkCardListPartial", cards);
    }
}
