using Involver.Authorization.Feedback;
using Involver.Common;
using Involver.Data;
using Involver.Models.FeedbackModel;
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
        public Feedback Feedback { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Feedback = await _context.Feedbacks.FirstOrDefaultAsync(m => m.FeedbackID == id);

            if (Feedback == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                 User, Feedback,
                                                 FeedbackOperations.Delete);
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

            Feedback = await _context.Feedbacks.Include(f => f.Comments).FirstOrDefaultAsync(f => f.FeedbackID == id);

            var comments = from c in _context.Comments
                           where c.FeedbackID == id
                           select c;

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                 User, Feedback,
                                                 FeedbackOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }
            
            if (Feedback != null)
            {
                _context.Comments.RemoveRange(comments);
                _context.Feedbacks.Remove(Feedback);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
