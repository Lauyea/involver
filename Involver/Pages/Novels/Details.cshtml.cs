using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;

using Involver.Authorization.Novel;
using Involver.Common;
using Involver.Helpers;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Novels;

[AllowAnonymous]
public class DetailsModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public Novel Novel { get; set; }
    public Profile Writer { get; set; }
    public PaginatedList<Episode> Episodes { get; set; }
    public List<Novel> RecommendNovels { get; set; }
    public bool Followed { get; set; } = false;

    public string UserId { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id, int? pageIndex, int? pageIndexEpisode)
    {
        if (id == null)
        {
            return NotFound();
        }

        Novel = await Context.Novels
            .Include(n => n.Profile)
            .Include(n => n.Follows)
            .Include(n => n.NovelTags)
            .Include(n => n.Viewers)
            .Include(n => n.Views)
            .FirstOrDefaultAsync(m => m.NovelID == id);

        if (Novel == null)
        {
            return NotFound();
        }

        //Check authorization
        var isAuthorized = User.IsInRole(Authorization.Novel.Novels.NovelManagersRole)
            || User.IsInRole(Authorization.Novel.Novels.NovelAdministratorsRole);

        UserId = UserManager.GetUserId(User);

        if (!isAuthorized
            && UserId != Novel.ProfileID
            && Novel.Block)
        {
            return Forbid();
        }

        Writer = Novel.Profile;

        IQueryable<Episode> episodes = from e in Context.Episodes
                                       select e;
        episodes = episodes
            .Where(e => e.NovelID == id)
            .OrderByDescending(e => e.EpisodeID);


        Episodes = await PaginatedList<Episode>.CreateAsync(
            episodes, pageIndexEpisode ?? 1, Parameters.EpisodePageSize);

        var tagArr = Novel.NovelTags.ToArray();

        var recommendNovels = Context.Novels
            .Where(n => n.Type == Novel.Type)
            .Where(n => n.Block == false)
            .Where(n => n.IsDeleted == false);

        // There is most 3 tags right now
        if (tagArr.Length > 2)
        {
            recommendNovels = recommendNovels
                .Where(n => n.NovelTags.Contains(tagArr[0])
                || n.NovelTags.Contains(tagArr[1])
                || n.NovelTags.Contains(tagArr[2]));
        }
        else if (tagArr.Length > 1)
        {
            recommendNovels = recommendNovels
                .Where(n => n.NovelTags.Contains(tagArr[0])
                || n.NovelTags.Contains(tagArr[1]));
        }
        else if (tagArr.Length > 0)
        {
            recommendNovels = recommendNovels
                .Where(n => n.NovelTags.Contains(tagArr[0]));
        }


        recommendNovels = recommendNovels.OrderByDescending(n => n.MonthlyCoins)
            .Take(5)
            .OrderByDescending(n => n.UpdateTime)
            .AsNoTracking();

        RecommendNovels = recommendNovels.ToList();

        Follow existingFollow = Novel.Follows
            .Where(f => f.FollowerID == UserId)
            .FirstOrDefault();

        Followed = existingFollow != null;

        await AddViewRecordAsync();

        if (UserId != null)
        {
            await AddViewer();
        }

        try
        {
            await Context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!NovelExists(Novel.NovelID))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        var toasts = await AchievementService.ReadNovelAsync(UserId);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        return Page();
    }

    /// <summary>
    /// AddViewer
    /// </summary>
    /// <returns></returns>
    /// TODO: 之後可能要與 AddView 合併
    private async Task AddViewer()
    {
        var userAsViewer = Novel.Viewers.Where(v => v.ProfileID == UserId).FirstOrDefault();

        if (userAsViewer != null)
        {
            var novelViewer = userAsViewer.NovelViewers.Where(v => v.ProfileID == UserId && v.NovelID == Novel.NovelID).FirstOrDefault();

            novelViewer.ViewDate = DateTime.Now;
        }
        else
        {
            var userProfile = await Context.Profiles.Where(p => p.ProfileID == UserId).FirstOrDefaultAsync();

            Novel.Viewers.Add(userProfile);
        }
    }

    private async Task AddViewRecordAsync()
    {
        var sessionCookie = Request.Cookies["ViewSession"];
        var sessionId = "";

        if (sessionCookie == null)
        {
            sessionId = Guid.NewGuid().ToString();
            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddDays(1),
                HttpOnly = false, // 若要讓 JS 也能存取，設為 false
                Secure = true,    // 若只允許 HTTPS，設為 true
                SameSite = SameSiteMode.Lax
            };
            Response.Cookies.Append("ViewSession", sessionId, cookieOptions);
        }
        else
        {
            sessionId = sessionCookie;
        }

        var userId = UserManager.GetUserId(User);

        // Check if a view has been recorded recently (e.g., in the last hour) to prevent spamming views by refreshing.
        // This timespan can be adjusted.
        var recentViewExists = await Context.Views
            .Where(v => v.NovelId == Novel.NovelID)
            .Where(v => (v.UserId != null && v.UserId == userId) || (v.SessionId == sessionId))
            .Where(v => v.CreateTime > DateTime.Now.AddHours(-1))
            .AnyAsync();

        if (recentViewExists)
        {
            return; // A recent view was found, so don't record another one.
        }

        var view = new DataAccess.Models.View
        {
            NovelId = Novel.NovelID,
            UserId = userId,
            SessionId = sessionId,
            CreateTime = DateTime.Now
        };

        Context.Views.Add(view);

        Novel.TotalViews++;
        Novel.DailyView++;
    }

    private bool NovelExists(int id)
    {
        return Context.Novels.Any(e => e.NovelID == id);
    }

    public async Task<IActionResult> OnPostAsync(int id, bool block)
    {
        var novel = await Context.Novels.FirstOrDefaultAsync(
                                                  m => m.NovelID == id);

        if (novel == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(User, novel,
                                    NovelOperations.Block);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }
        novel.Block = block;
        Context.Novels.Update(novel);
        await Context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    public IActionResult OnGetEpisodes(int novelId, int pageIndex)
    {
        int pageSize = Parameters.EpisodePageSize;
        return ViewComponent("EpisodeList", new { novelId, pageIndex, pageSize });
    }
}