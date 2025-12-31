using DataAccess.Data;
using DataAccess.Models.AchievementModel;

using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile;

[AllowAnonymous]
public class AchievementsModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public DataAccess.Models.Profile Profile { get; set; }

    public List<Achievement> Achievements { get; set; }
    public string UserID { get; set; }

    public bool ProfileOwner { get; set; } = false;

    private async Task LoadAsync(string id)
    {
        UserID = UserManager.GetUserId(User);
        if (UserID == id)
        {
            ProfileOwner = true;
        }
        Profile = await Context.Profiles
            .Include(p => p.Achievements)
                .ThenInclude(a => a.ProfileAchievements)
            .Where(p => p.ProfileID == id)
            .FirstOrDefaultAsync();
        Achievements = Profile.Achievements.ToList();
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        //TODO 可能參考steam的成就顯示方式
        await LoadAsync(id);
        if (Profile != null)
        {
            return Page();
        }
        else
        {
            return NotFound();
        }
    }
}