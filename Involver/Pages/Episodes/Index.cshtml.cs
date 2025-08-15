using Involver.Common;
using DataAccess.Data;
using DataAccess.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Episodes
{
    public class IndexModel : DI_BasePageModel
    {
        public IndexModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public IList<Episode> Episode { get;set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var isAuthorized = User.IsInRole(Authorization.Comment.Comments.CommentManagersRole) ||
                          User.IsInRole(Authorization.Comment.Comments.CommentAdministratorsRole);

            if (!isAuthorized)
            {
                return Forbid();
            }

            Episode = await _context.Episodes
                .Include(e => e.Novel).ToListAsync();

            return Page();
        }
    }
}
