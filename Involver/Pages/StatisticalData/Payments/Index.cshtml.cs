using DataAccess.Data;
using DataAccess.Models.StatisticalData;

using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.StatisticalData.Payments;

public class IndexModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public IList<Payment> Payments { get; set; }

    /// <summary>
    /// 付款紀錄列表，依付款時間由新到舊
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var isAuthorized = User.IsInRole(Authorization.Payment.Payments.PaymentManagersRole) ||
                       User.IsInRole(Authorization.Payment.Payments.PaymentAdministratorsRole);

        if (!isAuthorized)
        {
            return Forbid();
        }

        // 原本依字串排序，日期格式不一致時順序會錯；改用解析後的時間，無法解析的排在最後
        Payments = await Context.Payments
            .OrderByDescending(p => p.PaymentTime)
            .ThenByDescending(p => p.PaymentID)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        return Page();
    }
}