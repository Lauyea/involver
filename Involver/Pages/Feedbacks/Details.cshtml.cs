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
using Involver.Common;

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

            Feedback = await _context.Feedbacks
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

            UserID = _userManager.GetUserId(User);

            if (!isAuthorized
                && UserID != Feedback.OwnerID
                && Feedback.Block)
            {
                return Forbid();
            }

            if (!string.IsNullOrEmpty(ToastsJson))
            {
                Toasts = System.Text.Json.JsonSerializer.Deserialize<List<Toast>>(ToastsJson);
            }

            return Page();
        }

        private async Task SetComments(int? id, int? pageIndex)
        {
            IQueryable<Comment> comments = from c in _context.Comments
                                           select c;
            comments = comments
                .Include(c => c.Agrees)
                .Include(c => c.Messages.OrderByDescending(m => m.UpdateTime).Take(5))
                    .ThenInclude(c => c.Profile)
                .Include(c => c.Profile)
                .Include(c => c.Dices)
                .Where(c => c.FeedbackID == id)
                .OrderBy(c => c.CommentID);

            
            Comments = await PaginatedList<Comment>.CreateAsync(
                comments, pageIndex ?? 1, Parameters.CommetPageSize);
        }

        public async Task<IActionResult> OnPostAsync(int id, bool block)
        {
            var feedback = await _context.Feedbacks.FirstOrDefaultAsync(
                                                      m => m.FeedbackID == id);

            if (feedback == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(User, feedback,
                                        FeedbackOperations.Block);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }
            feedback.Block = block;
            _context.Feedbacks.Update(feedback);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostAcceptAsync(int id)
        {
            var feedback = await _context.Feedbacks
                .FirstOrDefaultAsync(m => m.FeedbackID == id);

            Profile profile = await _context.Profiles.FirstOrDefaultAsync(p => p.ProfileID == feedback.OwnerID);

            if (feedback == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(User, feedback,
                                        FeedbackOperations.Block);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }
            feedback.Accept = true;
            _context.Feedbacks.Update(feedback);
            profile.VirtualCoins += 50;//回報錯誤與採納意見獲得虛擬In幣50枚
            _context.Attach(profile).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
