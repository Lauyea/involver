using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Involver.Authorization.Novel;
using Involver.Data;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile
{
    [AllowAnonymous]
    public class CreationsModel : DI_BasePageModel
    {
        public CreationsModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        [TempData]
        public string StatusMessage { get; set; }

        public Models.Profile Profile { get; set; }

        public ICollection<Novel> Novels { get; set; }

        public string UserID { get; set; }

        public bool ProfileOwner { get; set; } = false;

        private async Task LoadAsync(string id)
        {
            UserID = UserManager.GetUserId(User);
            if (UserID == id)
            {
                ProfileOwner = true;
            }
            Profile = await Context.Profiles
                .Include(p => p.Novels)
                .Where(p => p.ProfileID == id)
                .FirstOrDefaultAsync();
            Novels = Profile.Novels.OrderByDescending(n => n.NovelID).ToList();
            //TODO 可能要Lazy loading or 分頁
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
