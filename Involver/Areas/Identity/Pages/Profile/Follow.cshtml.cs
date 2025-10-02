using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using Involver.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile
{
    [AllowAnonymous]
    public class FollowModel : DI_BasePageModel
    {
        public FollowModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            UserManager<InvolverUser> userManager)
            : base(context, authorizationService, userManager)
        {
        }

        public DataAccess.Models.Profile Profile { get; set; }
        public List<Follow> Follows { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            Profile = await _context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);

            if (Profile == null)
            {
                return NotFound();
            }

            Follows = await _context.Follows
                .Include(f => f.Novel)
                    .ThenInclude(n => n.Episodes)
                .Include(f => f.Novel)
                    .ThenInclude(n => n.Profile)
                .Where(f => f.FollowerID == id)
                .OrderByDescending(f => f.Novel.UpdateTime)
                .Take(Parameters.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnGetLoadMoreAsync(string id, int pageIndex)
        {
            var follows = await _context.Follows
                .Include(f => f.Novel)
                    .ThenInclude(n => n.Episodes)
                .Include(f => f.Novel)
                    .ThenInclude(n => n.Profile)
                .Where(f => f.FollowerID == id)
                .OrderByDescending(f => f.Novel.UpdateTime)
                .Skip((pageIndex - 1) * Parameters.PageSize)
                .Take(Parameters.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return Partial("_FollowListPartial", follows);
        }
    }
}
