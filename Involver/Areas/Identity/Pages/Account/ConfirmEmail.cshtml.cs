using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Involver.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<InvolverUser> _userManager;
        private ApplicationDbContext Context;

        public ConfirmEmailModel(UserManager<InvolverUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            Context = context;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            StatusMessage = result.Succeeded ? "感謝確認你的Email" : "Error 確認Emil發生錯誤";
            //外部登入者可以變更用戶名一次
            Models.Profile Profile;

            Profile = new Models.Profile
            {
                ProfileID = user.Id,
                UserName = user.UserName,
                RealCoins = 0,
                VirtualCoins = 300,
                EnrollmentDate = DateTime.Now,
                LastTimeLogin = DateTime.Now,
                Professioal = false,
                Prime = false,
                Banned = false,
                Missions = new Models.Missions(),
                Achievements = new Models.Achievements(),
                CanChangeUserName = user.PasswordHash == null
            };
            
            Context.Profiles.Add(Profile);
            await Context.SaveChangesAsync();
            
            return Page();
        }
    }
}
