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

            Article = await Context.Articles
                .Include(a => a.Profile)
                .FirstOrDefaultAsync(m => m.ArticleID == id);

            if (Article == null)
            {
                return NotFound();
            }

            //Check authorization
            var isAuthorized = User.IsInRole(Authorization.Feedback.Feedbacks.FeedbackManagersRole) ||
                           User.IsInRole(Authorization.Feedback.Feedbacks.FeedbackAdministratorsRole);

            var currentUserId = UserManager.GetUserId(User);

            if (!isAuthorized
                && currentUserId != Article.ProfileID
                && Article.Block)
            {
                return Forbid();
            }

            await SetComments(id, pageIndex);

            //Add views
            Article.Views++;
            Context.Attach(Article).State = EntityState.Modified;
            await CheckMissionWatchArticle();

            try
            {
                await Context.SaveChangesAsync();
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
                    userProfile.VirtualCoins += 5;
                    Context.Attach(userProfile).State = EntityState.Modified;
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
                    Context.Attach(userProfile).State = EntityState.Modified;
                }
            }
        }

        private async Task SetComments(int? id, int? pageIndex)
        {
            IQueryable<Comment> comments = from c in Context.Comments
                                           select c;
            comments = comments
                .Include(c => c.Agrees)
                .Include(c => c.Messages)
                .Include(c => c.Profile)
                .Include(c => c.Dices)
                .Where(c => c.ArticleID == id)
                .OrderBy(c => c.CommentID);

            int pageSize = 5;
            Comments = await PaginatedList<Comment>.CreateAsync(
                comments, pageIndex ?? 1, pageSize);
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
            article.Block = block;
            Context.Articles.Update(article);
            await Context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostShareAsync(int id)
        {
            //Check mission:ShareCreation //CheckMissionShareCreation
            string UserID = UserManager.GetUserId(User);
            if(UserID != null)
            {
                Profile userProfile = await Context.Profiles
                .Where(p => p.ProfileID == UserID)
                .Include(p => p.Missions)
                .FirstOrDefaultAsync();
                if (userProfile.Missions.ShareCreation != true)
                {
                    userProfile.Missions.ShareCreation = true;
                    userProfile.VirtualCoins += 5;
                    Context.Attach(userProfile).State = EntityState.Modified;
                    StatusMessage = "每週任務：分享一次創作 已完成，獲得5 虛擬In幣。";
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
                    Context.Attach(userProfile).State = EntityState.Modified;
                }
                await Context.SaveChangesAsync();
            }
            return RedirectToPage("./Details", "OnGet", new { id }, "Share");
        }
    }
}
