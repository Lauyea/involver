using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Common;
using Involver.Services.NotificationSetterService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Feedbacks
{
    [AllowAnonymous]
    public class DetailsModel : DI_BasePageModel
    {
        private readonly INotificationSetter _notificationSetter;

        public DetailsModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager,
        INotificationSetter notificationSetter)
        : base(context, authorizationService, userManager)
        {
            _notificationSetter = notificationSetter;
        }


        public Article Feedback { get; set; }
        public string UserID { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, int? pageIndex)
        {
            if (id == null)
            {
                return NotFound();
            }

            Feedback = await _context.Articles
                .Include(f => f.Profile)
                .FirstOrDefaultAsync(f => f.ArticleID == id);

            if (Feedback == null)
            {
                return NotFound();
            }

            //Comments = Context.Comments.Where(c => c.FeedbackID == id).OrderByDescending(c => c.UpdateTime).ToList();

            var isAuthorized = User.IsInRole(Authorization.Article.Articles.ArticleManagersRole) ||
                           User.IsInRole(Authorization.Article.Articles.ArticleAdministratorsRole);

            UserID = _userManager.GetUserId(User);

            if (!isAuthorized
                && UserID != Feedback.ProfileID
                && Feedback.Block)
            {
                return Forbid();
            }

            if (!string.IsNullOrEmpty(ToastsJson))
            {
                Toasts = System.Text.Json.JsonSerializer.Deserialize<List<Toast>>(ToastsJson);
            }

            var toasts = await Helpers.AchievementHelper.ReadFeedbackAsync(_context, UserID);

            Toasts.AddRange(toasts);

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id, bool block)
        {
            var feedback = await _context.Articles.FirstOrDefaultAsync(
                                                      m => m.ArticleID == id);

            if (feedback == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(User, feedback,
                                        ArticleOperations.Block);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }
            feedback.Block = block;
            _context.Articles.Update(feedback);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostAcceptAsync(int id)
        {
            var feedback = await _context.Articles
                .FirstOrDefaultAsync(m => m.ArticleID == id);

            Profile profile = await _context.Profiles.FirstOrDefaultAsync(p => p.ProfileID == feedback.ProfileID);

            if (feedback == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(User, feedback,
                                        ArticleOperations.Block);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }
            feedback.Accept = true;
            _context.Articles.Update(feedback);
            profile.VirtualCoins += 50;//回報錯誤與採納意見獲得虛擬In幣50枚
            await _context.SaveChangesAsync();

            var toasts = await Helpers.AchievementHelper.AcceptCountAsync(_context, feedback.ProfileID);

            // Set notification
            var url = $"{Request.Scheme}://{Request.Host}/Feedbacks/Details?id={id}";

            await _notificationSetter.ForFeedbackAcceptAsync(feedback.Title, feedback.ProfileID, url, toasts);

            return RedirectToPage("./Index");
        }
    }
}