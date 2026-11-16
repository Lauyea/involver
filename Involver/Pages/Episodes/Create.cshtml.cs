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

            // 「其他章節改為非最後一集」與「新增章節」放在同一個交易，新增失敗時不會讓所有章節都不是最後一集。
            // 也移到授權檢查之後，未授權的請求不會改到章節。
            await using (var transaction = await Context.Database.BeginTransactionAsync())
            {
                await SetOtherEpisodesNotLastAsync(id);

                Context.Episodes.Add(emptyEpisode);
                Novel.UpdateTime = DateTime.Now;
                await Context.SaveChangesAsync();

                await transaction.CommitAsync();
            }

            var toasts = await AchievementService.EpisodeCountAsync(Novel.ProfileID);

            if (toasts.Count > 0)
            {
                TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
            }

            return RedirectToPage("/Novels/Details", "OnGet", new { id }, "EpisodeHead");
        }
        return Page();
    }

    /// <summary>
    /// 將小說既有的章節都設為非最後一集（直接在資料庫更新，不載入章節全文）
    /// </summary>
    /// <param name="novelId">小說 ID</param>
    private async Task SetOtherEpisodesNotLastAsync(int novelId)
    {
        await Context.Episodes
            .Where(e => e.NovelID == novelId && e.IsLast)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsLast, false));
    }
}