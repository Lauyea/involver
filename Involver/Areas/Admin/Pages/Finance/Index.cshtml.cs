using System.ComponentModel.DataAnnotations;

using DataAccess.Data;
using DataAccess.Models.Dashboard;
using DataAccess.Models.StatisticalData;

using Involver.Authorization.Admin;
using Involver.Extensions;
using Involver.Models.ViewModels.Dashboard;
using Involver.Services.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Admin.Pages.Finance;

/// <summary>
/// A-2 金流：每月收入（儲值）、支出（創作者分潤、退款、網站支出）與虛擬 In 幣發行量；網站支出的手動登錄
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="userManager">帳號管理</param>
/// <param name="auditService">管理者操作紀錄</param>
[Authorize(Policy = AdminPolicies.Payments)]
public class IndexModel(
    ApplicationDbContext context,
    UserManager<InvolverUser> userManager,
    IAdminAuditService auditService) : AdminPageModel
{
    /// <summary>
    /// 圖表與表格顯示的月份數
    /// </summary>
    public const int MonthCount = 12;

    /// <summary>
    /// 每月的收支
    /// </summary>
    public class MonthRow
    {
        /// <summary>月份（1 日）</summary>
        public DateOnly Month { get; init; }

        /// <summary>儲值收入</summary>
        public decimal TopUp { get; set; }

        /// <summary>創作者分潤</summary>
        public decimal RevenueShare { get; set; }

        /// <summary>退款</summary>
        public decimal Refund { get; set; }

        /// <summary>網站支出</summary>
        public decimal SiteExpense { get; set; }

        /// <summary>虛擬 In 幣發行量</summary>
        public decimal VirtualIssued { get; set; }

        /// <summary>淨額（收入減分潤、退款、網站支出；不含虛擬 In 幣）</summary>
        public decimal Net => TopUp - RevenueShare - Refund - SiteExpense;
    }

    /// <summary>
    /// 新增網站支出的表單
    /// </summary>
    public class ExpenseInput
    {
        /// <summary>日期</summary>
        [Required]
        [Display(Name = "日期")]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        /// <summary>類別</summary>
        [Display(Name = "類別")]
        public SiteExpenseCategory Category { get; set; } = SiteExpenseCategory.Hosting;

        /// <summary>金額</summary>
        [Range(0.01, 100000000, ErrorMessage = "金額必須大於 0")]
        [Display(Name = "金額（元）")]
        public decimal Amount { get; set; }

        /// <summary>說明</summary>
        [StringLength(200)]
        [Display(Name = "說明")]
        public string Note { get; set; }
    }

    /// <summary>
    /// 新增網站支出的表單
    /// </summary>
    [BindProperty]
    public ExpenseInput Expense { get; set; } = new();

    /// <summary>
    /// 近 12 個月的收支，由舊到新
    /// </summary>
    public List<MonthRow> Months { get; private set; } = [];

    /// <summary>
    /// 本月 KPI
    /// </summary>
    public List<KpiCardViewModel> Cards { get; private set; } = [];

    /// <summary>
    /// 每月收支圖
    /// </summary>
    public ChartPanelViewModel CashChart { get; private set; }

    /// <summary>
    /// 虛擬 In 幣發行量圖
    /// </summary>
    public ChartPanelViewModel VirtualChart { get; private set; }

    /// <summary>
    /// 最近登錄的網站支出
    /// </summary>
    public List<SiteExpense> RecentExpenses { get; private set; } = [];

    /// <summary>
    /// 載入金流總覽
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    /// <summary>
    /// 新增網站支出
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostAddExpenseAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        // 變更與稽核紀錄在同一個交易中提交
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var expense = new SiteExpense
        {
            Date = Expense.Date,
            Category = Expense.Category,
            Amount = Expense.Amount,
            Note = Expense.Note?.Trim(),
            CreatedByProfileId = userManager.GetUserId(User),
            CreatedAt = DateTime.UtcNow,
        };
        context.SiteExpenses.Add(expense);
        await context.SaveChangesAsync(cancellationToken);

        await auditService.AddAsync(
            AdminAuditAction.SiteExpenseAdded, "SiteExpense", expense.Id.ToString(),
            after: $"{expense.Date:yyyy/MM/dd} {expense.Category.GetDisplayName()} {expense.Amount:N0} 元 {expense.Note}",
            cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        StatusMessage = "已新增網站支出。";
        return RedirectToPage();
    }

    /// <summary>
    /// 刪除登錄錯誤的網站支出
    /// </summary>
    /// <param name="id">支出 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostDeleteExpenseAsync(int id, CancellationToken cancellationToken)
    {
        var expense = await context.SiteExpenses.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        if (expense == null)
        {
            return NotFound();
        }

        context.SiteExpenses.Remove(expense);
        await auditService.AddAsync(
            AdminAuditAction.SiteExpenseDeleted, "SiteExpense", expense.Id.ToString(),
            before: $"{expense.Date:yyyy/MM/dd} {expense.Category.GetDisplayName()} {expense.Amount:N0} 元 {expense.Note}",
            after: "已刪除",
            cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        StatusMessage = "已刪除網站支出。";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var thisMonth = new DateOnly(now.Year, now.Month, 1);
        var firstMonth = thisMonth.AddMonths(-(MonthCount - 1));
        var firstMonthStart = firstMonth.ToDateTime(TimeOnly.MinValue);

        Months = Enumerable.Range(0, MonthCount)
            .Select(i => new MonthRow { Month = firstMonth.AddMonths(i) })
            .ToList();
        var byMonth = Months.ToDictionary(m => m.Month);

        // 儲值收入：實際付款成功的訂單（不含模擬付款、作廢）
        var topUps = await context.Payments
            .Where(p => p.PaymentTime >= firstMonthStart && p.RtnCode == 1 && p.SimulatePaid == 0 && !p.IsVoided)
            .GroupBy(p => new { p.PaymentTime!.Value.Year, p.PaymentTime!.Value.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(p => p.TradeAmt) })
            .ToListAsync(cancellationToken);
        foreach (var t in topUps)
        {
            if (byMonth.TryGetValue(new DateOnly(t.Year, t.Month, 1), out var row))
            {
                row.TopUp = t.Amount;
            }
        }

        // 已月結的月份讀月結表；本月尚未月結，讀流水帳
        var balances = await context.CoinMonthlyBalances
            .Where(b => b.Month >= firstMonth && (b.CoinType == CoinType.CreatorIncome || b.CoinType == CoinType.Virtual))
            .GroupBy(b => new { b.Month, b.CoinType })
            .Select(g => new { g.Key.Month, g.Key.CoinType, Credits = g.Sum(b => b.Credits) })
            .ToListAsync(cancellationToken);
        foreach (var b in balances)
        {
            if (byMonth.TryGetValue(b.Month, out var row))
            {
                if (b.CoinType == CoinType.CreatorIncome)
                {
                    row.RevenueShare = b.Credits;
                }
                else
                {
                    row.VirtualIssued = b.Credits;
                }
            }
        }

        var thisMonthUtc = thisMonth.ToDateTime(TimeOnly.MinValue).ToUniversalTime();
        var current = byMonth[thisMonth];
        current.RevenueShare = await context.CoinTransactions
            .Where(t => t.CreatedAt >= thisMonthUtc && t.CoinType == CoinType.CreatorIncome && t.Amount > 0)
            .SumAsync(t => t.Amount, cancellationToken);
        current.VirtualIssued = await context.CoinTransactions
            .Where(t => t.CreatedAt >= thisMonthUtc && t.CoinType == CoinType.Virtual && t.Amount > 0)
            .SumAsync(t => t.Amount, cancellationToken);

        // 退款很少，直接讀流水帳
        var firstMonthUtc = firstMonthStart.ToUniversalTime();
        var refunds = await context.CoinTransactions
            .Where(t => t.CreatedAt >= firstMonthUtc && t.TransactionType == CoinTransactionType.Refund)
            .Select(t => new { t.CreatedAt, t.Amount })
            .ToListAsync(cancellationToken);
        foreach (var r in refunds)
        {
            var local = r.CreatedAt.ToLocalTime();
            if (byMonth.TryGetValue(new DateOnly(local.Year, local.Month, 1), out var row))
            {
                row.Refund += Math.Abs(r.Amount);
            }
        }

        var expenses = await context.SiteExpenses
            .Where(e => e.Date >= firstMonth)
            .Select(e => new { e.Date, e.Amount })
            .ToListAsync(cancellationToken);
        foreach (var e in expenses)
        {
            if (byMonth.TryGetValue(new DateOnly(e.Date.Year, e.Date.Month, 1), out var row))
            {
                row.SiteExpense += e.Amount;
            }
        }

        RecentExpenses = await context.SiteExpenses
            .OrderByDescending(e => e.Date)
            .ThenByDescending(e => e.Id)
            .Take(30)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        BuildCards(current, Months.Count > 1 ? Months[^2] : null);
        BuildCharts();
    }

    private void BuildCards(MonthRow current, MonthRow previous)
    {
        static decimal? Change(decimal value, decimal? before) => DataAccess.Services.DashboardMetrics.ChangePercent(value, before);

        Cards =
        [
            new KpiCardViewModel { Label = "本月儲值", Value = current.TopUp, Suffix = "元", Icon = "fa-coins", ChangePercent = Change(current.TopUp, previous?.TopUp), ComparisonLabel = "與上月相比" },
            new KpiCardViewModel { Label = "本月創作者分潤", Value = current.RevenueShare, Suffix = "In", Icon = "fa-hand-holding-dollar", ChangePercent = Change(current.RevenueShare, previous?.RevenueShare), ComparisonLabel = "與上月相比", Hint = "累計中，下個月 1 日月結" },
            new KpiCardViewModel { Label = "本月網站支出", Value = current.SiteExpense, Suffix = "元", Icon = "fa-server", Hint = "手動登錄的主機、網域、手續費" },
            new KpiCardViewModel { Label = "本月虛擬 In 幣發行", Value = current.VirtualIssued, Suffix = "In", Icon = "fa-gift", Hint = "獎勵發放，不是現金支出" },
        ];
    }

    private void BuildCharts()
    {
        var labels = Months.Select(m => m.Month.ToString("yyyy/MM")).ToList();

        CashChart = new ChartPanelViewModel
        {
            Title = "每月收入與支出",
            Subtitle = "1 In 幣以 1 元計",
            Config = new ChartConfigViewModel
            {
                Type = "bar",
                Labels = labels,
                Datasets =
                [
                    new ChartDatasetViewModel { Label = "儲值收入", Data = Months.Select(m => m.TopUp).ToList(), ColorIndex = 2 },
                    new ChartDatasetViewModel { Label = "創作者分潤", Data = Months.Select(m => m.RevenueShare).ToList(), ColorIndex = 0 },
                    new ChartDatasetViewModel { Label = "網站支出", Data = Months.Select(m => m.SiteExpense).ToList(), ColorIndex = 4 },
                    new ChartDatasetViewModel { Label = "退款", Data = Months.Select(m => m.Refund).ToList(), ColorIndex = 3 },
                ],
            },
        };

        VirtualChart = new ChartPanelViewModel
        {
            Title = "虛擬 In 幣發行量",
            Subtitle = "平台負擔，不是現金支出",
            Small = true,
            Config = new ChartConfigViewModel
            {
                Type = "bar",
                Labels = labels,
                Datasets =
                [
                    new ChartDatasetViewModel { Label = "虛擬 In 幣", Data = Months.Select(m => m.VirtualIssued).ToList(), ColorIndex = 5 },
                ],
            },
        };
    }
}
