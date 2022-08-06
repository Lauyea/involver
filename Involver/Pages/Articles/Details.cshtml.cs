using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Involver.Data;
using Involver.Models.ArticleModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Models;
using Involver.Authorization.Article;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Involver.Common;

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

        public PaginatedList<Comment> Comments { get; set; }
        public Article Article { get; set; }
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, int? pageIndex)
        {
            if (id == null)
            {
                return NotFound();
            }

            Article = await _context.Articles
                .Include(a => a.Profile)
                .Include(a => a.ArticleTags)
                .Include(n => n.ViewIps)
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

            await SetComments(id, pageIndex);

            AddViewsByIp();

            await AddViewer(currentUserId);

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

            return Page();
        }

        private async Task AddViewer(string currentUserId)
        {
            var userAsViewer = Article.Viewers.Where(v => v.ProfileID == currentUserId).FirstOrDefault();

            if (userAsViewer != null)
            {
                var articleViewer = userAsViewer.ArticleViewers.Where(v => v.ProfileID == currentUserId && v.ArticleID == Article.ArticleID).FirstOrDefault();

                articleViewer.ViewDate = DateTime.Now;

                _context.Attach(articleViewer).State = EntityState.Modified;
            }
            else
            {
                var userProfile = await _context.Profiles.Where(p => p.ProfileID == currentUserId).FirstOrDefaultAsync();

                if (userProfile == null)
                {
                    return;
                }

                Article.Viewers.Add(userProfile);
            }
        }

        private void AddViewsByIp()
        {
            var ip = Request.HttpContext.Connection.RemoteIpAddress.ToString();

            var ipRecord = Article.ViewIps.Where(i => i.Ip == ip).FirstOrDefault();

            if(ipRecord != null)
            {
                return;
            }

            var newIp = new ViewIp()
            {
                Ip = ip
            };

            Article.Views++;
            Article.ViewIps.Add(newIp);
            _context.Attach(Article).State = EntityState.Modified;
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
                    userProfile.VirtualCoins += 5;
                    _context.Attach(userProfile).State = EntityState.Modified;
                    StatusMessage = "每週任務：看一篇文章 已完成，獲得5 虛擬In幣。";
                }
                //Check other missions
                Missions missions = userProfile.Missions;
                if (missions.WatchArticle
                    && missions.Vote
                    && missions.LeaveComment
                    && missions.ViewAnnouncement
                    && missions.ShareCreation
                    && missions.BeAgreed)
                {
                    userProfile.Missions.CompleteOtherMissions = true;
                    _context.Attach(userProfile).State = EntityState.Modified;
                }
            }
        }

        private async Task SetComments(int? id, int? pageIndex)
        {
            IQueryable<Comment> comments = from c in _context.Comments
                                           select c;
            comments = comments
                .Include(c => c.Agrees)
                .Include(c => c.Messages.OrderByDescending(m => m.UpdateTime).Take(5))
                    .ThenInclude(c => c.Profile)
                .Include(c => c.Profile)
                .Include(c => c.Dices)
                .Where(c => c.ArticleID == id)
                .OrderBy(c => c.CommentID);

            
            Comments = await PaginatedList<Comment>.CreateAsync(
                comments, pageIndex ?? 1, Parameters.CommetPageSize);
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
