using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using DataAccess.Data;
using DataAccess.Models;

using Involver.Common;
using Involver.Models.ViewModels;
using Involver.Services;

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
IMissionService missionService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    //private readonly ILogger<IndexModel> _logger;

    public Profile UserProfile;

    /// <summary>
    /// 追蹤中的創作（含最新一集）
    /// </summary>
    public List<FollowedNovelViewModel> Follows { get; set; }

    public async Task<IActionResult> OnGetAsync()
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
            return Page();
        }

        // 只取最新一集的 ID 與標題，不載入章節全文
        Follows = await FollowedNovelViewModel
            .QueryByFollower(Context.Follows, userId)
            .Take(10)
            .AsNoTracking()
            .ToListAsync();

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