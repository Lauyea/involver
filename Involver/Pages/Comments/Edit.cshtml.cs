using Involver.Authorization.Comment;
using Involver.Common;
using Involver.Data;
using Involver.Helpers;
using Involver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Comments
{
    public class EditModel : DI_BasePageModel
    {
        public EditModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            UserManager<InvolverUser> userManager)
            : base(context, authorizationService, userManager)
        {
        }

        [BindProperty]
        public Comment Comment { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Comment = await _context.Comments
                .Include(c => c.Announcement)
                .Include(c => c.Episode)
                .Include(c => c.Feedback)
                .Include(c => c.Novel)
                .Include(c => c.Article)
                .Include(c => c.Dices)
                .Include(c => c.Profile).FirstOrDefaultAsync(m => m.CommentID == id);

            if (Comment == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                  User, Comment,
                                                  CommentOperations.Update);
            if (!isAuthorized.Succeeded || Comment.Dices.Count() > 0)
            {
                return Forbid();
            }

            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int id, string from, int? fromID)
        {
            if(Comment.Content?.Length > Parameters.CommentLength)
            {
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Fetch data from DB to get OwnerID.
            var comment = await _context
                .Comments
                .FirstOrDefaultAsync(c => c.CommentID == id);

            if (comment == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                  User, comment,
                                                  CommentOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            isAuthorized = await _authorizationService.AuthorizeAsync(
                                                  User, comment,
                                                  CommentOperations.Block);

            if (!isAuthorized.Succeeded && comment.Block)
            {
                ErrorMessage = "這篇評論已被封鎖，其他讀者看不到這篇評論，如有問題請回報管理者";
                return Page();
            }

            comment.UpdateTime = DateTime.Now;
            if(from == Parameters.Feedbacks)
            {
                comment.FeedbackID = fromID;
            }
            else if(from == Parameters.Announcements)
            {
                comment.AnnouncementID = fromID;
            }
            else if (from == Parameters.Articles)
            {
                comment.ArticleID = fromID;
            }
            else if (from == Parameters.Novels)
            {
                comment.NovelID = fromID;
            }
            else if (from == Parameters.Episodes)
            {
                comment.EpisodeID = fromID;
            }

            comment.Content = Comment.Content;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CommentExists(Comment.CommentID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            var toasts = await AchievementHelper.FirstTimeEditAsync(_context, Comment.ProfileID);

            Toasts.AddRange(toasts);

            ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

            return RedirectToPage("/" + from + "/Details", "OnGet", new { id = fromID }, "CommentHead");
        }

        private bool CommentExists(int id)
        {
            return _context.Comments.Any(e => e.CommentID == id);
        }
    }
}
