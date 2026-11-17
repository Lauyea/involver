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
    IAchievementService achievementService,
    IMissionService missionService,
    IWebHostEnvironment environment) : PageModel
{
    /// <summary>
    /// 是否為開發環境；只有開發環境才會顯示並接受「以使用者 ID 登入」。
    /// </summary>
    public bool IsDevelopment => environment.IsDevelopment();
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

    public async Task<IActionResult> OnGetAsync(string returnUrl = null)
    {
        // 已登入者不需再看到登入表單，直接導回原頁（或首頁）
        if (signInManager.IsSignedIn(User))
        {
            return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/"));
        }

        if (!string.IsNullOrEmpty(ErrorMessage))
        {
            ModelState.AddModelError(string.Empty, ErrorMessage);
        }

        returnUrl ??= Url.Content("~/");

        // Clear the existing external cookie to ensure a clean login process
        await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

        ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        ReturnUrl = returnUrl;
        return Page();
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
                await ProfileOperation(Input.Username);
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

    /// <summary>
    /// 僅限開發環境：以使用者 ID（UUID）直接登入，不需密碼，方便本機測試。
    /// 非開發環境一律回傳 404。
    /// </summary>
    /// <param name="devUserId">要登入的使用者 ID。</param>
    /// <param name="returnUrl">登入後導回的網址。</param>
    public async Task<IActionResult> OnPostDevLoginAsync([FromForm] string devUserId, string returnUrl = null)
    {
        if (!IsDevelopment)
        {
            return NotFound();
        }

        returnUrl = Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Content("~/");

        // 這個 handler 不使用帳密欄位，避免其驗證錯誤混入
        ModelState.Clear();

        InvolverUser user = Guid.TryParse(devUserId?.Trim(), out Guid userGuid)
            ? await signInManager.UserManager.FindByIdAsync(userGuid.ToString())
            : null;

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "[DEV] 找不到此使用者 ID。");
            ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
            return Page();
        }

        await signInManager.SignInAsync(user, isPersistent: false);
        logger.LogWarning("[DEV] User {UserId} logged in by ID without password.", user.Id);
        await ProfileOperation(user.UserName);
        return LocalRedirect(returnUrl);
    }

    private async Task ProfileOperation(string userName)
    {
        DataAccess.Models.Profile userProfile = await context
                                .Profiles
                                .Where(p => p.UserName == userName)
                                .Include(p => p.Achievements)
                                .Include(p => p.Missions)
                                .FirstOrDefaultAsync();

        if (userProfile != null)
        {
            //確認成就表的存在，若無則建一個給Profile
            userProfile.Achievements ??= [];

            userProfile.LastTimeLogin = DateTime.Now;//登入時間
            // 每日登入任務：旗標與獎勵由任務服務原子更新
            if (await missionService.CompleteAsync(userProfile.ProfileID, MissionType.DailyLogin))
            {
                StatusMessage = "每日登入 已完成，獲得5 虛擬In幣。";
            }

            List<Toast> toasts = await achievementService.CheckGradeAsync(userProfile.ProfileID, userProfile.EnrollmentDate);

            // TODO: Beta時間登入即可解鎖成就，之後這個要刪掉
            toasts.AddRange(await achievementService.BeBetaInvolverAsync(userProfile.ProfileID));

            if (toasts.Count > 0)
            {
                TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
            }
        }
    }
}