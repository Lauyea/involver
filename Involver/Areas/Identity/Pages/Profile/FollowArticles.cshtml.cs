using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.ArticleModel;
using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile;

[AllowAnonymous]
public class FollowArticlesModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager, 
    IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public DataAccess.Models.Profile Profile { get; set; }
    public List<Article> Articles { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        Profile = await Context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);

        if (Profile == null)
        {
            return NotFound();
        }

        var followedProfileIds = await Context.Follows
            .Where(f => f.FollowerID == id && f.ProfileID != null)
            .Select(f => f.ProfileID)
            .ToListAsync();

        Articles = await Context.Articles
            .Include(a => a.Profile)
            .Where(a => followedProfileIds.Contains(a.ProfileID))
            .OrderByDescending(a => a.UpdateTime)
            .Take(Parameters.PageSize)
            .AsNoTracking()
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnGetLoadMoreAsync(string id, int pageIndex)
    {
        var followedProfileIds = await Context.Follows
            .Where(f => f.FollowerID == id && f.ProfileID != null)
            .Select(f => f.ProfileID)
            .ToListAsync();

        var articles = await Context.Articles
            .Include(a => a.Profile)
            .Where(a => followedProfileIds.Contains(a.ProfileID))
            .OrderByDescending(a => a.UpdateTime)
            .Skip((pageIndex - 1) * Parameters.PageSize)
            .Take(Parameters.PageSize)
            .AsNoTracking()
            .ToListAsync();

        return Partial("_FollowArticleListPartial", articles);
    }
}
