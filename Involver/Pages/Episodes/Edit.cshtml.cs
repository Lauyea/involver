using System.Text.Json;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.NovelModel;

using Involver.Authorization.Novel;
using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Episodes;

public class EditModel(
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
                                              NovelOperations.Update);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        return Page();
    }

    // To protect from overposting attacks, please enable the specific properties you want to bind to, for
    // more details see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (Episode.Content?.Length > Parameters.ArticleLength)
        {
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Fetch data from DB to get OwnerID.
        Episode episode = await Context
            .Episodes
            .Include(e => e.Novel)
            .FirstOrDefaultAsync(a => a.EpisodeID == id);

        if (episode == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                              User, episode.Novel,
                                              NovelOperations.Update);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        episode.UpdateTime = DateTime.Now;
        episode.OwnerID = UserManager.GetUserId(User);
        episode.Content = Episode.Content;
        episode.Title = Episode.Title;


        try
        {
            await Context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EpisodeExists(Episode.EpisodeID))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        var toasts = await AchievementService.FirstTimeEditAsync(Episode.OwnerID);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        return RedirectToPage("/Novels/Details", "OnGet", new { id = episode.NovelID }, "EpisodeHead");
    }

    private bool EpisodeExists(int id)
    {
        return Context.Episodes.Any(e => e.EpisodeID == id);
    }
}