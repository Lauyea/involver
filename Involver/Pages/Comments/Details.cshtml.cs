using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DataAccess.Data;
using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Authorization.Message;
using Involver.Common;
using Involver.Services.NotificationSetterService;
using DataAccess.Common;

namespace Involver.Pages.Comments
{
    [AllowAnonymous]
    public class DetailModel : DI_BasePageModel
    {
        private readonly INotificationSetter _notificationSetter;

        public DetailModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            UserManager<InvolverUser> userManager,
            INotificationSetter notificationSetter)
            : base(context, authorizationService, userManager)
        {
            _notificationSetter = notificationSetter;
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

            Comment = await _context.Comments
                .Include(c => c.Announcement)
                .Include(c => c.Article)
                .Include(c => c.Episode)
                .Include(c => c.Feedback)
                .Include(c => c.Novel)
                .Include(c => c.Dices)
                .Include(c => c.Profile).FirstOrDefaultAsync(m => m.CommentID == id);

            if (Comment.Announcement != null)
            {
                PreviousComment = await _context.Comments
                .Where(c => c.AnnouncementID == Comment.Announcement.AnnouncementID)
                .Where(c => c.CommentID < id)
                .OrderByDescending(c => c.CommentID)
                .FirstOrDefaultAsync();

                NextComment = await _context.Comments
                    .Where(c => c.AnnouncementID == Comment.Announcement.AnnouncementID)
                    .Where(c => c.CommentID > id)
                    .OrderBy(c => c.CommentID)
                    .FirstOrDefaultAsync();
            }

            if (Comment.Article != null)
            {
                PreviousComment = await _context.Comments
                .Where(c => c.ArticleID == Comment.Article.ArticleID)
                .Where(c => c.CommentID < id)
                .OrderByDescending(c => c.CommentID)
                .FirstOrDefaultAsync();

                NextComment = await _context.Comments
                    .Where(c => c.ArticleID == Comment.Article.ArticleID)
                    .Where(c => c.CommentID > id)
                    .OrderBy(c => c.CommentID)
                    .FirstOrDefaultAsync();
            }

            if (Comment.Episode != null)
            {
                PreviousComment = await _context.Comments
                .Where(c => c.EpisodeID == Comment.Episode.EpisodeID)
                .Where(c => c.CommentID < id)
                .OrderByDescending(c => c.CommentID)
                .FirstOrDefaultAsync();

                NextComment = await _context.Comments
                    .Where(c => c.EpisodeID == Comment.Episode.EpisodeID)
                    .Where(c => c.CommentID > id)
                    .OrderBy(c => c.CommentID)
                    .FirstOrDefaultAsync();
            }

            if (Comment.Feedback != null)
            {
                PreviousComment = await _context.Comments
                .Where(c => c.FeedbackID == Comment.Feedback.FeedbackID)
                .Where(c => c.CommentID < id)
                .OrderByDescending(c => c.CommentID)
                .FirstOrDefaultAsync();

                NextComment = await _context.Comments
                    .Where(c => c.FeedbackID == Comment.Feedback.FeedbackID)
                    .Where(c => c.CommentID > id)
                    .OrderBy(c => c.CommentID)
                    .FirstOrDefaultAsync();
            }

            if (Comment.Novel != null)
            {
                PreviousComment = await _context.Comments
                .Where(c => c.NovelID == Comment.Novel.NovelID)
                .Where(c => c.CommentID < id)
                .OrderByDescending(c => c.CommentID)
                .FirstOrDefaultAsync();

                NextComment = await _context.Comments
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

            if (!string.IsNullOrEmpty(ToastsJson))
            {
                Toasts = System.Text.Json.JsonSerializer.Deserialize<List<Toast>>(ToastsJson);
            }

            return Page();
        }

        private async Task SetMessages(int? id, int? pageIndex)
        {
            IQueryable<Message> messages = from m in _context.Messages
                                           select m;
            messages = messages
                .Where(m => m.CommentID == id)
                .OrderBy(m => m.CommentID);

            
            Messages = await PaginatedList<Message>.CreateAsync(
                messages, pageIndex ?? 1, Parameters.MessagePageSize);
        }

        public async Task<IActionResult> OnPostCreateMessageAsync(int id, int pageIndex)
        {
            if (!ModelState.IsValid)
            {
                var errorMessage = string.Join("; ",
                    ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));

                StatusMessage = $"Error 輸入錯誤：{errorMessage}";
                return RedirectToPage("/Comments/Details", new { id, pageIndex });
            }

            var user = await _userManager.GetUserAsync(User);
            if(user == null)
            {
                return Challenge();
            }
            if (user.Banned)
            {
                return Forbid();
            }

            Message.ProfileID = _userManager.GetUserId(User);

            var isAuthorized = await _authorizationService.AuthorizeAsync(User, Message,
                                        MessageOperations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            Message.CommentID = id;
            Message.UpdateTime = DateTime.Now;

            _context.Messages.Add(Message);
            await _context.SaveChangesAsync();

            // Set notification
            var url = $"{Request.Scheme}://{Request.Host}/Comments/Details?id={id}";

            await _notificationSetter.ForMessageAsync(id, Message.ProfileID, Message.Content, url);

            return RedirectToPage("/Comments/Details", "OnGet", new { id, pageIndex });
        }
    }
}
