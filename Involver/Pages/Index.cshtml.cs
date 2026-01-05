using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using DataAccess.Data;
using DataAccess.Models;

using Involver.Common;
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
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    //private readonly ILogger<IndexModel> _logger;

    public Profile UserProfile;

    public ICollection<Follow> Follows { get; set; }

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

        Follows = await Context.Follows
            .Include(f => f.Novel)
                .ThenInclude(n => n.Episodes)
             .Include(f => f.Novel)
                .ThenInclude(n => n.Profile)
            .Where(f => f.FollowerID == userId)
            .OrderByDescending(f => f.Novel.UpdateTime)
            .Take(10)
            .ToListAsync();

        if (UserProfile != null)
        {
            if (!UserProfile.Missions.DailyLogin)
            {
                UserProfile.LastTimeLogin = DateTime.Now;//登入時間
                UserProfile.Missions.DailyLogin = true;//每日登入任務
                UserProfile.VirtualCoins += 5;
                StatusMessage = "每日登入 已完成，獲得5 虛擬In幣。";
            }
            await Context.SaveChangesAsync();
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