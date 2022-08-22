using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Involver.Data;
using Involver.Models.ArticleModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Authorization.Article;
using Involver.Common;
using Involver.Helpers;

namespace Involver.Pages.Articles
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
        public Article Article { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Article = await _context.Articles.FirstOrDefaultAsync(m => m.ArticleID == id);

            if (Article == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                 User, Article,
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

            Article = await _context.Articles.FindAsync(id);

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                 User, Article,
                                                 ArticleOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var comments = from c in _context.Comments
                           where c.ArticleID == id
                           select c;
            var involvings = from i in _context.Involvings
                             where i.ArticleID == id
                             select i;

            if (Article != null)
            {
                _context.Involvings.RemoveRange(involvings);
                _context.Comments.RemoveRange(comments);
                _context.Articles.Remove(Article);
                await _context.SaveChangesAsync();
            }

            var toasts = await AchievementHelper.FirstTimeDeleteAsync(_context, Article.ProfileID);

            Toasts.AddRange(toasts);

            ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

            return RedirectToPage("./Index");
        }
    }
}
