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
    public class InteractionModel : DI_BasePageModel
    {
        public InteractionModel(
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
        public ICollection<Comment> Comments { get; set; }

        private async Task LoadAsync(string id)
        {
            UserID = _userManager.GetUserId(User);
            Profile = await _context.Profiles
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();
            Comments = await _context.Comments
                .Include(c => c.Profile)
                .Include(c => c.Novel)
                .Include(c => c.Episode)
                .Include(c => c.Announcement)
                .Include(c => c.Feedback)
                .Include(c => c.Article)
                .Where(c => c.ProfileID == id)
                .Where(c => c.Content != "")
                .OrderByDescending(c => c.UpdateTime)
                .Take(100)
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
