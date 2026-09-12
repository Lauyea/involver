using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Threading.Tasks;
using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using Involver.Common;
using Involver.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using PreviewEntity = DataAccess.Models.Preview;

namespace Involver.Pages.Functions
{
    [AllowAnonymous]
    public class PreviewModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IPasswordHasher<PreviewEntity> _passwordHasher;

        public PreviewModel(ApplicationDbContext context, IPasswordHasher<PreviewEntity> passwordHasher)
        {
            _context = context;
            _passwordHasher = passwordHasher;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public bool IsCreated { get; set; }
        public string PreviewUrl { get; set; } = string.Empty;
        public string ReadPasswordDisplay { get; set; } = string.Empty;
        public string ExpiresAtDisplay { get; set; } = string.Empty;

        public class InputModel
        {
            [Required(ErrorMessage = "必須要有標題")]
            [StringLength(Parameters.SmallContentLength, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 2)]
            [Display(Name = "標題")]
            public string Title { get; set; } = string.Empty;

            [Required(ErrorMessage = "必須要有小說內容")]
            [StringLength(Parameters.ArticleLength, ErrorMessage = "{0} 最多只能有 {1} 個字元")]
            [Display(Name = "小說內容")]
            public string ContentHtml { get; set; } = string.Empty;

            [Required(ErrorMessage = "請設定閱讀密碼")]
            [StringLength(100, ErrorMessage = "{0} 長度至少為 {2} 個字元", MinimumLength = 4)]
            [DataType(DataType.Password)]
            [Display(Name = "閱讀密碼")]
            public string ReadPassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "請設定撤銷密碼")]
            [StringLength(100, ErrorMessage = "{0} 長度至少為 {2} 個字元", MinimumLength = 4)]
            [DataType(DataType.Password)]
            [Display(Name = "撤銷密碼")]
            public string RevokePassword { get; set; } = string.Empty;

            [Required(ErrorMessage = "請設定有效期限")]
            [Display(Name = "有效期限")]
            [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
            public DateTime ExpiresAt { get; set; }
        }

        public void OnGet()
        {
            if (ViewData != null) ViewData["Title"] = "小說試閱";
            var now = DateTime.Now;
            Input.ExpiresAt = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, DateTimeKind.Local).AddDays(7);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ViewData != null) ViewData["Title"] = "小說試閱";

            if (!string.IsNullOrEmpty(Input.ReadPassword) && Input.ReadPassword == Input.RevokePassword)
            {
                ModelState.AddModelError("Input.RevokePassword", "撤銷密碼不可與閱讀密碼相同，避免閱覽者取得閱讀密碼後可以撤銷試閱。");
            }

            DateTime localExpiry = Input.ExpiresAt.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(Input.ExpiresAt, DateTimeKind.Local)
                : Input.ExpiresAt;

            DateTime cleanLocalExpiry = new DateTime(localExpiry.Year, localExpiry.Month, localExpiry.Day, localExpiry.Hour, localExpiry.Minute, 0, DateTimeKind.Local);
            DateTime expiresAtUtc = cleanLocalExpiry.ToUniversalTime();

            if (expiresAtUtc <= DateTime.UtcNow)
            {
                ModelState.AddModelError("Input.ExpiresAt", "有效期限必須是未來的時間。");
            }

            if (expiresAtUtc > DateTime.UtcNow.AddYears(1))
            {
                ModelState.AddModelError("Input.ExpiresAt", "有效期限最長不可超過 1 年。");
            }

            if (Input.ContentHtml?.Length > Parameters.ArticleLength)
            {
                ModelState.AddModelError("Input.ContentHtml", $"內容長度不可超過 {Parameters.ArticleLength} 字元。");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            byte[] randomBytes = RandomNumberGenerator.GetBytes(32);
            string token = WebEncoders.Base64UrlEncode(randomBytes);

            var preview = new PreviewEntity
            {
                Token = token,
                Title = Input.Title,
                ContentHtml = CustomHtmlSanitizer.SanitizeHtml(Input.ContentHtml),
                PasswordHash = string.Empty,
                RevokePasswordHash = string.Empty,
                ExpiresAt = expiresAtUtc,
                CreatedAt = DateTime.UtcNow
            };

            preview.PasswordHash = _passwordHasher.HashPassword(preview, Input.ReadPassword);
            preview.RevokePasswordHash = _passwordHasher.HashPassword(preview, Input.RevokePassword);

            _context.Previews.Add(preview);
            await _context.SaveChangesAsync();

            IsCreated = true;
            PreviewUrl = $"{Request.Scheme}://{Request.Host}/Preview/{token}";
            ReadPasswordDisplay = Input.ReadPassword;
            ExpiresAtDisplay = expiresAtUtc.ToLocalTime().ToString("yyyy/MM/dd HH:mm");

            return Page();
        }
    }
}
