using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Areas.Identity.Pages.Profile;

/// <summary>
/// 舊的編輯個人檔案網址：轉到帳號管理的「個人檔案」，讓舊連結和書籤仍然有效
/// </summary>
public class ChangeProfileModel : PageModel
{
    /// <summary>
    /// 轉到帳號管理的個人檔案設定
    /// </summary>
    /// <returns>轉址</returns>
    public IActionResult OnGet() => RedirectToPage("/Account/Manage/EditProfile", new { area = "Identity" });
}
