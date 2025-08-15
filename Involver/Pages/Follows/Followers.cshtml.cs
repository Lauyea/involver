using Involver.Common;
using DataAccess.Data;
using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Follows
{
    [AllowAnonymous]
    public class FollowersModel : DI_BasePageModel
    {
        public FollowersModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public int NovelID { get; set; }

        public ICollection<Follow> Follows { get; set; }

        private async Task LoadAsync(int id)
        {
            Follows = await _context.Follows
                .Include(f => f.Follower)
                .Where(f => f.NovelID == id)
                .OrderByDescending(f => f.UpdateTime)
                .ToListAsync();
        }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            NovelID = id;
            await LoadAsync(id);
            if (Follows != null)
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
