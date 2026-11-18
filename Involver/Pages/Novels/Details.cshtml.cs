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
IAchievementService achievementService,
IViewCountService viewCountService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public Novel Novel { get; set; }
    public Profile Writer { get; set; }

    /// <summary>
    /// 章節總頁數，供前端分頁使用（章節列表本身由 EpisodeList View Component 載入）
    /// </summary>
    public int EpisodeTotalPages { get; set; }

    public List<Novel> RecommendNovels { get; set; }
    public bool Followed { get; set; } = false;

    /// <summary>
    /// 追蹤這部小說的人數
    /// </summary>
    public int FollowCount { get; set; }

    public string UserId { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id, int? pageIndex, int? pageIndexEpisode)
    {
        if (id == null)
        {
            return NotFound();
        }

        Novel = await Context.Novels
            .Include(n => n.Profile)
            .Include(n => n.Tags)
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

        // 只需要總頁數，不必載入章節全文
        var episodeCount = await Context.Episodes
            .Where(e => e.NovelID == id)
            .CountAsync();

        EpisodeTotalPages = (int)Math.Ceiling(episodeCount / (double)Parameters.EpisodePageSize);

        var tagIds = Novel.Tags.Select(t => t.TagId).ToList();

        var recommendNovels = Context.Novels
            .Where(n => n.Type == Novel.Type)
            .Where(n => n.Block == false)
            .Where(n => n.IsDeleted == false);

        // 有任一個相同標籤的作品
        if (tagIds.Count > 0)
        {
            recommendNovels = recommendNovels
                .Where(n => n.Tags.Any(t => tagIds.Contains(t.TagId)));
        }


        recommendNovels = recommendNovels.OrderByDescending(n => n.MonthlyCoins)
            .Take(5)
            .OrderByDescending(n => n.UpdateTime)
            .AsNoTracking();

        RecommendNovels = await recommendNovels.ToListAsync();

        FollowCount = await Context.Follows
            .Where(f => f.NovelID == id)
            .CountAsync();

        if (UserId != null)
        {
            Followed = await Context.Follows
                .AnyAsync(f => f.NovelID == id && f.FollowerID == UserId);
        }

        var isNewView = await AddViewRecordAsync();

        if (UserId != null)
        {
            await AddViewer();
        }

        try
        {
            // 觀看紀錄與觀看數放在同一個交易，避免只寫入其中一邊
            await using var transaction = await Context.Database.BeginTransactionAsync();

            await Context.SaveChangesAsync();

            if (isNewView)
            {
                await viewCountService.AddNovelViewAsync(Novel);
            }

            await transaction.CommitAsync();
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
    /// 記錄目前使用者看過這部小說：已有紀錄就更新觀看時間，沒有就新增一筆（尚未存檔）。
    /// 直接以 (ProfileID, NovelID) 查 NovelViewers 那一筆，不載入所有看過的人。
    /// </summary>
    /// TODO: 之後可能要與 AddView 合併
    private async Task AddViewer()
    {
        var novelViewer = await Context.Set<NovelViewer>()
            .FirstOrDefaultAsync(v => v.ProfileID == UserId && v.NovelID == Novel.NovelID);

        if (novelViewer != null)
        {
            novelViewer.ViewDate = DateTime.Now;
        }
        else
        {
            Context.Set<NovelViewer>().Add(new NovelViewer
            {
                ProfileID = UserId,
                NovelID = Novel.NovelID,
                ViewDate = DateTime.Now
            });
        }
    }

    /// <summary>
    /// 新增一筆觀看紀錄到 DbContext（尚未存檔）。一小時內同一使用者或工作階段已看過則不新增。
    /// </summary>
    /// <returns>有新增觀看紀錄回傳 true</returns>
    private async Task<bool> AddViewRecordAsync()
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
            return false; // A recent view was found, so don't record another one.
        }

        var view = new DataAccess.Models.View
        {
            NovelId = Novel.NovelID,
            UserId = userId,
            SessionId = sessionId,
            CreateTime = DateTime.Now
        };

        Context.Views.Add(view);

        // 觀看數在存檔後由 IViewCountService 原子累加，避免併發時「讀出、改值、存回」掉計數
        return true;
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
        // novel 已被追蹤，SaveChanges 只會寫入 Block；呼叫 Update 會整列寫回，蓋掉期間累積的 In 幣
        novel.Block = block;
        await Context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    public IActionResult OnGetEpisodes(int novelId, int pageIndex)
    {
        int pageSize = Parameters.EpisodePageSize;
        return ViewComponent("EpisodeList", new { novelId, pageIndex, pageSize });
    }
}