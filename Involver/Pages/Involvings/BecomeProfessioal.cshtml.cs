using Involver.Common;
using Involver.Data;
using Involver.Models;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace Involver.Pages.Involvings
{
    public class BecomeProfessionalModel : DI_BasePageModel
    {
        public BecomeProfessionalModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public Profile Profile { get; set; }
        public string ProfileID { get; set; }
        public int Involvers { get; set; }
        public bool CanBeProfessional { get; set; } = false;

        private async Task LoadAsync(string id)
        {
            Profile = await _context.Profiles
                .Include(p => p.Followers)
                .Include(p => p.Achievements)
                .Include(p => p.Novels)
                    .ThenInclude(n => n.Follows)
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();
            if (Profile != null)
            {
                foreach (Follow follower in Profile.Followers)
                {
                    if (follower.ProfileMonthlyInvolver)
                    {
                        Involvers++;
                    }
                }
                foreach (Novel novel in Profile.Novels)
                {
                    foreach (Follow follow in novel.Follows)
                    {
                        if (follow.NovelMonthlyInvolver)
                        {
                            Involvers++;
                        }
                    }
                }
                if (Involvers > 100 && Profile.MonthlyCoins > 1000)
                {
                    CanBeProfessional = true;
                }
                else
                {
                    CanBeProfessional = false;
                }
            }
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            ProfileID = id;
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

        public async Task<IActionResult> OnPostAsync(string id)
        {
            ProfileID = id;
            await LoadAsync(id);
            if (CanBeProfessional)
            {
                Profile.Professional = true;

                var toasts = await Helpers.AchievementHelper.BeProfessionalAsync(_context, ProfileID);

                Toasts.AddRange(toasts);

                ToastsJson = JsonSerializer.Serialize(toasts);
            }

            return RedirectToPage("/Profile/Index", "OnGet", new { area = "Identity", id = ProfileID });
        }
    }
}
