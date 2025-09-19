using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

using DataAccess.Data;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Involver.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<InvolverUser> _signInManager;
        private readonly UserManager<InvolverUser> _userManager;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public RegisterModel(
            UserManager<InvolverUser> userManager,
            SignInManager<InvolverUser> signInManager,
            ILogger<RegisterModel> logger,
            ApplicationDbContext context,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "用戶名")]
            public string UserName { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "電子郵件")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 6)]
            [DataType(DataType.Password, ErrorMessage = "必須包含英文大小寫與特殊符號以及數字")]
            [Display(Name = "密碼")]
            public string Password { get; set; }

            [DataType(DataType.Password, ErrorMessage = "必須包含英文大小寫與特殊符號以及數字")]
            [Display(Name = "確認密碼")]
            [Compare("Password", ErrorMessage = "密碼與確認密碼並不一致")]
            public string ConfirmPassword { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var user = new InvolverUser { UserName = Input.UserName, Email = Input.Email };
                //設定每個用戶獨立郵件地址
                _userManager.Options.User.RequireUniqueEmail = true;
                DataAccess.Models.Profile profile = await _context.Profiles.Where(p => p.UserName == user.UserName).FirstOrDefaultAsync();
                if (profile != null)
                {
                    ModelState.AddModelError("Input.UserName", "已經有同樣的用戶名");
                    return Page();
                }
                var result = await _userManager.CreateAsync(user, Input.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    var callbackUrl = Url.Page(
                        "/Account/ConfirmEmail",
                        pageHandler: null,
                        values: new { area = "Identity", userId = user.Id, code = code },
                        protocol: Request.Scheme);

                    await _emailSender.SendEmailAsync(Input.Email, "確認你的Email",
                        $"請<a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>點擊此連結</a>以確認你的Email");

                    if (_userManager.Options.SignIn.RequireConfirmedAccount)
                    {
                        return RedirectToPage("RegisterConfirmation", new { email = Input.Email });
                    }
                    else
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return LocalRedirect(returnUrl);
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}