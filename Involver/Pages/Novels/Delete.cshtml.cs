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

namespace Involver.Pages.Novels;

public class DeleteModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    [BindProperty]
    public Novel Novel { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Novel = await Context.Novels
            .Include(n => n.Profile).FirstOrDefaultAsync(m => m.NovelID == id);

        if (Novel == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                             User, Novel,
                                             NovelOperations.Delete);

        return !isAuthorized.Succeeded ? Forbid() : Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Novel = await Context.Novels.FindAsync(id);

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                             User, Novel,
                                             NovelOperations.Delete);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        var comments = from c in Context.Comments
                       where c.NovelID == id
                       select c;

        var episodes = from e in Context.Episodes
                       where e.NovelID == id
                       select e;
        var commentsInEpisodes = from c in Context.Comments
                                 where c.Episode.NovelID == id
                                 select c;
        var votes = from v in Context.Votes
                    where v.NormalOption.Voting.Episode.NovelID == id
                    select v;
        var involvings = from i in Context.Involvings
                         where i.NovelID == id
                         select i;
        var follows = from f in Context.Follows
                      where f.NovelID == id
                      select f;

        if (Novel != null)
        {
            //_context.Follows.RemoveRange(follows);
            //_context.Involvings.RemoveRange(involvings);
            //_context.Votes.RemoveRange(votes);
            //_context.Comments.RemoveRange(comments);
            //_context.Comments.RemoveRange(commentsInEpisodes);
            //_context.Episodes.RemoveRange(episodes);
            //_context.Novels.Remove(Novel);

            Novel.IsDeleted = true;

            await Context.SaveChangesAsync();
        }

        var toasts = await AchievementService.FirstTimeDeleteAsync(Novel.ProfileID);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        return RedirectToPage("./Index");
    }
}