using DataAccess.Data;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Announcements;

public class DeleteModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    [BindProperty]
    public Article Announcement { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Announcement = await Context.Articles.Include(a => a.Profile).FirstOrDefaultAsync(a => a.ArticleID == id);

        if (Announcement == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                             User, Announcement,
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

        Announcement = await Context.Articles.FindAsync(id);

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                             User, Announcement,
                                             ArticleOperations.Delete);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }


        var comments = from c in Context.Comments
                       where c.ArticleID == id
                       select c;

        if (Announcement != null)
        {
            Context.Comments.RemoveRange(comments);
            Context.Articles.Remove(Announcement);
            await Context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}