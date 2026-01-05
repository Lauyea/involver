using System.Text.Json;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Feedbacks;

public class CreateModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
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

        var user = await UserManager.GetUserAsync(User);

        if (user.Banned)
        {
            return Forbid();
        }

        Feedback.ProfileID = UserManager.GetUserId(User);

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
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
                var tempUser = await Context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Feedback.ProfileID);
                emptyFeedback.ProfileID = Feedback.ProfileID;
                Context.Articles.Add(emptyFeedback);
                await Context.SaveChangesAsync();

                var toasts = await AchievementService.FeedbackCountAsync(Feedback.ProfileID);

                if (toasts.Count > 0)
                {
                    TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
                }

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