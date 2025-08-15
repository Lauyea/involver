using Involver.Authorization.Comment;
using Involver.Common;
using DataAccess.Data;
using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Comments
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
        public Comment Comment { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Comment = await _context.Comments.FirstOrDefaultAsync(m => m.CommentID == id);

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                 User, Comment,
                                                 CommentOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            if (Comment == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id, string from, int? fromID)
        {
            if (id == null)
            {
                return NotFound();
            }

            Comment = await _context.Comments.FindAsync(id);

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                 User, Comment,
                                                 CommentOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            if (Comment != null)
            {
                _context.Comments.Remove(Comment);
                await _context.SaveChangesAsync();
            }
            if(fromID != null)
            {
                return RedirectToPage("/" + from + "/Details", new { id = fromID });
            }
            return RedirectToPage("./Index");
        }
    }
}
