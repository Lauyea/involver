using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Data;

using Involver.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Involver.Areas.Identity.Pages.Account.Manage
{
    public class DeletePersonalDataModel : PageModel
    {
        private readonly UserManager<InvolverUser> _userManager;
        private readonly SignInManager<InvolverUser> _signInManager;
        private readonly ILogger<DeletePersonalDataModel> _logger;
        private readonly IUserAccountService _userAccountService;

        public DeletePersonalDataModel(
            UserManager<InvolverUser> userManager,
            SignInManager<InvolverUser> signInManager,
            ILogger<DeletePersonalDataModel> logger,
            IUserAccountService userAccountService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _userAccountService = userAccountService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }
        }

        public bool RequirePassword { get; set; }

        public async Task<IActionResult> OnGet()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            RequirePassword = await _userManager.HasPasswordAsync(user);
            if (RequirePassword)
            {
                if (!await _userManager.CheckPasswordAsync(user, Input.Password))
                {
                    ModelState.AddModelError(string.Empty, "Incorrect password.");
                    return Page();
                }
            }

            var userId = await _userManager.GetUserIdAsync(user);

            // 不實際刪除資料列：作品、留言與 In 幣流水帳都關聯到這個帳號，流水帳依規定必須保留。
            // 改為清除個人資料並鎖定帳號，之後無法再登入
            await _userAccountService.AnonymizeAsync(user);

            await _signInManager.SignOutAsync();

            _logger.LogInformation("User with ID '{UserId}' deleted (anonymized) themselves.", userId);

            return Redirect("~/");
        }
    }
}
