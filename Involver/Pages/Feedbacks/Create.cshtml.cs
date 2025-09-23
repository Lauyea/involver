using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Feedbacks
{
    public class CreateModel : DI_BasePageModel
    {

        public CreateModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Article Feedback { get; set; }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (Feedback.Content?.Length > Parameters.ArticleLength)
            {
                Page();
            }

            if (!ModelState.IsValid)
            {
                // 看 ModelState 錯誤內容
                var errorList = ModelState
                    .Where(ms => ms.Value.Errors.Any())
                    .Select(ms => new {
                        Field = ms.Key,
                        Errors = ms.Value.Errors.Select(e => e.ErrorMessage).ToList()
                    }).ToList();

                return Page();
            }

            var user = await _userManager.GetUserAsync(User);

            if (user.Banned)
            {
                return Forbid();
            }

            Feedback.ProfileID = _userManager.GetUserId(User);

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                        User, Feedback,
                                                        ArticleOperations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            Article emptyFeedback =
                new Article
                {
                    Title = "temp title",
                    Content = "temp content",
                    ProfileID = Feedback.ProfileID,
                    Type = ArticleType.Feedback
                };
            try
            {
                //Protect from overposting attacks
                if (await TryUpdateModelAsync<Article>(
                    emptyFeedback,
                    "feedback",   // Prefix for form value.
                    f => f.Title, f => f.Content))
                {
                    emptyFeedback.UpdateTime = DateTime.Now;
                    var tempUser = await _context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Feedback.ProfileID);
                    emptyFeedback.ProfileID = Feedback.ProfileID;
                    _context.Articles.Add(emptyFeedback);
                    await _context.SaveChangesAsync();

                    var toasts = await Helpers.AchievementHelper.FeedbackCountAsync(_context, Feedback.ProfileID);

                    Toasts.AddRange(toasts);

                    ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

                    return RedirectToPage("./Index");
                }
            }
            catch (Exception ex)
            {
                string errormessage = ex.ToString();
            }

            return Page();
        }
    }
}