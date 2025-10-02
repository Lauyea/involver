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
    public class NotificationsModel : DI_BasePageModel
    {
        public NotificationsModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            UserManager<InvolverUser> userManager)
            : base(context, authorizationService, userManager)
        {
        }

        public DataAccess.Models.Profile Profile { get; set; }
        public List<Notification> Notifications { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            Profile = await _context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);

            if (Profile == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (id != userId)
            {
                // Users can only see their own notifications
                return Forbid();
            }

            Notifications = await _context.Notifications
                .Where(n => n.ProfileID == id)
                .OrderByDescending(n => n.CreatedDate)
                .Take(Parameters.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnGetLoadMoreAsync(string id, int pageIndex)
        {
            var userId = _userManager.GetUserId(User);
            if (id != userId)
            {
                return Forbid();
            }

            var notifications = await _context.Notifications
                .Where(n => n.ProfileID == id)
                .OrderByDescending(n => n.CreatedDate)
                .Skip((pageIndex - 1) * Parameters.PageSize)
                .Take(Parameters.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return Partial("_NotificationListPartial", notifications);
        }
    }
}
