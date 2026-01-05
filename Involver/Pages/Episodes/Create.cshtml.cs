using System.Text.Json;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;

using Involver.Authorization.Novel;
using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Episodes;

public class CreateModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        Novel = Context.Novels.Where(n => n.NovelID == id).SingleOrDefault();

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                    User, Novel,
                                                    NovelOperations.Create);

        return !isAuthorized.Succeeded ? Forbid() : Page();
    }

    [BindProperty]
    public Episode Episode { get; set; }

    public string ErrorMessage { get; set; }
    public Novel Novel { get; set; }

    // To protect from overposting attacks, please enable the specific properties you want to bind to, for
    // more details see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync(int id)
    {
        Novel = await Context.Novels.Where(n => n.NovelID == id).FirstOrDefaultAsync();

        if (Episode.Content?.Length > Parameters.ArticleLength)
        {
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await SetOtherEpisodesNotLast(id);

        Episode.OwnerID = UserManager.GetUserId(User);

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                    User, Novel,
                                                    NovelOperations.Create);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        Episode emptyEpisode = new()
        {
            Title = "temp title",
            Content = "temp content"
        };

        //Protect from overposting attacks
        if (await TryUpdateModelAsync<Episode>(
            emptyEpisode,
            "Episode",   // Prefix for form value.
            e => e.Title, e => e.Content))
        {
            emptyEpisode.UpdateTime = DateTime.Now;
            emptyEpisode.NovelID = id;
            var tempUser = await Context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == emptyEpisode.OwnerID);
            emptyEpisode.OwnerID = Episode.OwnerID;
            emptyEpisode.IsLast = true;
            emptyEpisode.Views = 0;
            emptyEpisode.HasVoting = true;
            Context.Episodes.Add(emptyEpisode);
            Novel.UpdateTime = DateTime.Now;
            await Context.SaveChangesAsync();

            var toasts = await AchievementService.EpisodeCountAsync(Novel.ProfileID);

            if (toasts.Count > 0)
            {
                TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
            }

            return RedirectToPage("/Novels/Details", "OnGet", new { id }, "EpisodeHead");
        }
        return Page();
    }

    private async Task SetOtherEpisodesNotLast(int fromID)
    {
        List<Episode> episodes = Context.Episodes.Where(e => e.NovelID == fromID).ToList();
        episodes.ForEach(e => e.IsLast = false);
        await Context.SaveChangesAsync();
    }
}