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
    public class MessagesModel : DI_BasePageModel
    {
        public MessagesModel(
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
        public ICollection<Message> Messages { get; set; }

        private async Task LoadAsync(string id)
        {
            UserID = _userManager.GetUserId(User);
            Profile = await _context.Profiles
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();
            Messages = await _context.Messages
                .Include(m => m.Comment)
                .Where(c => c.ProfileID == id)
                .OrderByDescending(m => m.UpdateTime)
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
