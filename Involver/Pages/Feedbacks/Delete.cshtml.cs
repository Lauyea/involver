using System.Text.Json;

using DataAccess.Data;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Common;
using Involver.Helpers;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Feedbacks;

public class DeleteModel(
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

        Feedback = await Context.Articles.Include(f=> f.Profile).FirstOrDefaultAsync(m => m.ArticleID == id);

        if (Feedback == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                             User, Feedback,
                                             ArticleOperations.Delete);
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

        Feedback = await Context.Articles.Include(f => f.Comments).FirstOrDefaultAsync(f => f.ArticleID == id);

        var comments = from c in Context.Comments
                       where c.ArticleID == id
                       select c;

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                             User, Feedback,
                                             ArticleOperations.Delete);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        if (Feedback != null)
        {
            Context.Comments.RemoveRange(comments);
            Context.Articles.Remove(Feedback);
            await Context.SaveChangesAsync();
        }

        var toasts = await AchievementService.FirstTimeDeleteAsync(Feedback.ProfileID);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        return RedirectToPage("./Index");
    }
}