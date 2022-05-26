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

        [TempData]
        public string StatusMessage { get; set; }
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
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            await LoadAsync(id);
            if(Profile != null)
            {
                Profile.Views++;
                _context.Attach(Profile).State = EntityState.Modified;
                await _context.SaveChangesAsync();
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
                StatusMessage = "You unbanned this user successly.";
            }
            else
            {
                InvolverUser.Banned = true;
                Profile.Banned = true;
                StatusMessage = "You banned this user successly.";
            }
            _context.Attach(Profile).State = EntityState.Modified;
            _context.Attach(InvolverUser).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            return Page();
        }

        public async Task<IActionResult> OnPostFollowAsync(string id)
        {
            Profile = await _context.Profiles
                .Include(p => p.Follows)
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();

            if (Profile == null)
            {
                return Page();
            }
            string UserID = _userManager.GetUserId(User);
            Follow follow = Profile.Follows.Where(f => f.FollowerID == UserID).FirstOrDefault();

            if (follow == null)
            {
                Follow newFollow = new Follow
                {
                    FollowerID = UserID,
                    ProfileID = id,
                    UpdateTime = DateTime.Now,
                    NovelMonthlyInvolver = false,
                    ProfileMonthlyInvolver = false
                };
                _context.Follows.Add(newFollow);
                await _context.SaveChangesAsync();
            }
            else
            {
                _context.Follows.Remove(follow);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index", "OnGet", new { id });
        }
    }
}
