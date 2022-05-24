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
using Involver.Models;

namespace Involver.Pages.Feedbacks
{
    [AllowAnonymous]
    public class DetailsModel : DI_BasePageModel
    {
        public DetailsModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        
        public PaginatedList<Comment> Comments { get; set; }
        public Feedback Feedback { get; set; }
        public string UserID { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, int? pageIndex)
        {
            if (id == null)
            {
                return NotFound();
            }

            Feedback = await Context.Feedbacks
                //.Include(f => f.Comments)
                //    .ThenInclude(f => f.Profile)
                .FirstOrDefaultAsync(f => f.FeedbackID == id);

            if (Feedback == null)
            {
                return NotFound();
            }

            await SetComments(id, pageIndex);

            //Comments = Context.Comments.Where(c => c.FeedbackID == id).OrderByDescending(c => c.UpdateTime).ToList();

            var isAuthorized = User.IsInRole(Authorization.Feedback.Feedbacks.FeedbackManagersRole) ||
                           User.IsInRole(Authorization.Feedback.Feedbacks.FeedbackAdministratorsRole);

            UserID = UserManager.GetUserId(User);

            if (!isAuthorized
                && UserID != Feedback.OwnerID
                && Feedback.Block)
            {
                return Forbid();
            }

            return Page();
        }

        private async Task SetComments(int? id, int? pageIndex)
        {
            IQueryable<Comment> comments = from c in Context.Comments
                                           select c;
            comments = comments
                .Include(c => c.Agrees)
                .Include(c => c.Messages)
                .Include(c => c.Profile)
                .Include(c => c.Dices)
                .Where(c => c.FeedbackID == id)
                .OrderBy(c => c.CommentID);

            int pageSize = 5;
            Comments = await PaginatedList<Comment>.CreateAsync(
                comments, pageIndex ?? 1, pageSize);
        }

        public async Task<IActionResult> OnPostAsync(int id, bool block)
        {
            var feedback = await Context.Feedbacks.FirstOrDefaultAsync(
                                                      m => m.FeedbackID == id);

            if (feedback == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(User, feedback,
                                        FeedbackOperations.Block);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }
            feedback.Block = block;
            Context.Feedbacks.Update(feedback);
            await Context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostAcceptAsync(int id)
        {
            var feedback = await Context.Feedbacks
                .FirstOrDefaultAsync(m => m.FeedbackID == id);

            Profile profile = await Context.Profiles.FirstOrDefaultAsync(p => p.ProfileID == feedback.OwnerID);

            if (feedback == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(User, feedback,
                                        FeedbackOperations.Block);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }
            feedback.Accept = true;
            Context.Feedbacks.Update(feedback);
            profile.VirtualCoins += 50;//回報錯誤與採納意見獲得虛擬In幣50枚
            Context.Attach(profile).State = EntityState.Modified;
            await Context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
