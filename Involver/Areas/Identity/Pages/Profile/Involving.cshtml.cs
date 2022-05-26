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
    public class InvolvingModel : DI_BasePageModel
    {
        public InvolvingModel(
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

        public ICollection<Involving> Involvings { get; set; }
        public string TimeSpan { get; set; }

        private async Task LoadAsync(string id)
        {
            UserID = _userManager.GetUserId(User);
            Profile = await _context.Profiles
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();
            if(TimeSpan == "TotalTime")
            {
                Involvings = await _context.Involvings
                .Include(i => i.Profile)
                .Include(i => i.Novel)
                .Include(i => i.Article)
                .Where(i => i.InvolverID == id)
                .OrderByDescending(i => i.TotalValue)
                .Take(100)
                .ToListAsync();
            }
            else if (TimeSpan == "Monthly")
            {
                Involvings = await _context.Involvings
                .Include(i => i.Profile)
                .Include(i => i.Novel)
                .Include(i => i.Article)
                .Where(i => i.InvolverID == id)
                .OrderByDescending(i => i.MonthlyValue)
                .Take(100)
                .ToListAsync();
            }
            else
            {
                Involvings = await _context.Involvings
                .Include(i => i.Profile)
                .Include(i => i.Novel)
                .Include(i => i.Article)
                .Where(i => i.InvolverID == id)
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
