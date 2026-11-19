using DataAccess.Data;
using DataAccess.Models.Dashboard;

using Involver.Models.ViewModels.Dashboard;

using Microsoft.EntityFrameworkCore;

namespace Involver.Services.Admin;

/// <summary>
/// 儀表板的期間切換（7 / 30 / 90 天）
/// </summary>
public static class DashboardPeriod
{
    /// <summary>
    /// 可選的天數
    /// </summary>
    public static readonly int[] Options = [7, 30, 90];

    /// <summary>
    /// 預設天數
    /// </summary>
    public const int Default = 30;

    /// <summary>
    /// 不在選項內的天數一律改成預設值
    /// </summary>
    /// <param name="days">網址帶入的天數</param>
    /// <returns>7、30 或 90</returns>
    public static int Normalize(int days) => Options.Contains(days) ? days : Default;
}

/// <summary>
/// 把快照整理成圖表用的每日數列
/// </summary>
public static class DashboardSeries
{
    /// <summary>
    /// 期間內的每一天
    /// </summary>
    /// <param name="start">第一天（含）</param>
    /// <param name="end">最後一天（含）</param>
    /// <returns>由舊到新的日期</returns>
    public static List<DateOnly> Dates(DateOnly start, DateOnly end)
    {
        var dates = new List<DateOnly>();
        for (var d = start; d <= end; d = d.AddDays(1))
        {
            dates.Add(d);
        }

        return dates;
    }

    /// <summary>
    /// 圖表的 X 軸標籤
    /// </summary>
    /// <param name="dates">日期</param>
    /// <returns>MM/dd 格式的標籤</returns>
    public static List<string> Labels(IEnumerable<DateOnly> dates) => dates.Select(d => d.ToString("MM/dd")).ToList();

    /// <summary>
    /// 依日期取值，沒有快照的日子補 0
    /// </summary>
    /// <param name="stats">快照</param>
    /// <param name="dates">日期</param>
    /// <param name="selector">取值</param>
    /// <returns>與日期對應的數值</returns>
    public static List<decimal> Values(IEnumerable<DailySiteStat> stats, IReadOnlyList<DateOnly> dates, Func<DailySiteStat, decimal> selector)
    {
        var byDate = stats.ToDictionary(s => s.Date);
        return dates.Select(d => byDate.TryGetValue(d, out var s) ? selector(s) : 0).ToList();
    }

    /// <summary>
    /// 由每天的總數算出每日新增（今天的總數減前一天的總數）。
    /// 前一天沒有快照時無法計算，補 0；刪除造成的負數也以 0 表示
    /// </summary>
    /// <param name="stats">快照（需包含期間前一天）</param>
    /// <param name="dates">日期</param>
    /// <param name="selector">取總數</param>
    /// <returns>每日新增</returns>
    public static List<decimal> Deltas(IEnumerable<DailySiteStat> stats, IReadOnlyList<DateOnly> dates, Func<DailySiteStat, decimal> selector)
    {
        var byDate = stats.ToDictionary(s => s.Date);
        return dates
            .Select(d => byDate.TryGetValue(d, out var today) && byDate.TryGetValue(d.AddDays(-1), out var yesterday)
                ? Math.Max(0, selector(today) - selector(yesterday))
                : 0)
            .ToList();
    }
}

/// <summary>
/// KPI 目標值的期間計算
/// </summary>
public static class KpiTargetPeriods
{
    /// <summary>
    /// 包含指定日期的期間第一天
    /// </summary>
    /// <param name="period">期間種類</param>
    /// <param name="date">日期</param>
    /// <returns>月初、季初或年初</returns>
    public static DateOnly GetStart(KpiPeriod period, DateOnly date) => period switch
    {
        KpiPeriod.Month => new DateOnly(date.Year, date.Month, 1),
        KpiPeriod.Quarter => new DateOnly(date.Year, ((date.Month - 1) / 3 * 3) + 1, 1),
        KpiPeriod.Year => new DateOnly(date.Year, 1, 1),
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, null),
    };

    /// <summary>
    /// 期間結束的隔天（不含）
    /// </summary>
    /// <param name="period">期間種類</param>
    /// <param name="start">期間第一天</param>
    /// <returns>下一期的第一天</returns>
    public static DateOnly GetEnd(KpiPeriod period, DateOnly start) => period switch
    {
        KpiPeriod.Month => start.AddMonths(1),
        KpiPeriod.Quarter => start.AddMonths(3),
        KpiPeriod.Year => start.AddYears(1),
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, null),
    };

    /// <summary>
    /// 期間名稱，例如「2026/11」、「2026 Q4」、「2026 年」
    /// </summary>
    /// <param name="period">期間種類</param>
    /// <param name="start">期間第一天</param>
    /// <returns>期間名稱</returns>
    public static string GetLabel(KpiPeriod period, DateOnly start) => period switch
    {
        KpiPeriod.Month => $"{start:yyyy/MM}",
        KpiPeriod.Quarter => $"{start.Year} Q{((start.Month - 1) / 3) + 1}",
        KpiPeriod.Year => $"{start.Year} 年",
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, null),
    };

    /// <summary>
    /// 從多個目標中，挑出每個指標涵蓋指定日期、期間最短的那個
    /// </summary>
    /// <param name="targets">目標</param>
    /// <param name="today">日期</param>
    /// <returns>每個指標目前適用的目標</returns>
    public static Dictionary<KpiMetric, KpiTarget> SelectCurrent(IEnumerable<KpiTarget> targets, DateOnly today)
    {
        return targets
            .Where(t => t.PeriodStart <= today && today < GetEnd(t.Period, t.PeriodStart))
            .GroupBy(t => t.Metric)
            .ToDictionary(g => g.Key, g => g.OrderBy(t => t.Period).First());
    }

    /// <summary>
    /// 讀取目前適用的目標
    /// </summary>
    /// <param name="context">資料庫內容</param>
    /// <param name="today">今天</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>每個指標目前適用的目標</returns>
    public static async Task<Dictionary<KpiMetric, KpiTarget>> GetCurrentAsync(ApplicationDbContext context, DateOnly today, CancellationToken cancellationToken = default)
    {
        // 最長的期間是一年，更早開始的目標不可能涵蓋今天
        var earliest = today.AddYears(-1);
        var targets = await context.KpiTargets
            .Where(t => t.PeriodStart <= today && t.PeriodStart > earliest)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return SelectCurrent(targets, today);
    }

    /// <summary>
    /// 計算目標達成進度
    /// </summary>
    /// <param name="target">目標；null 時回傳 null</param>
    /// <param name="value">目前的值</param>
    /// <returns>進度</returns>
    public static KpiTargetProgressViewModel ToProgress(KpiTarget target, decimal value)
    {
        if (target == null || target.TargetValue <= 0)
        {
            return null;
        }

        return new KpiTargetProgressViewModel
        {
            PeriodLabel = GetLabel(target.Period, target.PeriodStart),
            TargetValue = target.TargetValue,
            Percent = Math.Round(value / target.TargetValue * 100, 0),
        };
    }
}
