﻿using Involver.Authorization.Feedback;
using Involver.Common;
using Involver.Data;
using Involver.Models.FeedbackModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            Feedback = await Context.Feedbacks.FirstOrDefaultAsync(m => m.FeedbackID == id);

            if (Feedback == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
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
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Fetch data from DB to get OwnerID.
            var feedback = await Context
                .Feedbacks.AsNoTracking()
                .FirstOrDefaultAsync(f => f.FeedbackID == id);

            if (feedback == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                     User, feedback,
                                                     FeedbackOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            Feedback.OwnerID = feedback.OwnerID;
            Feedback.Block = feedback.Block;
            var tempUser = await Context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Feedback.OwnerID);
            Feedback.OwnerName = tempUser.UserName;
            Feedback.UpdateTime = DateTime.Now;

            Context.Attach(Feedback).State = EntityState.Modified;

            try
            {
                await Context.SaveChangesAsync();
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

            return RedirectToPage("./Index");
        }

        private bool FeedbackExists(int id)
        {
            return Context.Feedbacks.Any(e => e.FeedbackID == id);
        }
    }
}
