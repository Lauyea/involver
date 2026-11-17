using DataAccess.Data;

using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.StatisticalData;

/// <summary>
/// 統計資料：所有人都看得到總用戶數；完整用戶列表僅限用戶或金流的管理者。
/// （「關於 Involver」頁面有公開連結到這裡，所以頁面本身維持匿名可看）
/// </summary>
[AllowAnonymous]
public class IndexModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    /// <summary>
    /// 用戶列表的一列
    /// </summary>
    /// <param name="ProfileID">用戶 ID</param>
    /// <param name="UserName">用戶名</param>
    public record ProfileItem(string ProfileID, string UserName);

    /// <summary>
    /// 總用戶數
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// 所有用戶（依註冊時間倒序）；非管理者為 null
    /// </summary>
    public List<ProfileItem> Profiles { get; set; }

    /// <summary>
    /// 載入用戶列表
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>頁面</returns>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        TotalCount = await Context.Profiles.CountAsync(cancellationToken);

        var isAuthorized = User.IsInRole(Authorization.Profile.Profiles.ProfileManagersRole)
            || User.IsInRole(Authorization.Profile.Profiles.ProfileAdministratorsRole)
            || User.IsInRole(Authorization.Payment.Payments.PaymentManagersRole)
            || User.IsInRole(Authorization.Payment.Payments.PaymentAdministratorsRole);

        if (!isAuthorized)
        {
            return Page();
        }

        // 只取列表需要的欄位，不載入整筆 Profile
        Profiles = await Context.Profiles
            .OrderByDescending(p => p.EnrollmentDate)
            .Select(p => new ProfileItem(p.ProfileID, p.UserName))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return Page();
    }
}
