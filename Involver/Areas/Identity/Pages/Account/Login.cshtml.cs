using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;

using DataAccess.Data;
using DataAccess.Models.AchievementModel;

using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class LoginModel(SignInManager<InvolverUser> signInManager,
    ILogger<LoginModel> logger,
    ApplicationDbContext context,
    IAchievementService achievementService) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; }

    public IList<AuthenticationScheme> ExternalLogins { get; set; }

    public string ReturnUrl { get; set; }

    [TempData]
    public string ErrorMessage { get; set; }

    [TempData]
    public string StatusMessage { get; set; }

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

        [Display(Name = "記住我？")]
        public bool RememberMe { get; set; }
    }

    public async Task OnGetAsync(string returnUrl = null)
    {
        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        // Clear the existing external cookie to ensure a clean login process
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        ReturnUrl = returnUrl;
    }

    public async Task<IActionResult> OnPostAsync(string returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        if (ModelState.IsValid)
        {
            // This doesn't count login failures towards account lockout
            // To enable password failures to trigger account lockout, set lockoutOnFailure: true
            var result = await signInManager.PasswordSignInAsync(Input.Username, Input.Password, Input.RememberMe, lockoutOnFailure: false);
            if (result.Succeeded)
            {
                logger.LogInformation("User logged in.");
                await ProfileOperation();
                return LocalRedirect(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, Input.RememberMe });
            }
            if (result.IsLockedOut)
            {
                logger.LogWarning("User account locked out.");
                return RedirectToPage("./Lockout");
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
                return Page();
            }
        }

        // If we got this far, something failed, redisplay form
        ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        return Page();
    }

    private async Task ProfileOperation()
    {
        DataAccess.Models.Profile userProfile = await context
                                .Profiles
                                .Where(p => p.UserName == Input.Username)
                                .Include(p => p.Achievements)
                                .Include(p => p.Missions)
                                .FirstOrDefaultAsync();
        //確認成就表的存在，若無則建一個給Profile
        userProfile.Achievements ??= [];

        if (userProfile != null)
        {
            userProfile.LastTimeLogin = DateTime.Now;//登入時間
            if (!userProfile.Missions.DailyLogin)
            {
                userProfile.Missions.DailyLogin = true;//每日登入任務
                userProfile.VirtualCoins += 5;
                StatusMessage = "每日登入 已完成，獲得5 虛擬In幣。";
            }

            List<Toast> toasts = await Helpers.AchievementHelper.CheckGradeAsync(context, userProfile.ProfileID, userProfile.EnrollmentDate);

            // TODO: Beta時間登入即可解鎖成就，之後這個要刪掉
            toasts.AddRange(await achievementService.BeBetaInvolverAsync(userProfile.ProfileID));

            if (toasts.Count > 0)
            {
                TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
            }
        }
    }
}