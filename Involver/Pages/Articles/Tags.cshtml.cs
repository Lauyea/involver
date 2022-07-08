using Involver.Common;
using Involver.Data;
using Involver.Models.ArticleModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Articles
{
    public class TagsModel : DI_BasePageModel
    {
        public TagsModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public List<ArticleTag> ArticleTags { get; set; }

        public async Task OnGetAsync()
        {
            ArticleTags = await _context.ArticleTags
                //.Include(t => t.Articles)
                .OrderByDescending(t => t.Articles.Count)
                .ToListAsync();
        }
    }
}
