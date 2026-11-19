using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.Dashboard;
using DataAccess.Services;

using Involver.Authorization.Admin;
using Involver.Models.ViewModels.Dashboard;
using Involver.Services.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Admin.Pages;

/// <summary>
/// A-1 總覽：核心指標、KPI 卡片（含目標進度）、每日趨勢與待處理數量
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="authorizationService">授權服務（決定待處理項目能否點進去）</param>
[Authorize(Policy = AdminPolicies.Access)]
public class IndexModel(ApplicationDbContext context, IAuthorizationService authorizationService) : AdminPageModel
{
    /// <summary>
    /// 待處理的項目
    /// </summary>
    /// <param name="Label">名稱</param>
    /// <param name="Count">數量</param>
    /// <param name="Url">處理頁面；沒有權限時為 null</param>
    /// <param name="Icon">Font Awesome 圖示</param>
    public record PendingItem(string Label, int Count, string Url, string Icon);

    /// <summary>
    /// 核心指標（讀者參與創作）
    /// </summary>
    public List<KpiCardViewModel> CoreMetrics { get; private set; } = [];

    /// <summary>
    /// 一般 KPI 卡片
    /// </summary>
    public List<KpiCardViewModel> Cards { get; private set; } = [];

    /// <summary>
    /// 每日瀏覽趨勢
    /// </summary>
    public ChartPanelViewModel ViewsChart { get; private set; }

    /// <summary>
    /// 每日註冊與活躍用戶趨勢
    /// </summary>
    public ChartPanelViewModel UsersChart { get; private set; }

    /// <summary>
    /// 待處理數量
    /// </summary>
    public List<PendingItem> PendingItems { get; private set; } = [];

    /// <summary>
    /// 最新一筆快照的日期；還沒有快照時為 null
    /// </summary>
    public DateOnly? LatestSnapshotDate { get; private set; }

    /// <summary>
    /// 載入總覽
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var utcNow = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // 比較需要往前多抓一段期間的快照
        var since = today.AddDays(-(Days * 2) - 1);
        var snapshots = await context.DailySiteStats
            .Where(s => s.Date >= since)
            .OrderBy(s => s.Date)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        LatestSnapshotDate = snapshots.LastOrDefault()?.Date;

        // 存量指標和「期間開始那天」的快照比較
        DailySiteStat SnapshotOn(DateOnly date) => snapshots.LastOrDefault(s => s.Date <= date && s.Date > date.AddDays(-3));
        var periodStart = SnapshotOn(today.AddDays(-Days));

        var targets = await KpiTargetPeriods.GetCurrentAsync(context, today, cancellationToken);

        await LoadCoreMetricsAsync(now, utcNow, today, SnapshotOn, targets, cancellationToken);
        await LoadCardsAsync(now, today, snapshots, periodStart, targets, cancellationToken);
        BuildCharts(today, snapshots);
        await LoadPendingItemsAsync(cancellationToken);
    }

    private async Task LoadCoreMetricsAsync(
        DateTime now,
        DateTime utcNow,
        DateOnly today,
        Func<DateOnly, DailySiteStat> snapshotOn,
        Dictionary<KpiMetric, KpiTarget> targets,
        CancellationToken cancellationToken)
    {
        // 核心指標的期間固定（每週、每月），和上一個同長度的期間比較
        var weeklyVoters = await DashboardMetrics.CountVotersAsync(
            context, utcNow.AddDays(-DashboardMetrics.WeeklyDays), utcNow, cancellationToken);
        var payingSupporters = await DashboardMetrics.CountPayingSupportersAsync(
            context, utcNow.AddDays(-DashboardMetrics.MonthlyDays), utcNow, cancellationToken);
        var activeCreators = await DashboardMetrics.CountActiveCreatorsAsync(
            context, now.AddDays(-DashboardMetrics.MonthlyDays), now, cancellationToken);

        var lastWeek = snapshotOn(today.AddDays(-DashboardMetrics.WeeklyDays));
        var lastMonth = snapshotOn(today.AddDays(-DashboardMetrics.MonthlyDays));

        CoreMetrics =
        [
            Card(KpiMetric.WeeklyVoters, "每週參與投票的讀者", weeklyVoters, lastWeek?.WeeklyVoters, "與上週相比", "fa-check-to-slot", targets, isCore: true, hint: "近 7 天投票的不重複讀者"),
            Card(KpiMetric.MonthlyPayingSupporters, "每月付費支持的讀者", payingSupporters, lastMonth?.MonthlyPayingSupporters, "與上月相比", "fa-hand-holding-heart", targets, isCore: true, hint: "近 30 天以實體 In 幣投票或贊助"),
            Card(KpiMetric.MonthlyActiveCreators, "每月有更新的創作者", activeCreators, lastMonth?.MonthlyActiveCreators, "與上月相比", "fa-pen-nib", targets, isCore: true, hint: "近 30 天更新章節或發表文章"),
        ];
    }

    private async Task LoadCardsAsync(
        DateTime now,
        DateOnly today,
        List<DailySiteStat> snapshots,
        DailySiteStat periodStart,
        Dictionary<KpiMetric, KpiTarget> targets,
        CancellationToken cancellationToken)
    {
        var profiles = context.Profiles.AsNoTracking();
        // 沒有用戶管理權限時，總用戶卡片不加連結（點了會 403）
        var canUsers = (await authorizationService.AuthorizeAsync(User, AdminPolicies.Users)).Succeeded;
        var compare = $"與 {Days} 天前相比";

        var totalUsers = await profiles.CountAsync(cancellationToken);
        var periodFrom = now.AddDays(-Days);
        var newUsers = await profiles.CountAsync(p => p.EnrollmentDate >= periodFrom, cancellationToken);
        var previousNewUsers = await profiles.CountAsync(
            p => p.EnrollmentDate >= periodFrom.AddDays(-Days) && p.EnrollmentDate < periodFrom, cancellationToken);
        var monthlyActive = await profiles.CountAsync(
            p => p.LastTimeLogin >= now.AddDays(-DashboardMetrics.MonthlyDays), cancellationToken);
        var prime = await profiles.CountAsync(p => p.Prime, cancellationToken);
        var professional = await profiles.CountAsync(p => p.Professional, cancellationToken);
        var novels = await context.Novels.CountAsync(n => !n.IsDeleted, cancellationToken);
        var articles = await context.Articles.CountAsync(a => !a.IsDeleted && a.Type == ArticleType.General, cancellationToken);

        // DAU 用最近一天完整的快照；還沒有快照時退回今天到目前為止的登入數
        var latest = snapshots.LastOrDefault(s => s.Date < today);
        var dailyActive = latest?.DailyActiveUsers
            ?? await profiles.CountAsync(p => p.LastTimeLogin >= now.Date, cancellationToken);
        var previousDaily = latest == null
            ? null
            : snapshots.LastOrDefault(s => s.Date <= latest.Date.AddDays(-Days))?.DailyActiveUsers;

        Cards =
        [
            Card(KpiMetric.TotalUsers, "總用戶", totalUsers, periodStart?.TotalUsers, compare, "fa-users", targets, url: canUsers ? Url.Page("/Users/Index") : null),
            Card(KpiMetric.NewUsers, $"{Days} 日新註冊", newUsers, previousNewUsers, $"與前 {Days} 天相比", "fa-user-plus", targets),
            Card(KpiMetric.DailyActiveUsers, "DAU", dailyActive, previousDaily, compare, "fa-user-clock", targets,
                hint: latest == null ? "今天到目前為止（依最後登入時間）" : $"{latest.Date:MM/dd} 登入的用戶"),
            Card(KpiMetric.MonthlyActiveUsers, "MAU", monthlyActive, periodStart?.MonthlyActiveUsers, compare, "fa-calendar-check", targets, hint: "近 30 天有登入"),
            Card(KpiMetric.PrimeUsers, "Prime 會員", prime, periodStart?.PrimeUsers, compare, "fa-crown", targets),
            Card(KpiMetric.ProfessionalUsers, "特約作家", professional, periodStart?.ProfessionalUsers, compare, "fa-feather", targets),
            Card(KpiMetric.Novels, "作品數", novels, periodStart?.Novels, compare, "fa-book", targets),
            Card(KpiMetric.Articles, "文章數", articles, periodStart?.Articles, compare, "fa-newspaper", targets),
        ];
    }

    private void BuildCharts(DateOnly today, List<DailySiteStat> snapshots)
    {
        var dates = DashboardSeries.Dates(today.AddDays(-Days), today.AddDays(-1));
        var labels = DashboardSeries.Labels(dates);

        ViewsChart = new ChartPanelViewModel
        {
            Title = "每日瀏覽",
            Subtitle = "作品與文章的瀏覽數",
            EmptyText = "每日快照開始寫入後才會有趨勢資料",
            Config = new ChartConfigViewModel
            {
                Type = "bar",
                Labels = labels,
                Datasets = snapshots.Count == 0 ? [] :
                [
                    new ChartDatasetViewModel { Label = "瀏覽數", Data = DashboardSeries.Values(snapshots, dates, s => s.Views), ColorIndex = 0 },
                ],
            },
        };

        UsersChart = new ChartPanelViewModel
        {
            Title = "每日註冊與活躍用戶",
            EmptyText = "每日快照開始寫入後才會有趨勢資料",
            Config = new ChartConfigViewModel
            {
                Type = "line",
                Labels = labels,
                Datasets = snapshots.Count == 0 ? [] :
                [
                    new ChartDatasetViewModel { Label = "新註冊", Data = DashboardSeries.Values(snapshots, dates, s => s.NewUsers), ColorIndex = 1 },
                    new ChartDatasetViewModel { Label = "DAU", Data = DashboardSeries.Values(snapshots, dates, s => s.DailyActiveUsers), ColorIndex = 2 },
                ],
            },
        };
    }

    private async Task LoadPendingItemsAsync(CancellationToken cancellationToken)
    {
        var pendingTags = await context.Tags.CountAsync(t => t.Status == TagStatus.Pending, cancellationToken);
        var pendingFeedbacks = await context.Articles.CountAsync(
            a => a.Type == ArticleType.Feedback && !a.IsDeleted && a.Accept == null, cancellationToken);
        var blocked = await context.Novels.CountAsync(n => n.Block && !n.IsDeleted, cancellationToken)
            + await context.Articles.CountAsync(a => a.Block && !a.IsDeleted, cancellationToken)
            + await context.Comments.CountAsync(c => c.Block, cancellationToken);

        var canTags = (await authorizationService.AuthorizeAsync(User, AdminPolicies.Tags)).Succeeded;
        var canContent = (await authorizationService.AuthorizeAsync(User, AdminPolicies.Content)).Succeeded;

        PendingItems =
        [
            new("待審標籤", pendingTags, canTags ? Url.Page("/Tags/Index") : null, "fa-tags"),
            new("未處理的意見回饋", pendingFeedbacks, Url.Page("/Feedbacks/Index", new { area = "" }), "fa-comment-dots"),
            new("被封鎖的內容", blocked, canContent ? Url.Page("/ContentStats/Index") + "#blocked" : null, "fa-ban"),
        ];
    }

    private static KpiCardViewModel Card(
        KpiMetric metric,
        string label,
        decimal value,
        decimal? previous,
        string comparisonLabel,
        string icon,
        Dictionary<KpiMetric, KpiTarget> targets,
        bool isCore = false,
        string hint = null,
        string url = null)
    {
        return new KpiCardViewModel
        {
            Label = label,
            Value = value,
            ChangePercent = DashboardMetrics.ChangePercent(value, previous),
            ComparisonLabel = comparisonLabel,
            Icon = icon,
            IsCore = isCore,
            Hint = hint,
            Url = url,
            Target = KpiTargetPeriods.ToProgress(targets.GetValueOrDefault(metric), value),
        };
    }
}
