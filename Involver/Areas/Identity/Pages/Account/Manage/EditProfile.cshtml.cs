using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DataAccess.Common;
using DataAccess.Data;

using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Account.Manage;

/// <summary>
/// 編輯個人檔案的公開資料：頭像、橫幅、自我介紹（原本在個人檔案的 ChangeProfile）。只能編輯自己的
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="userManager">使用者管理</param>
/// <param name="achievementService">成就（第一次寫自我介紹）</param>
public class EditProfileModel(
    ApplicationDbContext context,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService) : PageModel
{
    /// <summary>
    /// 表單欄位
    /// </summary>
    public class InputModel
    {
        /// <summary>
        /// 自我介紹
        /// </summary>
        [StringLength(Parameters.ProfileIntroLength, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 10)]
        [Display(Name = "自我介紹")]
        public string Introduction { get; set; }

        /// <summary>
        /// 頭像圖片網址
        /// </summary>
        [Url(ErrorMessage = "請輸入完整的網址（https:// 開頭）")]
        [StringLength(Parameters.ImageUrlLength)]
        [Display(Name = "頭像圖片網址")]
        public string ImageUrl { get; set; }

        /// <summary>
        /// 橫幅圖片網址
        /// </summary>
        [Url(ErrorMessage = "請輸入完整的網址（https:// 開頭）")]
        [StringLength(Parameters.ImageUrlLength)]
        [Display(Name = "橫幅圖片網址")]
        public string BannerImageUrl { get; set; }
    }

    /// <summary>
    /// 儲存後的提示
    /// </summary>
    [TempData]
    public string StatusMessage { get; set; }

    /// <summary>
    /// 表單欄位
    /// </summary>
    [BindProperty]
    public InputModel Input { get; set; } = new();

    /// <summary>
    /// 登入者的 ProfileID（連到個人檔案）
    /// </summary>
    public string ProfileId { get; private set; }

    /// <summary>
    /// 顯示表單
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>頁面</returns>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var profile = await FindProfileAsync(cancellationToken);
        if (profile == null)
        {
            return NotFound();
        }

        Input = new InputModel
        {
            Introduction = profile.Introduction,
            ImageUrl = profile.ImageUrl,
            BannerImageUrl = profile.BannerImageUrl,
        };
        return Page();
    }

    /// <summary>
    /// 儲存
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>成功時回到這一頁並顯示提示；驗證失敗時顯示錯誤</returns>
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var profile = await FindProfileAsync(cancellationToken);
        if (profile == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        profile.Introduction = string.IsNullOrWhiteSpace(Input.Introduction) ? null : Input.Introduction;
        profile.ImageUrl = string.IsNullOrWhiteSpace(Input.ImageUrl) ? null : Input.ImageUrl.Trim();
        profile.BannerImageUrl = string.IsNullOrWhiteSpace(Input.BannerImageUrl) ? null : Input.BannerImageUrl.Trim();
        await context.SaveChangesAsync(cancellationToken);
        StatusMessage = "個人檔案已更新";

        // 自傳作者成就要有寫自我介紹才給，只改頭像或橫幅不算
        if (profile.Introduction != null)
        {
            var toasts = await achievementService.BeAutobiographerAsync(profile.ProfileID);
            if (toasts.Count > 0)
            {
                TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
            }
        }

        return RedirectToPage();
    }

    /// <summary>
    /// 讀取登入者的個人檔案
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>個人檔案；找不到時為 null</returns>
    private async Task<DataAccess.Models.Profile> FindProfileAsync(CancellationToken cancellationToken)
    {
        ProfileId = userManager.GetUserId(User);
        return ProfileId == null
            ? null
            : await context.Profiles.FirstOrDefaultAsync(p => p.ProfileID == ProfileId, cancellationToken);
    }
}
