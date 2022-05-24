using Involver.Common;
using Involver.Data;
using Involver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile
{
    [AllowAnonymous]
    public class MissionsModel : DI_BasePageModel
    {
        public MissionsModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }
        public Models.Profile Profile { get; set; }

        public Missions Missions { get; set; }
        public string UserID { get; set; }

        public bool ProfileOwner { get; set; } = false;

        private async Task LoadAsync(string id)
        {
            UserID = UserManager.GetUserId(User);
            if (UserID == id)
            {
                ProfileOwner = true;
            }
            Profile = await Context.Profiles
                .Include(p => p.Missions)
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();
            Missions = Profile.Missions;
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
