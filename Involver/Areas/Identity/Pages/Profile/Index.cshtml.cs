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
    public class IndexModel : DI_BasePageModel
    {
        public IndexModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }
        public Models.Profile Profile { get; set; }
        public string UserID { get; set; }
        public bool ProfileOwner { get; set; } = false;
        public bool Followed { get; set; } = false;
        public InvolverUser InvolverUser { get; set; }

        private async Task LoadAsync(string id)
        {
            UserID = _userManager.GetUserId(User);
            if (UserID == id)
            {
                ProfileOwner = true;
            }
            InvolverUser = await _context.Users.Where(u => u.Id == id).FirstOrDefaultAsync();
            Profile = await _context.Profiles
                .Include(p => p.Follows)
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();
            if(Profile != null)
            {
                Follow ExistingFollow = Profile.Follows
                .Where(f => f.FollowerID == UserID)
                .FirstOrDefault();
                if (ExistingFollow != null)
                {
                    Followed = true;
                }
                else
                {
                    Followed = false;
                }
            }

            if (!string.IsNullOrEmpty(ToastsJson))
            {
                Toasts = System.Text.Json.JsonSerializer.Deserialize<List<Toast>>(ToastsJson);
            }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            await LoadAsync(id);
            if(Profile != null)
            {
                Profile.Views++;
                _context.Attach(Profile).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                var toasts = await Helpers.AchievementHelper.GetCoinsCountAsync(_context, Profile.ProfileID, Profile.VirtualCoins);

                Toasts.AddRange(toasts);

                toasts = await Helpers.AchievementHelper.CheckGradeAsync(_context, Profile.ProfileID, Profile.EnrollmentDate);

                Toasts.AddRange(toasts);

                return Page();
            }
            else
            {
                return NotFound();
            }
        }

        public async Task<IActionResult> OnPostAsync(string id)
        {
            await LoadAsync(id);
            if (Profile.Banned)
            {
                InvolverUser.Banned = false;
                Profile.Banned = false;
                StatusMessage = "You unbanned this user successfully.";
            }
            else
            {
                InvolverUser.Banned = true;
                Profile.Banned = true;
                StatusMessage = "You banned this user successfully.";
            }
            _context.Attach(Profile).State = EntityState.Modified;
            _context.Attach(InvolverUser).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            return Page();
        }
    }
}
