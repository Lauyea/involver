using Involver.Common;
using Involver.Data;
using Involver.Models.ArticleModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile
{
    [AllowAnonymous]
    public class ArticlesModel : DI_BasePageModel
    {
        public ArticlesModel(
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

        public bool ProfileOwner { get; set; } = false;
        public ICollection<Article> Articles { get; set; }

        private async Task LoadAsync(string id)
        {
            UserID = _userManager.GetUserId(User);
            if (UserID == id)
            {
                ProfileOwner = true;
            }
            Profile = await _context.Profiles
                .Include(p => p.Articles)
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();
            Articles = Profile.Articles.OrderByDescending(a => a.UpdateTime).Take(100).ToList();
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
