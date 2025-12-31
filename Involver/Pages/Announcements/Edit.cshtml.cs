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

namespace Involver.Pages.Announcements;

public class EditModel(
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

        Announcement = await Context.Articles.FirstOrDefaultAsync(m => m.ArticleID == id);

        if (Announcement == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                              User, Announcement,
                                              ArticleOperations.Update);
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
        if (Announcement.Content?.Length > Parameters.ArticleLength)
        {
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Fetch data from DB to get OwnerID.
        var announcement = await Context
            .Articles
            .FirstOrDefaultAsync(f => f.ArticleID == id);

        if (announcement == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                              User, announcement,
                                              ArticleOperations.Update);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        var tempUser = await Context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Announcement.ProfileID);
        announcement.UpdateTime = DateTime.Now;
        announcement.Content = Announcement.Content;
        announcement.Title = Announcement.Title;

        try
        {
            await Context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!AnnouncementExists(Announcement.ArticleID))
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

    private bool AnnouncementExists(int id)
    {
        return Context.Articles.Any(e => e.ArticleID == id);
    }
}