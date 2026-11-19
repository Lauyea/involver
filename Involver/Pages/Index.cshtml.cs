using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using DataAccess.Data;
using DataAccess.Models;

using Involver.Common;
using Involver.Models.ViewModels.Feed;
using Involver.Services;
using Involver.Services.Recommendations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Involver.Pages;

[AllowAnonymous]
public class IndexModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService,
IMissionService missionService,
IHomeFeedService homeFeedService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    //private readonly ILogger<IndexModel> _logger;

    public Profile UserProfile;

    /// <summary>
    /// 會員首頁 Feed（未登入時為 null）
    /// </summary>
    public HomeFeedViewModel Feed { get; set; }

    /// <summary>
    /// 未登入首頁的熱門創作
    /// </summary>
    public List<ContentCardViewModel> Trending { get; private set; } = [];

    /// <summary>
    /// 冷啟動時選擇的分類標籤 ID
    /// </summary>
    [BindProperty]
    public List<int> SelectedCategoryIds { get; set; } = [];

    /// <summary>
    /// 冷啟動最多選幾個分類
    /// </summary>
    public int MaxPreferredCategories => HomeFeedService.MaxPreferredCategories;

    /// <summary>
    /// 儲存喜歡的分類（冷啟動），之後的推薦會以此為起點
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到首頁</returns>
    public async Task<IActionResult> OnPostPreferencesAsync(CancellationToken cancellationToken)
    {
        var userId = UserManager.GetUserId(User);
        if (userId == null)
        {
            return Challenge();
        }

        var selected = (SelectedCategoryIds ?? []).Distinct().ToList();
        if (selected.Count == 0 || selected.Count > HomeFeedService.MaxPreferredCategories)
        {
            StatusMessage = $"錯誤：請選擇 1 到 {HomeFeedService.MaxPreferredCategories} 個喜歡的分類。";
            return RedirectToPage();
        }

        await homeFeedService.SavePreferredCategoriesAsync(userId, selected, cancellationToken);
        StatusMessage = "已記下你喜歡的分類，推薦會依此調整。";
        return RedirectToPage(null, null, "feed-recommended");
    }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        string userId = UserManager.GetUserId(User);

        //if(userId == null)
        //{
        //    return RedirectToPage("/Feed/TrendingCreations", "OnGet");
        //}

        UserProfile = await Context
                                .Profiles
                                .Where(p => p.ProfileID == userId)
                                .Include(p => p.Missions)
                                .FirstOrDefaultAsync();

        if (UserProfile == null)
        {
            Trending = await homeFeedService.GetTrendingAsync(HomeFeedService.DiscoverCount, cancellationToken);
            return Page();
        }

        Feed = await homeFeedService.BuildAsync(userId, cancellationToken);

        if (UserProfile != null)
        {
            // 每日登入任務：旗標與獎勵由任務服務原子更新
            bool rewarded = await missionService.CompleteAsync(UserProfile.ProfileID, MissionType.DailyLogin);
            if (rewarded)
            {
                UserProfile.LastTimeLogin = DateTime.Now;//登入時間
                StatusMessage = "每日登入 已完成，獲得5 虛擬In幣。";
            }
            await Context.SaveChangesAsync();

            if (rewarded)
            {
                // 讀回剛發的獎勵，下面的成就檢查才會算到
                await Context.Entry(UserProfile).ReloadAsync();
            }
        }

        var toasts = await AchievementService.GetCoinsCountAsync(userId, UserProfile.VirtualCoins + UserProfile.RealCoins);

        toasts.AddRange(await AchievementService.CheckGradeAsync(UserProfile.ProfileID, UserProfile.EnrollmentDate));

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        return Page();
    }
}