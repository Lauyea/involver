using System.Globalization;
using System.Text;

using DataAccess.Data;
using DataAccess.Models.StatisticalData;
using DataAccess.Services;

using Involver.Authorization.Admin;
using Involver.Helpers;
using Involver.Models.ViewModels.Dashboard;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Admin.Pages.Payments;

/// <summary>
/// 訂單的付款狀態篩選
/// </summary>
public enum PaymentStatusFilter
{
    /// <summary>全部</summary>
    All,

    /// <summary>付款成功</summary>
    Succeeded,

    /// <summary>付款失敗</summary>
    Failed,

    /// <summary>模擬付款</summary>
    Simulated,

    /// <summary>已作廢</summary>
    Voided,
}

/// <summary>
/// 訂單列表：KPI、日期區間與付款狀態篩選、分頁、匯出 CSV。金流紀錄不提供刪除
/// </summary>
/// <param name="context">資料庫內容</param>
[Authorize(Policy = AdminPolicies.Payments)]
public class IndexModel(ApplicationDbContext context) : AdminPageModel
{
    /// <summary>
    /// 每頁筆數
    /// </summary>
    public const int PageSize = 30;

    /// <summary>
    /// 訂單列表的一列
    /// </summary>
    public class PaymentRow
    {
        /// <summary>訂單 ID</summary>
        public int PaymentId { get; init; }

        /// <summary>付款時間（無法解析時用訂單成立時間）</summary>
        public DateTime? Time { get; init; }

        /// <summary>綠界回傳的原始付款時間</summary>
        public string RawTime { get; init; }

        /// <summary>用戶 ID</summary>
        public string UserId { get; init; }

        /// <summary>用戶名</summary>
        public string UserName { get; init; }

        /// <summary>特店交易編號</summary>
        public string MerchantTradeNo { get; init; }

        /// <summary>綠界交易編號</summary>
        public string TradeNo { get; init; }

        /// <summary>金額</summary>
        public int Amount { get; init; }

        /// <summary>綠界回傳的交易狀態</summary>
        public int RtnCode { get; init; }

        /// <summary>綠界回傳的交易訊息</summary>
        public string RtnMsg { get; init; }

        /// <summary>是否為模擬付款</summary>
        public bool Simulated { get; init; }

        /// <summary>是否已作廢</summary>
        public bool Voided { get; init; }
    }

    /// <summary>
    /// 開始日期（含）
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateOnly? From { get; set; }

    /// <summary>
    /// 結束日期（含）
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateOnly? To { get; set; }

    /// <summary>
    /// 付款狀態
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public PaymentStatusFilter Status { get; set; }

    /// <summary>
    /// 頁碼
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 總頁數
    /// </summary>
    public int TotalPages { get; private set; }

    /// <summary>
    /// 符合條件的筆數
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// 本頁的訂單
    /// </summary>
    public List<PaymentRow> Payments { get; private set; } = [];

    /// <summary>
    /// 本月 KPI
    /// </summary>
    public List<KpiCardViewModel> Cards { get; private set; } = [];

    /// <summary>
    /// 載入訂單列表
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadCardsAsync(cancellationToken);

        var query = ApplyFilters(context.Payments.AsNoTracking());
        TotalCount = await query.CountAsync(cancellationToken);
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        PageIndex = Math.Clamp(PageIndex, 1, TotalPages);

        Payments = await ProjectAsync(query.Skip((PageIndex - 1) * PageSize).Take(PageSize), cancellationToken);
    }

    /// <summary>
    /// 依目前的篩選條件匯出 CSV（不分頁）
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>UTF-8（含 BOM）CSV 檔</returns>
    public async Task<IActionResult> OnGetExportAsync(CancellationToken cancellationToken)
    {
        var rows = await ProjectAsync(ApplyFilters(context.Payments.AsNoTracking()), cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("訂單ID,付款時間,用戶名,用戶ID,特店交易編號,綠界交易編號,金額,狀態,交易訊息");
        foreach (var row in rows)
        {
            csv.AppendJoin(',',
                row.PaymentId,
                row.Time?.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture) ?? CsvHelper.Escape(row.RawTime),
                CsvHelper.Escape(row.UserName),
                CsvHelper.Escape(row.UserId),
                CsvHelper.Escape(row.MerchantTradeNo),
                CsvHelper.Escape(row.TradeNo),
                row.Amount,
                StatusText(row),
                CsvHelper.Escape(row.RtnMsg));
            csv.AppendLine();
        }

        return File(CsvHelper.ToUtf8WithBom(csv), "text/csv; charset=utf-8", $"payments-{DateTime.Now:yyyyMMdd}.csv");
    }

    /// <summary>
    /// 訂單狀態的顯示文字
    /// </summary>
    /// <param name="row">訂單</param>
    /// <returns>作廢、模擬付款、成功或失敗</returns>
    public static string StatusText(PaymentRow row) => row switch
    {
        { Voided: true } => "作廢",
        { Simulated: true } => "模擬付款",
        { RtnCode: 1 } => "成功",
        _ => "失敗",
    };

    /// <summary>
    /// 訂單狀態的 badge 樣式
    /// </summary>
    /// <param name="row">訂單</param>
    /// <returns>Bootstrap badge class</returns>
    public static string StatusBadge(PaymentRow row) => row switch
    {
        { Voided: true } => "text-bg-secondary",
        { Simulated: true } => "text-bg-info",
        { RtnCode: 1 } => "text-bg-success",
        _ => "text-bg-danger",
    };

    private IQueryable<Payment> ApplyFilters(IQueryable<Payment> query)
    {
        if (From.HasValue)
        {
            var from = From.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(p => p.PaymentTime >= from);
        }

        if (To.HasValue)
        {
            var to = To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(p => p.PaymentTime < to);
        }

        query = Status switch
        {
            PaymentStatusFilter.Succeeded => query.Where(p => !p.IsVoided && p.SimulatePaid == 0 && p.RtnCode == 1),
            PaymentStatusFilter.Failed => query.Where(p => !p.IsVoided && p.SimulatePaid == 0 && p.RtnCode != 1),
            PaymentStatusFilter.Simulated => query.Where(p => p.SimulatePaid != 0),
            PaymentStatusFilter.Voided => query.Where(p => p.IsVoided),
            _ => query,
        };

        // 無法解析時間的排在最後
        return query
            .OrderByDescending(p => p.PaymentTime)
            .ThenByDescending(p => p.PaymentID);
    }

    private async Task<List<PaymentRow>> ProjectAsync(IQueryable<Payment> query, CancellationToken cancellationToken)
    {
        return await query
            .Select(p => new PaymentRow
            {
                PaymentId = p.PaymentID,
                Time = p.PaymentTime ?? p.TradeTime,
                RawTime = p.PaymentDate,
                UserId = p.InvolverID,
                UserName = context.Profiles.Where(u => u.ProfileID == p.InvolverID).Select(u => u.UserName).FirstOrDefault(),
                MerchantTradeNo = p.MerchantTradeNo,
                TradeNo = p.TradeNo,
                Amount = p.TradeAmt,
                RtnCode = p.RtnCode,
                RtnMsg = p.RtnMsg,
                Simulated = p.SimulatePaid != 0,
                Voided = p.IsVoided,
            })
            .ToListAsync(cancellationToken);
    }

    private async Task LoadCardsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);

        var stats = await MonthStatsAsync(monthStart, monthStart.AddMonths(1), cancellationToken);
        var lastMonth = await MonthStatsAsync(lastMonthStart, monthStart, cancellationToken);

        Cards =
        [
            new KpiCardViewModel
            {
                Label = "本月儲值金額",
                Value = stats.Amount,
                Suffix = "元",
                Icon = "fa-coins",
                ChangePercent = DashboardMetrics.ChangePercent(stats.Amount, lastMonth.Amount),
                ComparisonLabel = "與上月相比",
            },
            new KpiCardViewModel
            {
                Label = "本月成功筆數",
                Value = stats.Succeeded,
                Icon = "fa-receipt",
                ChangePercent = DashboardMetrics.ChangePercent(stats.Succeeded, lastMonth.Succeeded),
                ComparisonLabel = "與上月相比",
            },
            new KpiCardViewModel
            {
                Label = "本月付款成功率",
                Value = stats.Total == 0 ? 0 : Math.Round(stats.Succeeded * 100m / stats.Total, 1),
                Format = "0.#",
                Suffix = "%",
                Icon = "fa-circle-check",
                Hint = $"成功 {stats.Succeeded:N0} ／ 全部 {stats.Total:N0} 筆（不含模擬付款、作廢）",
            },
        ];
    }

    private async Task<(int Amount, int Succeeded, int Total)> MonthStatsAsync(DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var month = context.Payments
            .Where(p => p.PaymentTime >= from && p.PaymentTime < to && p.SimulatePaid == 0 && !p.IsVoided);

        var total = await month.CountAsync(cancellationToken);
        var succeeded = month.Where(p => p.RtnCode == 1);
        return (
            await succeeded.SumAsync(p => p.TradeAmt, cancellationToken),
            await succeeded.CountAsync(cancellationToken),
            total);
    }
}
