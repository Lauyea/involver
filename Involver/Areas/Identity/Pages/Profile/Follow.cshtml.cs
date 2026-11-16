using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;

using Involver.Common;
using Involver.Models.ViewModels;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile;

[AllowAnonymous]
public class FollowModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public DataAccess.Models.Profile Profile { get; set; }
    /// <summary>
    /// 追蹤中的創作（含最新一集）
    /// </summary>
    public List<FollowedNovelViewModel> Follows { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        Profile = await Context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);

        if (Profile == null)
        {
            return NotFound();
        }

        Follows = await FollowedNovelViewModel
            .QueryByFollower(Context.Follows, id)
            .Take(Parameters.PageSize)
            .AsNoTracking()
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnGetLoadMoreAsync(string id, int pageIndex)
    {
        var follows = await FollowedNovelViewModel
            .QueryByFollower(Context.Follows, id)
            .Skip((pageIndex - 1) * Parameters.PageSize)
            .Take(Parameters.PageSize)
            .AsNoTracking()
            .ToListAsync();

        return Partial("_FollowListPartial", follows);
    }
}
