using DataAccess.Common;
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
public class InteractionModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public DataAccess.Models.Profile Profile { get; set; }
    public List<Comment> Comments { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        Profile = await Context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);

        if (Profile == null)
        {
            return NotFound();
        }

        Comments = await Context.Comments
            .Include(c => c.Novel)
            .Include(c => c.Episode)
            .Include(c => c.Article)
            .Where(c => c.ProfileID == id)
            .Where(c => c.Content != "")
            .OrderByDescending(c => c.UpdateTime)
            .Take(Parameters.PageSize)
            .AsNoTracking()
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnGetLoadMoreAsync(string id, int pageIndex)
    {
        var comments = await Context.Comments
            .Include(c => c.Novel)
            .Include(c => c.Episode)
            .Include(c => c.Article)
            .Where(c => c.ProfileID == id)
            .Where(c => c.Content != "")
            .OrderByDescending(c => c.UpdateTime)
            .Skip((pageIndex - 1) * Parameters.PageSize)
            .Take(Parameters.PageSize)
            .AsNoTracking()
            .ToListAsync();

        return Partial("_InteractionListPartial", comments);
    }
}
