using DataAccess.Data;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Account.Manage;

/// <summary>
/// 錢包與會員：In 幣餘額、付費會員與特約作家的狀態（原本在個人檔案的基本資料，只有本人看得到）
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="userManager">使用者管理</param>
public class WalletModel(ApplicationDbContext context, UserManager<InvolverUser> userManager) : PageModel
{
    /// <summary>
    /// 登入者的個人檔案
    /// </summary>
    public DataAccess.Models.Profile Profile { get; private set; }

    /// <summary>
    /// 顯示錢包
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>頁面</returns>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var userId = userManager.GetUserId(User);
        Profile = await context.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProfileID == userId, cancellationToken);
        if (Profile == null)
        {
            return NotFound();
        }

        return Page();
    }
}
