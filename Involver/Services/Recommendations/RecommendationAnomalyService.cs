using DataAccess.Data;
using DataAccess.Models.Recommendation;
using DataAccess.Services.Recommendations;

using Microsoft.EntityFrameworkCore;

namespace Involver.Services.Recommendations;

/// <summary>
/// 一個內容在觀察期間與過去的不重複讀者數
/// </summary>
/// <param name="Content">內容</param>
/// <param name="RecentReaders">觀察期間的不重複讀者</param>
/// <param name="BaselineReaders">過去 <see cref="RecommendationAnomalyService.BaselineDays"/> 天的不重複讀者</param>
public sealed record ReaderSample(ContentKey Content, int RecentReaders, int BaselineReaders);

/// <summary>
/// 短時間暴增的內容（異常警示）
/// </summary>
/// <param name="Content">內容</param>
/// <param name="Title">標題</param>
/// <param name="AuthorName">作者用戶名</param>
/// <param name="RecentReaders">觀察期間的不重複讀者</param>
/// <param name="BaselineAverage">過去平均每個觀察期間的不重複讀者</param>
public sealed record ContentSpike(ContentKey Content, string Title, string AuthorName, int RecentReaders, double BaselineAverage)
{
    /// <summary>
    /// 是過去平均的幾倍（過去平均不到 1 時以 1 計）
    /// </summary>
    public double Ratio => RecentReaders / Math.Max(BaselineAverage, 1);
}

/// <summary>
/// Feed 各區塊、各設定版本的點擊率
/// </summary>
/// <param name="Section">區塊</param>
/// <param name="SettingVersion">設定版本（只有「為你推薦」有）</param>
/// <param name="Impressions">曝光數</param>
/// <param name="Clicks">點擊數</param>
public sealed record SectionCtr(FeedSection Section, int? SettingVersion, int Impressions, int Clicks)
{
    /// <summary>
    /// 點擊率（0～1）
    /// </summary>
    public double Ctr => Impressions == 0 ? 0 : (double)Clicks / Impressions;
}

/// <summary>
/// 推薦的監控：短時間暴增的作品（防灌）與各區塊的點擊率
/// </summary>
public interface IRecommendationAnomalyService
{
    /// <summary>
    /// 找出短時間暴增的作品與文章
    /// </summary>
    /// <param name="settings">門檻</param>
    /// <param name="now">現在時間</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>依倍數排序</returns>
    Task<List<ContentSpike>> GetSpikesAsync(RecommendationSettings settings, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 最近幾天各區塊、各設定版本的點擊率
    /// </summary>
    /// <param name="days">天數</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>點擊率</returns>
    Task<List<SectionCtr>> GetCtrAsync(int days, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class RecommendationAnomalyService(ApplicationDbContext context) : IRecommendationAnomalyService
{
    /// <summary>
    /// 和過去幾天比較
    /// </summary>
    public const int BaselineDays = 7;

    /// <summary>
    /// 從樣本中找出暴增的內容：觀察期間的讀者達到最少人數，而且是過去平均的指定倍數以上
    /// </summary>
    /// <param name="samples">樣本</param>
    /// <param name="settings">門檻</param>
    /// <returns>暴增的內容（不含標題），依倍數排序</returns>
    public static List<(ContentKey Content, int RecentReaders, double BaselineAverage)> FindSpikes(
        IEnumerable<ReaderSample> samples,
        RecommendationSettings settings)
    {
        var windows = BaselineDays * 24.0 / settings.SpikeWindowHours;
        return samples
            .Select(s => (s.Content, s.RecentReaders, BaselineAverage: s.BaselineReaders / windows))
            .Where(s => s.RecentReaders >= settings.SpikeMinReaders
                && s.RecentReaders >= settings.SpikeMultiplier * Math.Max(s.BaselineAverage, 1))
            .OrderByDescending(s => s.RecentReaders / Math.Max(s.BaselineAverage, 1))
            .ThenBy(s => s.Content)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<ContentSpike>> GetSpikesAsync(RecommendationSettings settings, DateTime now, CancellationToken cancellationToken = default)
    {
        var windowStart = now.AddHours(-settings.SpikeWindowHours);
        var baselineStart = windowStart.AddDays(-BaselineDays);

        // 不重複讀者：登入者以帳號、未登入以 Session 區分；作者本人不計
        var readers = await context.Views
            .Where(v => v.CreateTime >= baselineStart && v.CreateTime <= now)
            .Where(v => v.NovelId == null || v.UserId == null || v.UserId != v.Novel!.ProfileID)
            .Where(v => v.ArticleId == null || v.UserId == null || v.UserId != v.Article!.ProfileID)
            .Select(v => new
            {
                v.NovelId,
                v.ArticleId,
                Reader = v.UserId ?? v.SessionId,
                Recent = v.CreateTime >= windowStart,
            })
            .Distinct()
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var samples = readers
            .GroupBy(r => r.NovelId is { } novelId ? ContentKey.Novel(novelId) : ContentKey.Article(r.ArticleId ?? 0))
            .Select(g => new ReaderSample(
                g.Key,
                g.Where(r => r.Recent).Select(r => r.Reader).Distinct().Count(),
                g.Where(r => !r.Recent).Select(r => r.Reader).Distinct().Count()));

        var spikes = FindSpikes(samples, settings);
        if (spikes.Count == 0)
        {
            return [];
        }

        var novelIds = spikes.Where(s => s.Content.Type == RecommendationContentType.Novel).Select(s => s.Content.Id).ToList();
        var articleIds = spikes.Where(s => s.Content.Type == RecommendationContentType.Article).Select(s => s.Content.Id).ToList();

        var novels = await context.Novels
            .Where(n => novelIds.Contains(n.NovelID))
            .Select(n => new { n.NovelID, n.Title, AuthorName = n.Profile!.UserName })
            .AsNoTracking()
            .ToDictionaryAsync(n => n.NovelID, cancellationToken);
        var articles = await context.Articles
            .Where(a => articleIds.Contains(a.ArticleID))
            .Select(a => new { a.ArticleID, a.Title, AuthorName = a.Profile!.UserName })
            .AsNoTracking()
            .ToDictionaryAsync(a => a.ArticleID, cancellationToken);

        return spikes
            .Select(s =>
            {
                string title = "（已刪除）";
                string author = null;
                if (s.Content.Type == RecommendationContentType.Novel && novels.TryGetValue(s.Content.Id, out var n))
                {
                    (title, author) = (n.Title, n.AuthorName);
                }
                else if (s.Content.Type == RecommendationContentType.Article && articles.TryGetValue(s.Content.Id, out var a))
                {
                    (title, author) = (a.Title, a.AuthorName);
                }

                return new ContentSpike(s.Content, title, author, s.RecentReaders, s.BaselineAverage);
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task<List<SectionCtr>> GetCtrAsync(int days, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-days);
        var rows = await context.RecommendationImpressions
            .Where(i => i.ShownAt >= since)
            .GroupBy(i => new { i.Section, i.SettingVersion })
            .Select(g => new
            {
                g.Key.Section,
                g.Key.SettingVersion,
                Impressions = g.Count(),
                Clicks = g.Count(i => i.ClickedAt != null),
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new SectionCtr(r.Section, r.SettingVersion, r.Impressions, r.Clicks))
            .OrderBy(r => r.Section)
            .ThenByDescending(r => r.SettingVersion)
            .ToList();
    }
}
