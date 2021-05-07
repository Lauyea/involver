using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Involver.Data;
using Involver.Models.FeedbackModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Authorization.Feedback;

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

            Feedback = await Context.Feedbacks.FirstOrDefaultAsync(m => m.FeedbackID == id);

            if (Feedback == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
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

            Feedback = await Context.Feedbacks.Include(f => f.Comments).FirstOrDefaultAsync(f => f.FeedbackID == id);

            var comments = from c in Context.Comments
                           where c.FeedbackID == id
                           select c;

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                 User, Feedback,
                                                 FeedbackOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }
            
            if (Feedback != null)
            {
                Context.Comments.RemoveRange(comments);
                Context.Feedbacks.Remove(Feedback);
                await Context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
