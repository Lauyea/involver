using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.StatisticalData;
using DataAccess.Services;

using Involver.Models.ViewModels.Dashboard;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Creator;

/// <summary>
/// 創作者中心 B-1 收入：每月收入、收入來源、本月累計與下次月結、前幾名贊助者；
/// B-4 讀者視角：本月花費、支持過的作者、閱讀紀錄摘要
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="userManager">帳號管理</param>
public class IndexModel(ApplicationDbContext context, UserManager<InvolverUser> userManager) : CreatorPageModel(userManager)
{
    /// <summary>
    /// 每月收入圖顯示的月份數
    /// </summary>
    public const int MonthCount = 12;

    /// <summary>
    /// 贊助者、支持的作者列出幾位
    /// </summary>
    public const int TopCount = 10;

    /// <summary>
    /// 贊助者或支持的作者
    /// </summary>
    /// <param name="ProfileId">用戶 ID</param>
    /// <param name="UserName">用戶名</param>
    /// <param name="Total">累計金額</param>
    /// <param name="Monthly">本月金額</param>
    public record Supporter(string ProfileId, string UserName, decimal Total, decimal Monthly);

    /// <summary>
    /// 最近看過的作品
    /// </summary>
    /// <param name="NovelId">作品 ID</param>
    /// <param name="Title">標題</param>
    /// <param name="ViewDate">觀看時間</param>
    public record RecentNovel(int NovelId, string Title, DateTime ViewDate);

    /// <summary>
    /// 是否有作品或文章（沒有時只顯示讀者視角）
    /// </summary>
    public bool HasWorks { get; private set; }

    /// <summary>
    /// 收入 KPI
    /// </summary>
    public List<KpiCardViewModel> IncomeCards { get; private set; } = [];

    /// <summary>
    /// 每月收入圖
    /// </summary>
    public ChartPanelViewModel MonthlyChart { get; private set; }

    /// <summary>
    /// 收入來源圖
    /// </summary>
    public ChartPanelViewModel SourceChart { get; private set; }

    /// <summary>
    /// 前幾名贊助者
    /// </summary>
    public List<Supporter> TopSupporters { get; private set; } = [];

    /// <summary>
    /// 讀者視角 KPI
    /// </summary>
    public List<KpiCardViewModel> ReaderCards { get; private set; } = [];

    /// <summary>
    /// 支持過的作者
    /// </summary>
    public List<Supporter> SupportedCreators { get; private set; } = [];

    /// <summary>
    /// 最近看過的作品
    /// </summary>
    public List<RecentNovel> RecentNovels { get; private set; } = [];

    /// <summary>
    /// 下次月結時間
    /// </summary>
    public DateTime NextSettlement { get; private set; }

    /// <summary>
    /// 載入創作者中心首頁
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var me = UserId;
        HasWorks = await context.Novels.AnyAsync(n => n.ProfileID == me && !n.IsDeleted, cancellationToken)
            || await context.Articles.AnyAsync(a => a.ProfileID == me && !a.IsDeleted && a.Type == ArticleType.General, cancellationToken);

        var now = DateTime.Now;
        var thisMonth = new DateOnly(now.Year, now.Month, 1);
        NextSettlement = thisMonth.AddMonths(1).ToDateTime(TimeOnly.MinValue);

        if (HasWorks)
        {
            await LoadIncomeAsync(me, thisMonth, cancellationToken);
            await LoadSourcesAsync(me, cancellationToken);
            await LoadTopSupportersAsync(me, cancellationToken);
        }

        await LoadReaderViewAsync(me, thisMonth, cancellationToken);
    }

    private async Task LoadIncomeAsync(string me, DateOnly thisMonth, CancellationToken cancellationToken)
    {
        var firstMonth = thisMonth.AddMonths(-(MonthCount - 1));
        var months = Enumerable.Range(0, MonthCount).Select(i => firstMonth.AddMonths(i)).ToList();

        // 已月結的月份讀月結表；本月讀流水帳
        var settled = await context.CoinMonthlyBalances
            .Where(b => b.ProfileId == me && b.CoinType == CoinType.CreatorIncome && b.Month >= firstMonth)
            .Select(b => new { b.Month, b.Credits })
            .AsNoTracking()
            .ToDictionaryAsync(b => b.Month, b => b.Credits, cancellationToken);

        var thisMonthUtc = thisMonth.ToDateTime(TimeOnly.MinValue).ToUniversalTime();
        var current = await context.CoinTransactions
            .Where(t => t.ProfileId == me && t.CoinType == CoinType.CreatorIncome && t.Amount > 0 && t.CreatedAt >= thisMonthUtc)
            .SumAsync(t => t.Amount, cancellationToken);

        var values = months.Select(m => m == thisMonth ? current : settled.GetValueOrDefault(m)).ToList();
        var lastMonth = settled.GetValueOrDefault(thisMonth.AddMonths(-1));
        var monthlyCoins = await context.Profiles
            .Where(p => p.ProfileID == me)
            .Select(p => p.MonthlyCoins)
            .FirstOrDefaultAsync(cancellationToken);

        IncomeCards =
        [
            new KpiCardViewModel
            {
                Label = "本月累計收入",
                Value = monthlyCoins,
                Suffix = "In",
                Icon = "fa-coins",
                ChangePercent = DashboardMetrics.ChangePercent(monthlyCoins, lastMonth),
                ComparisonLabel = "與上月全月相比",
                Hint = $"下次月結：{NextSettlement:yyyy/MM/dd HH:mm}",
                IsCore = true,
            },
            new KpiCardViewModel { Label = "上月收入", Value = lastMonth, Suffix = "In", Icon = "fa-calendar-check" },
            new KpiCardViewModel { Label = $"近 {MonthCount} 個月收入", Value = values.Sum(), Suffix = "In", Icon = "fa-chart-line" },
        ];

        MonthlyChart = new ChartPanelViewModel
        {
            Title = "每月收入",
            Subtitle = "創作者分潤（本月為累計中）",
            Config = new ChartConfigViewModel
            {
                Type = "bar",
                Labels = months.Select(m => m.ToString("yyyy/MM")).ToList(),
                Datasets = values.Any(v => v > 0) ? [new ChartDatasetViewModel { Label = "收入", Data = values, ColorIndex = 0 }] : [],
            },
        };
    }

    private async Task LoadSourcesAsync(string me, CancellationToken cancellationToken)
    {
        var since = DateTime.UtcNow.AddDays(-Days);

        // 分潤和讀者的付款在同一個請求寫入，共用 CorrelationId；由付款的交易類型判斷收入來源
        var incomes = await context.CoinTransactions
            .Where(r => r.ProfileId == me
                && r.CoinType == CoinType.CreatorIncome
                && r.TransactionType == CoinTransactionType.RevenueShare
                && r.CreatedAt >= since)
            .Select(r => new
            {
                r.Amount,
                SpendType = context.CoinTransactions
                    .Where(s => s.CorrelationId == r.CorrelationId && s.ProfileId == r.CounterpartyProfileId && s.Amount < 0)
                    .Select(s => (CoinTransactionType?)s.TransactionType)
                    .FirstOrDefault(),
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var bySource = incomes
            .GroupBy(i => CreatorIncomeSource.Classify(i.SpendType))
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));
        var names = CreatorIncomeSource.Names.Where(n => bySource.ContainsKey(n)).ToList();

        SourceChart = new ChartPanelViewModel
        {
            Title = "收入來源",
            Subtitle = $"近 {Days} 天",
            Small = true,
            EmptyText = $"近 {Days} 天沒有收入",
            Config = new ChartConfigViewModel
            {
                Type = "doughnut",
                Labels = names,
                Datasets = names.Count == 0 ? [] : [new ChartDatasetViewModel { Label = "收入", Data = names.Select(n => bySource[n]).ToList() }],
            },
        };
    }

    private async Task LoadTopSupportersAsync(string me, CancellationToken cancellationToken)
    {
        // 贊助創作者本人、作品或文章都算
        var involvings = await context.Involvings
            .Where(i => i.ProfileID == me || i.Novel!.ProfileID == me || i.Article!.ProfileID == me)
            .GroupBy(i => i.InvolverID)
            .Select(g => new { InvolverId = g.Key, Total = g.Sum(i => i.TotalValue), Monthly = g.Sum(i => i.MonthlyValue) })
            .OrderByDescending(g => g.Total)
            .Take(TopCount)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var ids = involvings.Select(i => i.InvolverId).ToList();
        var names = await context.Profiles
            .Where(p => ids.Contains(p.ProfileID))
            .Select(p => new { p.ProfileID, p.UserName })
            .ToDictionaryAsync(p => p.ProfileID, p => p.UserName, cancellationToken);

        TopSupporters = involvings
            .Select(i => new Supporter(i.InvolverId, names.GetValueOrDefault(i.InvolverId, "（已刪除）"), i.Total, i.Monthly))
            .ToList();
    }

    private async Task LoadReaderViewAsync(string me, DateOnly thisMonth, CancellationToken cancellationToken)
    {
        var thisMonthUtc = thisMonth.ToDateTime(TimeOnly.MinValue).ToUniversalTime();
        var spending = await context.CoinTransactions
            .Where(t => t.ProfileId == me
                && t.Amount < 0
                && DashboardMetrics.SupportTypes.Contains(t.TransactionType)
                && t.CreatedAt >= thisMonthUtc)
            .Select(t => new { t.CoinType, t.Amount })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var since = DateTime.Now.AddDays(-DashboardMetrics.MonthlyDays);
        var novelsRead = await context.Set<DataAccess.Models.NovelModel.NovelViewer>()
            .CountAsync(v => v.ProfileID == me && v.ViewDate >= since, cancellationToken);
        var articlesRead = await context.Set<DataAccess.Models.ArticleModel.ArticleViewer>()
            .CountAsync(v => v.ProfileID == me && v.ViewDate >= since, cancellationToken);

        ReaderCards =
        [
            new KpiCardViewModel { Label = "本月花費（實體 In 幣）", Value = -spending.Where(s => s.CoinType == CoinType.Real).Sum(s => s.Amount), Suffix = "In", Icon = "fa-wallet" },
            new KpiCardViewModel { Label = "本月花費（虛擬 In 幣）", Value = -spending.Where(s => s.CoinType == CoinType.Virtual).Sum(s => s.Amount), Suffix = "In", Icon = "fa-gift" },
            new KpiCardViewModel { Label = "近 30 天看過的作品", Value = novelsRead, Icon = "fa-book-open-reader" },
            new KpiCardViewModel { Label = "近 30 天看過的文章", Value = articlesRead, Icon = "fa-newspaper" },
        ];

        var supported = await context.CoinTransactions
            .Where(t => t.ProfileId == me
                && t.Amount < 0
                && t.CounterpartyProfileId != null
                && t.CounterpartyProfileId != me
                && DashboardMetrics.SupportTypes.Contains(t.TransactionType))
            .GroupBy(t => t.CounterpartyProfileId)
            .Select(g => new
            {
                CreatorId = g.Key,
                Total = -g.Sum(t => t.Amount),
                Monthly = -g.Where(t => t.CreatedAt >= thisMonthUtc).Sum(t => t.Amount),
            })
            .OrderByDescending(g => g.Total)
            .Take(TopCount)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var ids = supported.Select(s => s.CreatorId).ToList();
        var names = await context.Profiles
            .Where(p => ids.Contains(p.ProfileID))
            .Select(p => new { p.ProfileID, p.UserName })
            .ToDictionaryAsync(p => p.ProfileID, p => p.UserName, cancellationToken);
        SupportedCreators = supported
            .Select(s => new Supporter(s.CreatorId, names.GetValueOrDefault(s.CreatorId, "（已刪除）"), s.Total, s.Monthly))
            .ToList();

        RecentNovels = await context.Set<DataAccess.Models.NovelModel.NovelViewer>()
            .Where(v => v.ProfileID == me && !v.Novel!.IsDeleted)
            .OrderByDescending(v => v.ViewDate)
            .Take(5)
            .Select(v => new RecentNovel(v.NovelID, v.Novel!.Title, v.ViewDate))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }
}
