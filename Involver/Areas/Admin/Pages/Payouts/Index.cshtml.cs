using Involver.Authorization.Admin;
using Involver.Services.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Admin.Pages.Payouts;

/// <summary>
/// 分潤清單：每位創作者的待撥、已撥金額；標記已撥款會寫入管理者操作紀錄
/// </summary>
/// <param name="payoutService">撥款服務</param>
[Authorize(Policy = AdminPolicies.Payouts)]
public class IndexModel(IPayoutService payoutService) : AdminPageModel
{
    /// <summary>
    /// 是否只列有待撥金額的創作者
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool PendingOnly { get; set; } = true;

    /// <summary>
    /// 撥款狀態
    /// </summary>
    public List<CreatorPayoutSummary> Summaries { get; private set; } = [];

    /// <summary>
    /// 待撥金額合計
    /// </summary>
    public decimal TotalPending { get; private set; }

    /// <summary>
    /// 已撥金額合計
    /// </summary>
    public decimal TotalPaid { get; private set; }

    /// <summary>
    /// 本月累計中的創作者收入合計
    /// </summary>
    public decimal TotalCurrent { get; private set; }

    /// <summary>
    /// 載入撥款狀態
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var all = await payoutService.GetSummariesAsync(cancellationToken);
        TotalPending = all.Sum(s => s.PendingAmount);
        TotalPaid = all.Sum(s => s.PaidAmount);
        TotalCurrent = all.Sum(s => s.CurrentMonthIncome);
        Summaries = PendingOnly ? all.Where(s => s.PendingAmount > 0).ToList() : all;
    }

    /// <summary>
    /// 把創作者所有待撥月份標記為已撥款
    /// </summary>
    /// <param name="profileId">創作者 ID</param>
    /// <param name="note">備註（例如轉帳編號）</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到列表</returns>
    public async Task<IActionResult> OnPostMarkPaidAsync(string profileId, string note, CancellationToken cancellationToken)
    {
        PayoutMarkResult result;
        try
        {
            result = await payoutService.MarkPaidAsync(profileId, note, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // 兩位管理者同時標記，或重複送出：唯一索引擋下了重複撥款
            StatusMessage = "錯誤：這些月份剛剛已被標記為已撥款，請重新整理確認。";
            return RedirectToPage(new { PendingOnly });
        }

        StatusMessage = result.Months == 0
            ? "錯誤：這位創作者沒有待撥的月份。"
            : $"已標記 {result.Months} 個月、共 {result.Amount:N0} 元為已撥款。";

        return RedirectToPage(new { PendingOnly });
    }
}
