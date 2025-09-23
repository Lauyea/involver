using DataAccess.Data;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Announcements
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
        public Article Announcement { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Announcement = await _context.Articles.FirstOrDefaultAsync(a => a.ArticleID == id);

            if (Announcement == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                 User, Announcement,
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

            Announcement = await _context.Articles.FindAsync(id);

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                 User, Announcement,
                                                 ArticleOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }


            var comments = from c in _context.Comments
                           where c.ArticleID == id
                           select c;

            if (Announcement != null)
            {
                _context.Comments.RemoveRange(comments);
                _context.Articles.Remove(Announcement);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}