using Involver.Common;
using DataAccess.Data;
using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile
{
    [AllowAnonymous]
    public class InvolversModel : DI_BasePageModel
    {
        public InvolversModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public DataAccess.Models.Profile Profile { get; set; }

        public string UserID { get; set; }

        public string TimeSpan { get; set; }
        public ICollection<Involving> Involvers { get; set; }

        private async Task LoadAsync(string id)
        {
            UserID = _userManager.GetUserId(User);
            Profile = await _context.Profiles
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();
            if (TimeSpan == "TotalTime")
            {
                Involvers = await _context.Involvings
                .Include(i => i.Involver)
                .Where(i => i.ProfileID == id)
                .OrderByDescending(i => i.TotalValue)
                .Take(100)
                .ToListAsync();
            }
            else if (TimeSpan == "Monthly")
            {
                Involvers = await _context.Involvings
                .Include(i => i.Involver)
                .Where(i => i.ProfileID == id)
                .OrderByDescending(i => i.MonthlyValue)
                .Take(100)
                .ToListAsync();
            }
            else
            {
                Involvers = await _context.Involvings
                .Include(i => i.Involver)
                .Where(i => i.ProfileID == id)
                .OrderByDescending(i => i.LastTime)
                .Take(100)
                .ToListAsync();
            }
        }

        public async Task<IActionResult> OnGetAsync(string id, string TimeSpan)
        {
            this.TimeSpan = TimeSpan;
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
