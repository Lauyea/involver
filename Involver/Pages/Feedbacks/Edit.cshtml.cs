using System.Text.Json;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Feedbacks;

public class EditModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    [BindProperty]
    public Article Feedback { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Feedback = await Context.Articles.FirstOrDefaultAsync(m => m.ArticleID == id);

        if (Feedback == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                              User, Feedback,
                                              ArticleOperations.Update);
        return !isAuthorized.Succeeded ? Forbid() : Page();
    }

    // To protect from overposting attacks, please enable the specific properties you want to bind to, for
    // more details see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (Feedback.Content?.Length > Parameters.ArticleLength)
        {
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Fetch data from DB to get OwnerID.
        var feedback = await Context
            .Articles
            .FirstOrDefaultAsync(f => f.ArticleID == id);

        if (feedback == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                 User, feedback,
                                                 ArticleOperations.Update);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        var tempUser = await Context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Feedback.ProfileID);
        feedback.UpdateTime = DateTime.Now;
        feedback.Title = Feedback.Title;
        feedback.Content = Feedback.Content;

        try
        {
            await Context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!FeedbackExists(Feedback.ArticleID))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        var toasts = await AchievementService.FirstTimeEditAsync(Feedback.ProfileID);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        return RedirectToPage("./Index");
    }

    private bool FeedbackExists(int id)
    {
        return Context.Articles.Any(e => e.ArticleID == id);
    }
}