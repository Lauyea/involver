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

namespace Involver.Pages.Articles;

public class DeleteModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager, 
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    [BindProperty]
    public Article Article { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Article = await Context.Articles.Include(a => a.Profile).FirstOrDefaultAsync(m => m.ArticleID == id);

        if (Article == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                             User, Article,
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

        Article = await Context.Articles.FindAsync(id);

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                             User, Article,
                                             ArticleOperations.Delete);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        var comments = from c in Context.Comments
                       where c.ArticleID == id
                       select c;
        var involvings = from i in Context.Involvings
                         where i.ArticleID == id
                         select i;

        if (Article != null)
        {
            //_context.Involvings.RemoveRange(involvings);
            //_context.Comments.RemoveRange(comments);
            //_context.Articles.Remove(Article);

            Article.IsDeleted = true;

            await Context.SaveChangesAsync();
        }

        var toasts = await AchievementService.FirstTimeDeleteAsync(Article.ProfileID);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        return RedirectToPage("./Index");
    }
}