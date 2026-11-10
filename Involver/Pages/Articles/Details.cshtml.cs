using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Authorization.Comment;
using Involver.Common;
using Involver.Extensions;
using Involver.Helpers;
using Involver.Models.ViewModels;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Involver.Pages.Articles;

[AllowAnonymous]
public class DetailsModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService,
IViewCountService viewCountService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public Article Article { get; set; }
    public List<TocItem> Toc { get; set; }
    public string ProcessedContent { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id, int? pageIndex)
    {
        if (id == null)
        {
            return NotFound();
        }

        Article = await Context.Articles
            .Include(a => a.Profile)
            .Include(a => a.ArticleTags)
            .Include(n => n.Viewers)
            .FirstOrDefaultAsync(m => m.ArticleID == id);

        if (Article == null)
        {
            return NotFound();
        }

        //Check authorization
        var isAuthorized = User.IsInRole(Authorization.Article.Articles.ArticleManagersRole) ||
                       User.IsInRole(Authorization.Article.Articles.ArticleAdministratorsRole);

        var currentUserId = UserManager.GetUserId(User);

        if (!isAuthorized
            && currentUserId != Article.ProfileID
            && Article.Block)
        {
            return Forbid();
        }

        //TOC
        string content = Article.Content;
        if (content != null)
        {
            content = content.Replace("\r\n", "<br />");
        }
        Toc = TableOfContentsHelper.Generate(content, out string processedContent);
        ProcessedContent = processedContent;

        var isNewView = await AddViewRecordAsync();

        if (currentUserId != null)
        {
            await AddViewer(currentUserId);
        }

        await CheckMissionWatchArticle();

        try
        {
            // 觀看紀錄與觀看數放在同一個交易，避免只寫入其中一邊
            await using var transaction = await Context.Database.BeginTransactionAsync();

            await Context.SaveChangesAsync();

            if (isNewView)
            {
                await viewCountService.AddArticleViewAsync(Article);
            }

            await transaction.CommitAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ArticleExists(Article.ArticleID))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        var toasts = await AchievementService.ReadArticleAsync(currentUserId);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        return Page();
    }

    /// <summary>
    /// AddViewer
    /// </summary>
    /// <param name="currentUserId"></param>
    /// <returns></returns>
    /// TODO: 之後可能要與 AddView 合併
    private async Task AddViewer(string currentUserId)
    {
        var userAsViewer = Article.Viewers.Where(v => v.ProfileID == currentUserId).FirstOrDefault();

        if (userAsViewer != null)
        {
            var articleViewer = userAsViewer.ArticleViewers.Where(v => v.ProfileID == currentUserId && v.ArticleID == Article.ArticleID).FirstOrDefault();

            articleViewer.ViewDate = DateTime.Now;
        }
        else
        {
            var userProfile = await Context.Profiles.Where(p => p.ProfileID == currentUserId).FirstOrDefaultAsync();

            Article.Viewers.Add(userProfile);
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
            .Where(v => v.ArticleId == Article.ArticleID)
            .Where(v => (v.UserId != null && v.UserId == userId) || (v.SessionId == sessionId))
            .Where(v => v.CreateTime > DateTime.Now.AddHours(-1))
            .AnyAsync();

        if (recentViewExists)
        {
            return false; // A recent view was found, so don't record another one.
        }

        var view = new View
        {
            ArticleId = Article.ArticleID,
            UserId = userId,
            SessionId = sessionId,
            CreateTime = DateTime.Now
        };

        Context.Views.Add(view);

        // 觀看數在存檔後由 IViewCountService 原子累加，避免併發時「讀出、改值、存回」掉計數
        return true;
    }

    private async Task CheckMissionWatchArticle()
    {
        //Check mission:WatchArticle
        string UserID = UserManager.GetUserId(User);
        if (UserID != null)
        {
            Profile userProfile = await Context.Profiles
            .Where(p => p.ProfileID == UserID)
            .Include(p => p.Missions)
            .FirstOrDefaultAsync();
            if (userProfile.Missions.WatchArticle != true)
            {
                userProfile.Missions.WatchArticle = true;
                userProfile.AwardCoins();
                StatusMessage = "每週任務：看一篇文章 已完成，獲得5 虛擬In幣。";
            }

            // 檢查是否完成所有任務，若完成會自動加獎勵幣
            userProfile.Missions.CheckCompletion(userProfile);
        }
    }

    private bool ArticleExists(int id)
    {
        return Context.Articles.Any(e => e.ArticleID == id);
    }

    public async Task<IActionResult> OnPostAsync(int id, bool block)
    {
        var article = await Context.Articles.FirstOrDefaultAsync(
                                                  m => m.ArticleID == id);

        if (article == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(User, article,
                                    ArticleOperations.Block);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }
        // article 已被追蹤，SaveChanges 只會寫入 Block；呼叫 Update 會整列寫回，蓋掉期間累積的 In 幣
        article.Block = block;
        await Context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}