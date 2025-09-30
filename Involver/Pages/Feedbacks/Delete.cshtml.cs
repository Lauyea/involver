using DataAccess.Data;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Common;
using Involver.Helpers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Feedbacks
{
    public class DeleteModel : DI_BasePageModel
    {

        public DeleteModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        [BindProperty]
        public Article Feedback { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Feedback = await _context.Articles.Include(f=> f.Profile).FirstOrDefaultAsync(m => m.ArticleID == id);

            if (Feedback == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                 User, Feedback,
                                                 ArticleOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Feedback = await _context.Articles.Include(f => f.Comments).FirstOrDefaultAsync(f => f.ArticleID == id);

            var comments = from c in _context.Comments
                           where c.ArticleID == id
                           select c;

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                 User, Feedback,
                                                 ArticleOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            if (Feedback != null)
            {
                _context.Comments.RemoveRange(comments);
                _context.Articles.Remove(Feedback);
                await _context.SaveChangesAsync();
            }

            var toasts = await AchievementHelper.FirstTimeDeleteAsync(_context, Feedback.ProfileID);

            Toasts.AddRange(toasts);

            ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

            return RedirectToPage("./Index");
        }
    }
}