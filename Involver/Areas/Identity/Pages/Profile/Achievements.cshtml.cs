using Involver.Common;
using Involver.Data;
using Involver.Models.AchievementModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile
{
    [AllowAnonymous]
    public class AchievementsModel : DI_BasePageModel
    {
        public AchievementsModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }
        public Models.Profile Profile { get; set; }

        public List<Achievement> Achievements { get; set; }
        public string UserID { get; set; }

        public bool ProfileOwner { get; set; } = false;

        private async Task LoadAsync(string id)
        {
            UserID = _userManager.GetUserId(User);
            if (UserID == id)
            {
                ProfileOwner = true;
            }
            Profile = await _context.Profiles
                .Include(p => p.Achievements)
                    //.ThenInclude(a => a.ProfileAchievements) �o��n���լO�_�u���ݭn
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();
            Achievements = Profile.Achievements.ToList();
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            //TODO �i��Ѧ�steam�����N��ܤ覡
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