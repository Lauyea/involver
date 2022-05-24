using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Involver.Data;
using Involver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Authorization.Message;
using Involver.Common;

namespace Involver.Pages.Comments
{
    [AllowAnonymous]
    public class DetailModel : DI_BasePageModel
    {
        public DetailModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            UserManager<InvolverUser> userManager)
            : base(context, authorizationService, userManager)
        {
        }

        public Comment Comment { get; set; }

        public Comment PreviousComment { get; set; }
        public Comment NextComment { get; set; }
        public PaginatedList<Message> Messages { get; set; }

        [BindProperty]
        public Message Message { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, int pageIndex)
        {
            if (id == null)
            {
                return NotFound();
            }

            Comment = await Context.Comments
                .Include(c => c.Announcement)
                .Include(c => c.Article)
                .Include(c => c.Episode)
                .Include(c => c.Feedback)
                .Include(c => c.Novel)
                .Include(c => c.Dices)
                .Include(c => c.Profile).FirstOrDefaultAsync(m => m.CommentID == id);

            if (Comment.Announcement != null)
            {
                PreviousComment = await Context.Comments
                .Where(c => c.AnnouncementID == Comment.Announcement.AnnouncementID)
                .Where(c => c.CommentID < id)
                .OrderByDescending(c => c.CommentID)
                .FirstOrDefaultAsync();

                NextComment = await Context.Comments
                    .Where(c => c.AnnouncementID == Comment.Announcement.AnnouncementID)
                    .Where(c => c.CommentID > id)
                    .OrderBy(c => c.CommentID)
                    .FirstOrDefaultAsync();
            }

            if (Comment.Article != null)
            {
                PreviousComment = await Context.Comments
                .Where(c => c.ArticleID == Comment.Article.ArticleID)
                .Where(c => c.CommentID < id)
                .OrderByDescending(c => c.CommentID)
                .FirstOrDefaultAsync();

                NextComment = await Context.Comments
                    .Where(c => c.ArticleID == Comment.Article.ArticleID)
                    .Where(c => c.CommentID > id)
                    .OrderBy(c => c.CommentID)
                    .FirstOrDefaultAsync();
            }

            if (Comment.Episode != null)
            {
                PreviousComment = await Context.Comments
                .Where(c => c.EpisodeID == Comment.Episode.EpisodeID)
                .Where(c => c.CommentID < id)
                .OrderByDescending(c => c.CommentID)
                .FirstOrDefaultAsync();

                NextComment = await Context.Comments
                    .Where(c => c.EpisodeID == Comment.Episode.EpisodeID)
                    .Where(c => c.CommentID > id)
                    .OrderBy(c => c.CommentID)
                    .FirstOrDefaultAsync();
            }

            if (Comment.Feedback != null)
            {
                PreviousComment = await Context.Comments
                .Where(c => c.FeedbackID == Comment.Feedback.FeedbackID)
                .Where(c => c.CommentID < id)
                .OrderByDescending(c => c.CommentID)
                .FirstOrDefaultAsync();

                NextComment = await Context.Comments
                    .Where(c => c.FeedbackID == Comment.Feedback.FeedbackID)
                    .Where(c => c.CommentID > id)
                    .OrderBy(c => c.CommentID)
                    .FirstOrDefaultAsync();
            }

            if (Comment.Novel != null)
            {
                PreviousComment = await Context.Comments
                .Where(c => c.NovelID == Comment.Novel.NovelID)
                .Where(c => c.CommentID < id)
                .OrderByDescending(c => c.CommentID)
                .FirstOrDefaultAsync();

                NextComment = await Context.Comments
                    .Where(c => c.NovelID == Comment.Novel.NovelID)
                    .Where(c => c.CommentID > id)
                    .OrderBy(c => c.CommentID)
                    .FirstOrDefaultAsync();
            }

            if (Comment == null)
            {
                return NotFound();
            }

            await SetMessages(id, pageIndex);

            return Page();
        }

        private async Task SetMessages(int? id, int? pageIndex)
        {
            IQueryable<Message> messages = from m in Context.Messages
                                           select m;
            messages = messages
                .Where(m => m.CommentID == id)
                .OrderBy(m => m.CommentID);

            int pageSize = 5;
            Messages = await PaginatedList<Message>.CreateAsync(
                messages, pageIndex ?? 1, pageSize);
        }

        public async Task<IActionResult> OnPostCreateMessageAsync(int id, int pageIndex)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await UserManager.GetUserAsync(User);
            if(user == null)
            {
                return Challenge();
            }
            if (user.Banned)
            {
                return Forbid();
            }

            Message.ProfileID = UserManager.GetUserId(User);

            var isAuthorized = await AuthorizationService.AuthorizeAsync(User, Message,
                                        MessageOperations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            Message.CommentID = id;
            Message.UpdateTime = DateTime.Now;

            Context.Messages.Add(Message);
            await Context.SaveChangesAsync();

            return RedirectToPage("/Comments/Details", "OnGet", new { id, pageIndex });
        }
    }
}
