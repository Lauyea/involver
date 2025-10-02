using DataAccess.Data;
using DataAccess.Models.ArticleModel;
using Involver.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataAccess.Common;
using Involver.Extensions;

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

        public class ViewedArticleViewModel
        {
            public int ArticleID { get; set; }
            public string Title { get; set; }
            public string Content { get; set; }
            public DateTime ViewDate { get; set; }
        }

        public DataAccess.Models.Profile Profile { get; set; }
        public List<ViewedArticleViewModel> Articles { get; set; }

        private async Task<List<ViewedArticleViewModel>> GetViewedArticles(string id, int pageIndex = 1)
        {
            var userId = _userManager.GetUserId(User);
            if (id != userId)
            {
                return new List<ViewedArticleViewModel>(); // Or handle as an error
            }

            Profile = await _context.Profiles
                .Where(p => p.ProfileID == id)
                .Include(p => p.ViewedArticles)
                .FirstOrDefaultAsync();

            var articles = Profile.ViewedArticles
                .OrderByDescending(v => v.ArticleViewers.FirstOrDefault()?.ViewDate)
                .Select(v => new ViewedArticleViewModel
                {
                    ArticleID = v.ArticleID,
                    Title = v.Title.Length < Parameters.SmallContentLength
                        ? v.Title
                        : v.Title.Substring(0, Parameters.SmallContentLength) + "...",
                    Content = v.Content.StripHTML().Length < Parameters.ContentLength
                        ? v.Content.StripHTML()
                        : v.Content.StripHTML().Substring(0, Parameters.ContentLength) + "...",
                    ViewDate = (DateTime)(v.ArticleViewers.FirstOrDefault()?.ViewDate)
                })
                .Skip((pageIndex - 1) * Parameters.PageSize)
                .Take(Parameters.PageSize)
                .ToList();

            return articles;
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            Profile = await _context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);

            if (Profile == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (id != userId)
            {
                return Forbid();
            }

            Articles = await GetViewedArticles(id);

            return Page();
        }

        public async Task<IActionResult> OnGetLoadMoreAsync(string id, int pageIndex)
        {
            var articles = await GetViewedArticles(id, pageIndex);
            return Partial("_ViewedArticleListPartial", articles);
        }
    }
}
