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
    public class AgreesModel : DI_BasePageModel
    {
        public AgreesModel(
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
        public ICollection<Agree> Agrees { get; set; }
        private async Task LoadAsync(string id)
        {
            UserID = UserManager.GetUserId(User);
            Profile = await Context.Profiles
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();
            Agrees = await Context.Agrees
                .Include(a => a.Comment)
                    .ThenInclude(c => c.Novel)
                .Include(a => a.Comment)
                    .ThenInclude(c => c.Episode)
                .Include(a => a.Comment)
                    .ThenInclude(c => c.Announcement)
                .Include(a => a.Comment)
                    .ThenInclude(c => c.Feedback)
                .Include(a => a.Comment)
                    .ThenInclude(c => c.Article)
                .Include(a => a.Message)
                    .ThenInclude(m => m.Comment)
                .Where(a => a.ProfileID == id)
                .OrderByDescending(a => a.UpdateTime)
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
