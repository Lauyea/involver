using DataAccess.Common;
using DataAccess.Data;

using Involver.Common;
using Involver.Extensions;
using Involver.Helpers;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile;

[AllowAnonymous]
public class FollowersModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public class FollowerViewModel
    {
        public string ProfileID { get; set; }
        public string UserName { get; set; }
        public string Introduction { get; set; }
        public string ImageUrl { get; set; }
        public DateTime FollowTime { get; set; }
    }

    public DataAccess.Models.Profile Profile { get; set; }
    public List<FollowerViewModel> Followers { get; set; }

    private async Task<List<FollowerViewModel>> GetFollowers(string id, int page = 1)
    {
        var follows = await Context.Follows
            .Include(f => f.Follower)
            .Where(f => f.ProfileID == id)
            .OrderByDescending(f => f.UpdateTime)
            .Skip((page - 1) * Parameters.PageSize)
            .Take(Parameters.PageSize)
            .AsNoTracking()
            .ToListAsync();

        var followerViewModels = new List<FollowerViewModel>();

        foreach (var follow in follows)
        {
            if (follow.Follower == null) continue;

            string imageUrl = follow.Follower.ImageUrl;
            if (string.IsNullOrEmpty(imageUrl))
            {
                var user = await UserManager.FindByIdAsync(follow.FollowerID);
                if (user != null)
                {
                    imageUrl = "https://www.gravatar.com/avatar/" + user.Email.ToMd5() + "?d=retro";
                }
            }

            followerViewModels.Add(new FollowerViewModel
            {
                ProfileID = follow.FollowerID,
                UserName = follow.Follower.UserName,
                Introduction = follow.Follower.Introduction,
                ImageUrl = imageUrl,
                FollowTime = follow.UpdateTime
            });
        }

        return followerViewModels;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        Profile = await Context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);

        if (Profile == null)
        {
            return NotFound();
        }

        Followers = await GetFollowers(id);

        return Page();
    }

    public async Task<IActionResult> OnGetLoadMoreAsync(string id, int pageIndex)
    {
        var followers = await GetFollowers(id, pageIndex);
        return Partial("_FollowerListPartial", followers);
    }
}
