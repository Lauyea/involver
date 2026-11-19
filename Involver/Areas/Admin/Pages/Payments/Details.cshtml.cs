using System.ComponentModel.DataAnnotations;

using DataAccess.Data;
using DataAccess.Models.Dashboard;
using DataAccess.Models.StatisticalData;

using Involver.Authorization.Admin;
using Involver.Services;
using Involver.Services.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using PaymentRoles = Involver.Authorization.Payment.Payments;

namespace Involver.Areas.Admin.Pages.Payments;

/// <summary>
/// 訂單詳細資料：作廢與 In 幣沖正。金流紀錄不能刪除，所有操作都寫入管理者操作紀錄
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="coinService">In 幣異動服務</param>
/// <param name="auditService">管理者操作紀錄</param>
/// <param name="userManager">帳號管理</param>
[Authorize(Policy = AdminPolicies.Payments)]
public class DetailsModel(
    ApplicationDbContext context,
    ICoinService coinService,
    IAdminAuditService auditService,
    UserManager<InvolverUser> userManager) : AdminPageModel
{
    /// <summary>
    /// 目前的使用者能否沖正（只限金流管理員，且不能是付款用戶本人）
    /// </summary>
    public bool CanAdjust => User.IsInRole(PaymentRoles.PaymentAdministratorsRole)
        && Payment?.InvolverID != null
        && Payment.InvolverID != userManager.GetUserId(User);

    /// <summary>
    /// 作廢表單
    /// </summary>
    public class VoidInput
    {
        /// <summary>作廢原因</summary>
        [Required(ErrorMessage = "請填寫作廢原因")]
        [StringLength(200)]
        [Display(Name = "作廢原因")]
        public string Reason { get; set; }
    }

    /// <summary>
    /// 沖正表單
    /// </summary>
    public class AdjustInput
    {
        /// <summary>調整金額，正數為補發、負數為扣回</summary>
        [Range(-1000000, 1000000)]
        [Display(Name = "調整金額")]
        public decimal Amount { get; set; }

        /// <summary>說明</summary>
        [Required(ErrorMessage = "請填寫說明")]
        [StringLength(150)]
        [Display(Name = "說明")]
        public string Note { get; set; }
    }

    /// <summary>
    /// 訂單 ID
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    /// <summary>
    /// 作廢表單
    /// </summary>
    [BindProperty]
    public VoidInput Void { get; set; } = new();

    /// <summary>
    /// 沖正表單
    /// </summary>
    [BindProperty]
    public AdjustInput Adjust { get; set; } = new();

    /// <summary>
    /// 訂單
    /// </summary>
    public Payment Payment { get; private set; }

    /// <summary>
    /// 付款用戶名
    /// </summary>
    public string UserName { get; private set; }

    /// <summary>
    /// 這筆訂單相關的 In 幣流水帳（儲值與沖正）
    /// </summary>
    public List<CoinTransaction> Transactions { get; private set; } = [];

    /// <summary>
    /// 流水帳關聯 ID（金流商:特店交易編號）
    /// </summary>
    public string ReferenceId => Payment == null ? null : $"{PaymentProvider.ECPay}:{Payment.MerchantTradeNo}";

    /// <summary>
    /// 載入訂單
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>頁面；找不到時 404</returns>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        return await LoadAsync(cancellationToken) ? Page() : NotFound();
    }

    /// <summary>
    /// 作廢訂單（只做標記，不異動 In 幣；需要退回時另外沖正）
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostVoidAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (!TryValidateModel(Void, nameof(Void)))
        {
            return await LoadAsync(cancellationToken) ? Page() : NotFound();
        }

        var payment = await context.Payments.FirstOrDefaultAsync(p => p.PaymentID == Id, cancellationToken);
        if (payment == null)
        {
            return NotFound();
        }

        if (payment.IsVoided)
        {
            StatusMessage = "錯誤：訂單已經作廢。";
            return RedirectToPage(new { Id });
        }

        payment.IsVoided = true;
        payment.VoidedAt = DateTime.UtcNow;
        payment.VoidReason = Void.Reason.Trim();

        await auditService.AddAsync(
            AdminAuditAction.PaymentVoided, "Payment", payment.PaymentID.ToString(),
            before: $"{payment.MerchantTradeNo}：{payment.TradeAmt:N0} 元，RtnCode {payment.RtnCode}",
            after: $"作廢：{payment.VoidReason}",
            cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        StatusMessage = "訂單已作廢。若需要退回或扣回 In 幣，請另外沖正。";
        return RedirectToPage(new { Id });
    }

    /// <summary>
    /// 以管理員調整（AdminAdjust）沖正付款用戶的實體 In 幣
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostAdjustAsync(CancellationToken cancellationToken)
    {
        ModelState.Clear();
        if (!TryValidateModel(Adjust, nameof(Adjust)) || Adjust.Amount == 0)
        {
            if (Adjust.Amount == 0)
            {
                ModelState.AddModelError($"{nameof(Adjust)}.{nameof(AdjustInput.Amount)}", "調整金額不可為 0");
            }

            return await LoadAsync(cancellationToken) ? Page() : NotFound();
        }

        // 沖正會直接產生或扣除實體 In 幣，只限金流管理員
        if (!User.IsInRole(PaymentRoles.PaymentAdministratorsRole))
        {
            return Forbid();
        }

        var payment = await context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.PaymentID == Id, cancellationToken);
        if (payment == null)
        {
            return NotFound();
        }

        if (string.IsNullOrEmpty(payment.InvolverID))
        {
            StatusMessage = "錯誤：這筆訂單沒有付款用戶，無法沖正。";
            return RedirectToPage(new { Id });
        }

        if (payment.InvolverID == userManager.GetUserId(User))
        {
            StatusMessage = "錯誤：不能沖正自己的訂單，請由其他管理員處理。";
            return RedirectToPage(new { Id });
        }

        var referenceId = $"{PaymentProvider.ECPay}:{payment.MerchantTradeNo}";

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // 補發時，這筆訂單累計入帳（儲值與先前的調整）不能超過訂單金額
        if (Adjust.Amount > 0)
        {
            var credited = await context.CoinTransactions
                .Where(t => t.ProfileId == payment.InvolverID
                    && t.CoinType == CoinType.Real
                    && t.ReferenceType == CoinReferenceType.Payment
                    && t.ReferenceId == referenceId)
                .SumAsync(t => t.Amount, cancellationToken);

            if (credited + Adjust.Amount > payment.TradeAmt)
            {
                StatusMessage = $"錯誤：補發後這筆訂單累計入帳 {credited + Adjust.Amount:N0}，超過訂單金額 {payment.TradeAmt:N0}。";
                return RedirectToPage(new { Id });
            }
        }

        var info = new CoinTransactionInfo(
            CoinTransactionType.AdminAdjust,
            CoinReferenceType.Payment,
            referenceId,
            Note: $"訂單沖正：{Adjust.Note.Trim()}");

        if (!await coinService.AdjustAsync(payment.InvolverID, CoinType.Real, Adjust.Amount, info, cancellationToken))
        {
            StatusMessage = "錯誤：找不到用戶，或扣回後實體 In 幣會變成負數。";
            return RedirectToPage(new { Id });
        }

        await auditService.AddAsync(
            AdminAuditAction.CoinAdjusted, "Payment", payment.PaymentID.ToString(),
            before: $"{payment.MerchantTradeNo}：用戶 {payment.InvolverID}",
            after: $"實體 In 幣 {Adjust.Amount:+#,0.##;-#,0.##}：{Adjust.Note.Trim()}",
            cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        StatusMessage = $"已沖正 {Adjust.Amount:+#,0.##;-#,0.##} 實體 In 幣。";
        return RedirectToPage(new { Id });
    }

    private async Task<bool> LoadAsync(CancellationToken cancellationToken)
    {
        Payment = await context.Payments.AsNoTracking().FirstOrDefaultAsync(p => p.PaymentID == Id, cancellationToken);
        if (Payment == null)
        {
            return false;
        }

        UserName = await context.Profiles
            .Where(p => p.ProfileID == Payment.InvolverID)
            .Select(p => p.UserName)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var referenceId = ReferenceId;
        Transactions = await context.CoinTransactions
            .Where(t => t.ReferenceType == CoinReferenceType.Payment && t.ReferenceId == referenceId)
            .OrderBy(t => t.Id)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return true;
    }
}
