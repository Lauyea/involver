using Involver.Common;
using Involver.Data;
using Involver.Models.ArticleModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile
{
    [AllowAnonymous]
    public class ViewedArticlesModel : DI_BasePageModel
    {
        public ViewedArticlesModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }
        [TempData]
        public string StatusMessage { get; set; }

        public Models.Profile Profile { get; set; }

        public string UserID { get; set; }

        public ICollection<Article> Articles { get; set; }

        private async Task LoadAsync(string id)
        {
            UserID = _userManager.GetUserId(User);

            Profile = await _context.Profiles
                .Where(p => p.ProfileID == id)
                .Include(p => p.ViewedArticles)
                .FirstOrDefaultAsync();

            Articles = Profile.ViewedArticles.OrderByDescending(a => a.ArticleViewers.FirstOrDefault()?.ViewDate).ToList();
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            await LoadAsync(id);
            if (Profile != null)
            {
                return Page();
            }
            else
            {
                return NotFound();
            }
        }
    }
}
