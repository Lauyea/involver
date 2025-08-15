using Involver.Common;
using DataAccess.Data;
using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Feed
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

        public List<Notification> Notifications { get; set; }

        public string UserId { get; set; }

        public int Count { get; set; } = 0;

        public async Task<IActionResult> OnGetAsync(string userId)
        {
            if(userId == null)
            {
                return Page();
            }

            UserId = userId;

            Notifications = await _context.Notifications
                .Where(n => n.ProfileID == userId)
                .OrderByDescending(n => n.CreatedDate)
                .Take(10)
                .ToListAsync().ConfigureAwait(false);

            Count = Notifications.Where(n => n.IsRead == false).Count();

            return Page();
        }

        /// <summary>
        /// 設定通知已讀
        /// </summary>
        /// <param name="userId"></param>
        /// <returns></returns>
        public async Task OnPostReadAsync(string userId)
        {
            var notifications = await _context.Notifications.Where(n => n.ProfileID == userId).ToListAsync();

            foreach(var notification in notifications)
            {
                notification.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }
    }
}
