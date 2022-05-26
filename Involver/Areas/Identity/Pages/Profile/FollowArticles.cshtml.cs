using Involver.Common;
using Involver.Data;
using Involver.Models;
using Involver.Models.ArticleModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile
{
    [AllowAnonymous]
    public class FollowArticlesModel : DI_BasePageModel
    {
        public FollowArticlesModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        [TempData]
        public string StatusMessage { get; set; }

        public Models.Profile Profile { get; set; }
        public ICollection<Follow> Follows { get; set; }
        public ICollection<Article> Articles { get; set; } = new List<Article>();

        public string UserID { get; set; }

        private async Task LoadAsync(string id)
        {
            UserID = _userManager.GetUserId(User);

            Profile = await _context.Profiles
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();

            Follows = await _context.Follows
                .Include(f => f.Profile)
                    .ThenInclude(p => p.Articles)
                .Where(f => f.FollowerID == id)
                .ToListAsync();
            foreach(Follow follow in Follows)
            {
                if(follow.Profile != null)
                {
                    foreach (Article article in follow.Profile.Articles)
                    {
                        Articles.Add(article);
                    }
                }
            }
            Articles.OrderByDescending(a => a.UpdateTime).Take(100);
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
