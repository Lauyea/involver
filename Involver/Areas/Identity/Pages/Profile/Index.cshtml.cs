using System.Text.Json;

using DataAccess.Data;
using DataAccess.Models;

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
    public InvolverUser InvolverUser { get; set; }

    private async Task LoadAsync(string id)
    {
        UserID = UserManager.GetUserId(User);
        if (UserID == id)
        {
            ProfileOwner = true;
        }
        InvolverUser = await Context.Users.Where(u => u.Id == id).FirstOrDefaultAsync();
        Profile = await Context.Profiles
            .Include(p => p.Follows)
            .Where(p => p.ProfileID == id)
            .FirstOrDefaultAsync();
        if (Profile != null)
        {
            Follow existingFollow = Profile.Follows
            .Where(f => f.FollowerID == UserID)
            .FirstOrDefault();
            Followed = existingFollow != null;
        }
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        await LoadAsync(id);
        if (Profile != null)
        {
            // 觀看數以原子更新累加，避免併發時「讀出、改值、存回」掉計數
            await viewCountService.AddProfileViewAsync(Profile);

            var toasts = await AchievementService.GetCoinsCountAsync(Profile.ProfileID, Profile.VirtualCoins + Profile.RealCoins);

            toasts.AddRange(await AchievementService.CheckGradeAsync(Profile.ProfileID, Profile.EnrollmentDate));

            if (toasts.Count > 0)
            {
                TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
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
        if (Profile.Banned)
        {
            InvolverUser.Banned = false;
            Profile.Banned = false;
            StatusMessage = "You unbanned this user successfully.";
        }
        else
        {
            InvolverUser.Banned = true;
            Profile.Banned = true;
            StatusMessage = "You banned this user successfully.";
        }
        await Context.SaveChangesAsync();

        return Page();
    }
}