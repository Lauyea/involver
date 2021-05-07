using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Involver.Data;
using Involver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
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

        [TempData]
        public string StatusMessage { get; set; }

        public Models.Profile Profile { get; set; }

        public string UserID { get; set; }

        public string TimeSpan { get; set; }
        public ICollection<Involving> Involvers { get; set; }

        private async Task LoadAsync(string id)
        {
            UserID = UserManager.GetUserId(User);
            Profile = await Context.Profiles
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();
            if (TimeSpan == "TotalTime")
            {
                Involvers = await Context.Involvings
                .Include(i => i.Involver)
                .Where(i => i.ProfileID == id)
                .OrderByDescending(i => i.TotalValue)
                .Take(100)
                .ToListAsync();
            }
            else if (TimeSpan == "Monthly")
            {
                Involvers = await Context.Involvings
                .Include(i => i.Involver)
                .Where(i => i.ProfileID == id)
                .OrderByDescending(i => i.MonthlyValue)
                .Take(100)
                .ToListAsync();
            }
            else
            {
                Involvers = await Context.Involvings
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
