using Involver.Common;
using Involver.Data;
using Involver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

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

        public Models.Profile Profile { get; set; }

        public string UserID { get; set; }
        public List<Notification> Notifications { get; set; }

        private async Task LoadAsync(string id)
        {
            UserID = _userManager.GetUserId(User);

            Profile = await _context.Profiles
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();

            Notifications = await _context.Notifications
                .Where(n => n.ProfileID == UserID)
                .OrderByDescending(n => n.CreatedDate)
                //.Take(100) // TODO 暫時先不取一定數量，因為routine work 會把一個月前的刪掉
                .ToListAsync().ConfigureAwait(false);
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
