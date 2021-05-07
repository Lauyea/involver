using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Involver.Data;
using Involver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile
{
    [AllowAnonymous]
    public class FollowCreatorsModel : DI_BasePageModel
    {
        public FollowCreatorsModel(
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

        public string UserID { get; set; }

        private async Task LoadAsync(string id)
        {
            UserID = UserManager.GetUserId(User);

            Profile = await Context.Profiles
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();

            Follows = await Context.Follows
                .Include(f => f.Profile)
                .Where(f => f.FollowerID == id)
                .OrderByDescending(f => f.UpdateTime)
                .ToListAsync();
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
