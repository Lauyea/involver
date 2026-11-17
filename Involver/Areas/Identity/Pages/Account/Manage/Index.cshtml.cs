using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Data;

using Involver.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Involver.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<InvolverUser> _userManager;
        private readonly SignInManager<InvolverUser> _signInManager;
        private readonly ApplicationDbContext Context;
        private readonly IUserAccountService _userAccountService;

        public IndexModel(
            UserManager<InvolverUser> userManager,
            SignInManager<InvolverUser> signInManager,
            ApplicationDbContext context,
            IUserAccountService userAccountService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            Context = context;
            _userAccountService = userAccountService;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public InvolverUser user { get; set; }

        public DataAccess.Models.Profile Profile { get; set; }

        public class InputModel
        {
            [DataType(DataType.Text)]
            [Display(Name = "用戶名")]
            public string UserName { get; set; }

            [Phone]
            [Display(Name = "手機號碼")]
            public string PhoneNumber { get; set; }
        }

        private async Task LoadAsync(InvolverUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Input = new InputModel
            {
                UserName = userName,
                PhoneNumber = phoneNumber
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }
            Profile = await Context.Profiles.Where(p => p.ProfileID == user.Id).FirstOrDefaultAsync();

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            Profile = await Context.Profiles.Where(p => p.ProfileID == user.Id).FirstOrDefaultAsync();
            if (Profile == null)
            {
                return NotFound($"Unable to load profile with ID '{user.Id}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            var userName = await _userManager.GetUserNameAsync(user);
            if (Input.UserName != userName && Input.UserName != null)
            {
                // 畫面只在可改名時開放輸入，伺服器端也要檢查，避免直接 POST 改名
                if (!Profile.CanChangeUserName)
                {
                    ModelState.AddModelError("Input.UserName", "用戶名只能變更一次");
                    return Page();
                }

                // AspNetUsers 與 Profiles 的用戶名一律經由同一個服務一起變更
                var error = await _userAccountService.ChangeUserNameAsync(user, Input.UserName);
                if (error != null)
                {
                    ModelState.AddModelError("Input.UserName", error);
                    return Page();
                }
            }

            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    var userId = await _userManager.GetUserIdAsync(user);
                    throw new InvalidOperationException($"Unexpected error occurred setting phone number for user with ID '{userId}'.");
                }
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "你的個人資料已經更新";
            return RedirectToPage();
        }
    }
}