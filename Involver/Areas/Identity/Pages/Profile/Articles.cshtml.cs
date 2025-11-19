using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.ArticleModel;
using Involver.Common;
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

        public DataAccess.Models.Profile Profile { get; set; }
        public List<Article> Articles { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            Profile = await _context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);

            if (Profile == null)
            {
                return NotFound();
            }

            Articles = await _context.Articles
                .Where(a => a.ProfileID == id && a.IsDeleted == false)
                .OrderByDescending(a => a.UpdateTime)
                .Take(Parameters.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnGetLoadMoreAsync(string id, int pageIndex)
        {
            var articles = await _context.Articles
                .Where(a => a.ProfileID == id)
                .OrderByDescending(a => a.UpdateTime)
                .Skip((pageIndex - 1) * Parameters.PageSize)
                .Take(Parameters.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return Partial("_ArticleListPartial", articles);
        }
    }
}