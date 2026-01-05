using System.Text.Json;

using DataAccess.Data;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;

using Involver.Authorization.Novel;
using Involver.Common;
using Involver.Helpers;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Episodes;

public class DeleteModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    [BindProperty]
    public Episode Episode { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Episode = await Context.Episodes
            .Include(e => e.Novel).FirstOrDefaultAsync(m => m.EpisodeID == id);

        if (Episode == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                             User, Episode.Novel,
                                             NovelOperations.Delete);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Episode = await Context.Episodes
            .Include(e => e.Novel).FirstOrDefaultAsync(m => m.EpisodeID == id);

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                             User, Episode.Novel,
                                             NovelOperations.Delete);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        var comments = from c in Context.Comments
                       where c.EpisodeID == id
                       select c;
        var votes = from v in Context.Votes
                    where v.NormalOption.Voting.EpisodeID == id
                    select v;

        if (Episode != null)
        {
            //_context.Votes.RemoveRange(votes);
            //_context.Comments.RemoveRange(comments);
            //_context.Episodes.Remove(Episode);

            Episode.IsDeleted = true;

            await Context.SaveChangesAsync();
        }

        var toasts = await AchievementService.FirstTimeDeleteAsync(Episode.OwnerID);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        //return RedirectToPage("./Index");
        return RedirectToPage("/Novels/Details", "OnGet", new { id = Episode.NovelID });
    }
}