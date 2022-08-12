using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Involver.Data;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore;
using Involver.Models.AchievementModel;
using Involver.Common;
using System.Text.Json;

namespace Involver.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class LoginModel : PageModel
    {
        private readonly UserManager<InvolverUser> _userManager;
        private readonly SignInManager<InvolverUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ApplicationDbContext Context;

        public LoginModel(SignInManager<InvolverUser> signInManager, 
            ILogger<LoginModel> logger,
            UserManager<InvolverUser> userManager,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            Context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public string ReturnUrl { get; set; }

        [TempData]
        public string ErrorMessage { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [TempData]
        public string ToastsJson { get; set; }

        public List<Toast> Toasts { get; set; } = new List<Toast>();

        public class InputModel
        {
            [Required]
            [Display(Name = "用戶名")]
            [DataType(DataType.Text)]
            public string Username { get; set; }

            [Required]
            [Display(Name = "密碼")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "記住我?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl = returnUrl ?? Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");

            if (ModelState.IsValid)
            {
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Input.Username, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    await ProfileOperation();
                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            return Page();
        }

        private async Task ProfileOperation()
        {
            Models.Profile UserProfile = await Context
                                    .Profiles
                                    .Where(p => p.UserName == Input.Username)
                                    .Include(p => p.Achievements)
                                    .Include(p => p.Missions)
                                    .FirstOrDefaultAsync();
            //確認成就表的存在，若無則建一個給Profile
            if (UserProfile.Achievements == null)
            {
                UserProfile.Achievements = new List<Achievement>();
            }
            if (UserProfile != null)
            {
                UserProfile.LastTimeLogin = DateTime.Now;//登入時間
                if (!UserProfile.Missions.DailyLogin)
                {
                    UserProfile.Missions.DailyLogin = true;//每日登入任務
                    UserProfile.VirtualCoins += 5;
                    StatusMessage = "每日登入 已完成，獲得5 虛擬In幣。";
                }

                var toasts = await Helpers.AchievementHelper.CheckGradeAsync(Context, UserProfile.ProfileID, UserProfile.EnrollmentDate);

                Toasts.AddRange(toasts);

                // TODO: Beta時間登入即可解鎖成就，之後這個要刪掉
                toasts = await Helpers.AchievementHelper.BeBetaInvolverAsync(Context, UserProfile.ProfileID);

                Toasts.AddRange(toasts);

                ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);
            }
        }
    }
}
