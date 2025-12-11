using System;
using System.Collections.Generic;
using System.Linq;
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
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Involver.Pages.Articles
{
    [AllowAnonymous]
    public class DetailsModel : DI_BasePageModel
    {
        public DetailsModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public Article Article { get; set; }
        public List<TocItem> Toc { get; set; }
        public string ProcessedContent { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, int? pageIndex)
        {
            if (id == null)
            {
                return NotFound();
            }

            Article = await _context.Articles
                .Include(a => a.Profile)
                .Include(a => a.ArticleTags)
                .Include(n => n.Views)
                .Include(n => n.Viewers)
                .FirstOrDefaultAsync(m => m.ArticleID == id);

            if (Article == null)
            {
                return NotFound();
            }

            //Check authorization
            var isAuthorized = User.IsInRole(Authorization.Article.Articles.ArticleManagersRole) ||
                           User.IsInRole(Authorization.Article.Articles.ArticleAdministratorsRole);

            var currentUserId = _userManager.GetUserId(User);

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

            await AddViewRecordAsync();

            if (currentUserId != null)
            {
                await AddViewer(currentUserId);
            }

            await CheckMissionWatchArticle();

            try
            {
                await _context.SaveChangesAsync();
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

            if (!string.IsNullOrEmpty(ToastsJson))
            {
                Toasts = System.Text.Json.JsonSerializer.Deserialize<List<Toast>>(ToastsJson);
            }

            var toasts = await Helpers.AchievementHelper.ReadArticleAsync(_context, currentUserId);

            Toasts.AddRange(toasts);

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
                var userProfile = await _context.Profiles.Where(p => p.ProfileID == currentUserId).FirstOrDefaultAsync();

                Article.Viewers.Add(userProfile);
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
                    IsEssential = true
                };
                Response.Cookies.Append("ViewSession", sessionId, cookieOptions);
            }
            else
            {
                sessionId = sessionCookie;
            }

            var userId = _userManager.GetUserId(User);

            // Check if a view has been recorded recently (e.g., in the last hour) to prevent spamming views by refreshing.
            // This timespan can be adjusted.
            var recentViewExists = await _context.Views
                .Where(v => v.ArticleId == Article.ArticleID)
                .Where(v => (v.UserId != null && v.UserId == userId) || (v.SessionId == sessionId))
                .Where(v => v.CreateTime > DateTime.Now.AddHours(-1))
                .AnyAsync();

            if (recentViewExists)
            {
                return; // A recent view was found, so don't record another one.
            }

            var view = new DataAccess.Models.View
            {
                ArticleId = Article.ArticleID,
                UserId = userId,
                SessionId = sessionId,
                CreateTime = DateTime.Now
            };

            _context.Views.Add(view);

            Article.TotalViews++;
            Article.DailyView++;
        }

        private async Task CheckMissionWatchArticle()
        {
            //Check mission:WatchArticle
            string UserID = _userManager.GetUserId(User);
            if (UserID != null)
            {
                Profile userProfile = await _context.Profiles
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
            return _context.Articles.Any(e => e.ArticleID == id);
        }

        public async Task<IActionResult> OnPostAsync(int id, bool block)
        {
            var article = await _context.Articles.FirstOrDefaultAsync(
                                                      m => m.ArticleID == id);

            if (article == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(User, article,
                                        ArticleOperations.Block);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }
            article.Block = block;
            _context.Articles.Update(article);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}