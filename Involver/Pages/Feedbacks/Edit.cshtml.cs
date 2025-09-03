using Involver.Authorization.Feedback;
using Involver.Common;
using DataAccess.Data;
using DataAccess.Models.FeedbackModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataAccess.Common;

namespace Involver.Pages.Feedbacks
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
                                                  FeedbackOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int id)
        {
            if(Feedback.Content?.Length > Parameters.ArticleLength)
            {
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Fetch data from DB to get OwnerID.
            var feedback = await _context
                .Feedbacks
                .FirstOrDefaultAsync(f => f.FeedbackID == id);

            if (feedback == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                     User, feedback,
                                                     FeedbackOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var tempUser = await _context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Feedback.OwnerID);
            feedback.OwnerName = tempUser.UserName;
            feedback.UpdateTime = DateTime.Now;
            feedback.Title = Feedback.Title;
            feedback.Content = Feedback.Content;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!FeedbackExists(Feedback.FeedbackID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            var toasts = await Helpers.AchievementHelper.FirstTimeEditAsync(_context, Feedback.OwnerID);

            Toasts.AddRange(toasts);

            ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

            return RedirectToPage("./Index");
        }

        private bool FeedbackExists(int id)
        {
            return _context.Feedbacks.Any(e => e.FeedbackID == id);
        }
    }
}
