using Involver.Common;
using DataAccess.Data;
using DataAccess.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile
{
    [AllowAnonymous]
    public class ViewesModel : DI_BasePageModel
    {
        public ViewesModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public DataAccess.Models.Profile Profile { get; set; }

        public string UserID { get; set; }

        public ICollection<Novel> Novels { get; set; }

        private async Task LoadAsync(string id)
        {
            UserID = _userManager.GetUserId(User);

            Profile = await _context.Profiles
                .Where(p => p.ProfileID == id)
                .Include(p => p.ViewedNovels)
                .FirstOrDefaultAsync();

            Novels = Profile.ViewedNovels.OrderByDescending(a => a.NovelViewers.FirstOrDefault()?.ViewDate).ToList();
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
