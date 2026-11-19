using System.Text.Json;

using DataAccess.Data;
using DataAccess.Models;

using Involver.Authorization.Profile;
using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile;

[AllowAnonymous]
public class IndexModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService,
IViewCountService viewCountService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public DataAccess.Models.Profile Profile { get; set; }
    public string UserID { get; set; }
    public bool ProfileOwner { get; set; } = false;
    public bool Followed { get; set; } = false;

    /// <summary>
    /// 追蹤此使用者的人數
    /// </summary>
    public int FollowerCount { get; set; }

    private async Task LoadAsync(string id)
    {
        UserID = UserManager.GetUserId(User);
        if (UserID == id)
        {
            ProfileOwner = true;
        }
        Profile = await Context.Profiles
            .Where(p => p.ProfileID == id)
            .FirstOrDefaultAsync();
        if (Profile != null)
        {
            // 被追蹤的對象是 Follow.ProfileID，追蹤者是 Follow.FollowerID；只需要計數與是否已追蹤，不載入整個集合
            FollowerCount = await Context.Follows
                .CountAsync(f => f.ProfileID == id);

            Followed = UserID != null && await Context.Follows
                .AnyAsync(f => f.ProfileID == id && f.FollowerID == UserID);
        }
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        await LoadAsync(id);
        if (Profile != null)
        {
            // 觀看數以原子更新累加，避免併發時「讀出、改值、存回」掉計數
            await viewCountService.AddProfileViewAsync(Profile);

            // 成就與 Toast 只屬於本人：造訪別人的頁面時不可替對方檢查成就，否則 Toast 會顯示在造訪者畫面上
            if (ProfileOwner)
            {
                var toasts = await AchievementService.GetCoinsCountAsync(Profile.ProfileID, Profile.VirtualCoins + Profile.RealCoins);

                toasts.AddRange(await AchievementService.CheckGradeAsync(Profile.ProfileID, Profile.EnrollmentDate));

                if (toasts.Count > 0)
                {
                    TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
                }
            }

            return Page();
        }
        else
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        await LoadAsync(id);
        if (Profile == null)
        {
            return NotFound();
        }

        // 畫面只對有權限的人顯示按鈕，伺服器端也必須檢查，否則任何人都能直接 POST 封鎖別人
        var isAuthorized = await AuthorizationService.AuthorizeAsync(User, Profile, ProfileOperations.Ban);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        // 封鎖狀態以 Profiles.Banned 為唯一來源
        if (Profile.Banned)
        {
            Profile.Banned = false;
            StatusMessage = "You unbanned this user successfully.";
        }
        else
        {
            Profile.Banned = true;
            StatusMessage = "You banned this user successfully.";
        }
        await Context.SaveChangesAsync();

        return Page();
    }
}