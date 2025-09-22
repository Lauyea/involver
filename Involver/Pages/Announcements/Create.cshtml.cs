using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;

using Involver.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Announcements
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
            var isAuthorized = User.IsInRole(Authorization.Article.Articles.ArticleManagersRole) ||
                           User.IsInRole(Authorization.Article.Articles.ArticleAdministratorsRole);

            if (!isAuthorized)
            {
                return Forbid();
            }
            return Page();
        }

        [BindProperty]
        public Article Announcement { get; set; }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (Announcement.Content?.Length > Parameters.ArticleLength)
            {
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            Announcement.ProfileID = _userManager.GetUserId(User);

            var isAuthorized = User.IsInRole(Authorization.Article.Articles.ArticleManagersRole) ||
                           User.IsInRole(Authorization.Article.Articles.ArticleAdministratorsRole);

            if (!isAuthorized)
            {
                return Forbid();
            }


            Article emptyAnnouncement =
                new Article
                {
                    Title = "temp title",
                    Content = "temp content",
                    ProfileID = Announcement.ProfileID
                };

            //Protect from overposting attacks
            if (await TryUpdateModelAsync<Article>(
                emptyAnnouncement,
                "announcement",   // Prefix for form value.
                f => f.Title, f => f.Content))
            {
                emptyAnnouncement.UpdateTime = DateTime.Now;
                var tempUser = await _context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Announcement.ProfileID);
                emptyAnnouncement.ProfileID = Announcement.ProfileID;
                emptyAnnouncement.TotalViews = 0;
                _context.Articles.Add(emptyAnnouncement);
                await _context.SaveChangesAsync();

                return RedirectToPage("./Index");
            }

            //Context.Announcements.Add(Announcement);
            //await Context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}