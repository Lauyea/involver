using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.NovelModel;
using DataAccess.Models.Recommendation;
using DataAccess.Services.Recommendations;

using Involver.Models.ViewModels.Feed;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;


namespace Involver.Services.Recommendations;

/// <summary>
/// 組出會員首頁 Feed：繼續閱讀、投票進行中、為你推薦、熱門／新作。
/// 推薦只讀排程預先算好的 <see cref="UserRecommendation"/>，不在請求時計算；所有區塊都排除已刪除、封鎖的作品。
/// </summary>
public interface IHomeFeedService
{
    /// <summary>
    /// 組出某位會員的 Feed
    /// </summary>
    /// <param name="profileId">會員</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Feed</returns>
    Task<HomeFeedViewModel> BuildAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 熱門作品：近 7 天不重複讀者最多（作者本人、Banned、註冊未滿 <see cref="RecommendationSettings.TrustMinAccountAgeDays"/> 天的帳號不計），快取幾分鐘
    /// </summary>
    /// <param name="count">筆數，最多 <see cref="HomeFeedService.TrendingPageCount"/></param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>作品卡片，依讀者數由多到少</returns>
    Task<List<ContentCardViewModel>> GetTrendingAsync(int count = HomeFeedService.DiscoverCount, CancellationToken cancellationToken = default);

    /// <summary>
    /// 儲存會員喜歡的主分類（冷啟動），取代原本的選擇
    /// </summary>
    /// <param name="profileId">會員</param>
    /// <param name="tagIds">分類標籤 ID（最多 <see cref="HomeFeedService.MaxPreferredCategories"/> 個，不是分類的會略過）</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task SavePreferredCategoriesAsync(string profileId, IEnumerable<int> tagIds, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
/// <param name="context">資料庫內容</param>
/// <param name="cache">快取</param>
/// <param name="settingsService">推薦設定（熱門排行排除新帳號的天數）</param>
/// <param name="categoryService">分類</param>
public class HomeFeedService(
    ApplicationDbContext context,
    IMemoryCache cache,
    IRecommendationSettingsService settingsService,
    ICategoryService categoryService) : IHomeFeedService
{
    /// <summary>
    /// 冷啟動最多選幾個主分類
    /// </summary>
    public const int MaxPreferredCategories = 3;

    /// <summary>
    /// 繼續閱讀最多幾部
    /// </summary>
    public const int ContinueReadingCount = 12;

    /// <summary>
    /// 投票最多幾個
    /// </summary>
    public const int VotingCount = 6;

    /// <summary>
    /// 為你推薦顯示幾張
    /// </summary>
    public const int RecommendedCount = 12;

    /// <summary>
    /// 熱門、新作各幾張
    /// </summary>
    public const int DiscoverCount = 8;

    /// <summary>
    /// 熱門創作頁顯示的筆數（快取也存這麼多，首頁再從中取前幾名）
    /// </summary>
    public const int TrendingPageCount = 24;

    /// <summary>
    /// 熱門以最近幾天的不重複讀者計算
    /// </summary>
    public const int TrendingDays = 7;

    /// <summary>
    /// 繼續閱讀只看最近幾天更新的章節
    /// </summary>
    public const int ContinueReadingDays = 180;

    /// <summary>
    /// 熱門、新作的快取時間
    /// </summary>
    public static readonly TimeSpan DiscoverCacheDuration = TimeSpan.FromMinutes(10);

    private const string TrendingCacheKey = "Feed:Trending";
    private const string NewestCacheKey = "Feed:Newest";

    /// <inheritdoc />
    public async Task<HomeFeedViewModel> BuildAsync(string profileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(profileId);

        var followedNovelIds = await context.Follows
            .Where(f => f.FollowerID == profileId && f.NovelID != null)
            .Select(f => f.NovelID!.Value)
            .ToListAsync(cancellationToken);

        var continueReading = await GetContinueReadingAsync(profileId, followedNovelIds, cancellationToken);

        var categories = await categoryService.GetCategoriesAsync(cancellationToken);
        var preferredCategoryIds = await context.ProfileCategoryPreferences
            .Where(p => p.ProfileId == profileId && p.Tag!.IsCategory)
            .OrderBy(p => p.Tag!.CategoryOrder)
            .Select(p => p.TagId)
            .ToListAsync(cancellationToken);

        var recommended = await GetRecommendedAsync(profileId, followedNovelIds, cancellationToken);
        var needsCategoryPicker = recommended.Count == 0 && preferredCategoryIds.Count == 0 && categories.Count > 0;
        if (recommended.Count == 0 && preferredCategoryIds.Count > 0)
        {
            // 剛選完分類、排程還沒算到這位會員：先用選的分類找作品
            recommended = await GetByPreferredCategoriesAsync(profileId, preferredCategoryIds, cancellationToken);
        }

        var recommendedNovelIds = recommended
            .Where(c => c.ContentType == RecommendationContentType.Novel)
            .Select(c => c.ContentId)
            .ToList();
        var votings = await GetVotingsAsync(followedNovelIds, recommendedNovelIds, cancellationToken);

        var trending = await GetTrendingAsync(DiscoverCount, cancellationToken);
        var newest = await GetNewestAsync(cancellationToken);

        return new HomeFeedViewModel
        {
            ContinueReading = continueReading.Select((c, i) => c.At(FeedSection.ContinueReading, i)).ToList(),
            HasFollowedNovels = followedNovelIds.Count > 0,
            Votings = votings,
            Recommended = recommended.Select((c, i) => c.At(FeedSection.Recommended, i)).ToList(),
            NeedsCategoryPicker = needsCategoryPicker,
            CategoryOptions = categories,
            PreferredCategoryIds = preferredCategoryIds,
            Trending = trending.Select((c, i) => c.At(FeedSection.Trending, i)).ToList(),
            Newest = newest.Select((c, i) => c.At(FeedSection.Newest, i)).ToList(),
        };
    }

    /// <inheritdoc />
    public async Task<List<ContentCardViewModel>> GetTrendingAsync(int count = DiscoverCount, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 0, TrendingPageCount);
        if (cache.TryGetValue(TrendingCacheKey, out List<ContentCardViewModel> cached))
        {
            return cached.Take(count).ToList();
        }

        var now = DateTime.Now;
        var since = now.AddDays(-TrendingDays);
        var settings = (await settingsService.GetActiveAsync(cancellationToken)).Settings;
        var enrolledBefore = now.AddDays(-settings.TrustMinAccountAgeDays);

        // 不重複讀者：登入者以帳號、未登入以 Session 區分；作者本人、Banned 與註冊未滿天數的帳號不計（防灌）
        var readers = await context.Views
            .Where(v => v.NovelId != null && v.CreateTime >= since)
            .Where(v => v.UserId == null
                || (v.UserId != v.Novel!.ProfileID
                    && context.Profiles.Any(p => p.ProfileID == v.UserId && !p.Banned && p.EnrollmentDate <= enrolledBefore)))
            .GroupBy(v => v.NovelId!.Value)
            .Select(g => new { NovelId = g.Key, Readers = g.Select(v => v.UserId ?? v.SessionId).Distinct().Count() })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var rankedIds = readers.Select(r => r.NovelId).ToList();
        var readable = (await ReadableNovels()
                .Where(n => rankedIds.Contains(n.NovelID))
                .Select(n => n.NovelID)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var ranked = readers
            .Where(r => readable.Contains(r.NovelId))
            .OrderByDescending(r => r.Readers)
            .ThenBy(r => r.NovelId)
            .Select(r => r.NovelId)
            .Take(TrendingPageCount)
            .ToList();

        var cards = await LoadNovelCardsAsync(ranked, cancellationToken);
        var result = ranked
            .Where(cards.ContainsKey)
            .Select(id => cards[id])
            .Take(TrendingPageCount)
            .ToList();

        // 近期沒有讀者時，以總觀看數補滿
        if (result.Count < TrendingPageCount)
        {
            var exclude = result.Select(c => c.ContentId).ToList();
            var fillIds = await ReadableNovels()
                .Where(n => !exclude.Contains(n.NovelID))
                .OrderByDescending(n => n.TotalViews)
                .ThenByDescending(n => n.NovelID)
                .Select(n => n.NovelID)
                .Take(TrendingPageCount - result.Count)
                .ToListAsync(cancellationToken);
            var fillCards = await LoadNovelCardsAsync(fillIds, cancellationToken);
            result.AddRange(fillIds.Where(fillCards.ContainsKey).Select(id => fillCards[id]));
        }

        cache.Set(TrendingCacheKey, result, DiscoverCacheDuration);
        return result.Take(count).ToList();
    }

    /// <inheritdoc />
    public async Task SavePreferredCategoriesAsync(string profileId, IEnumerable<int> tagIds, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(profileId);
        var categoryIds = (await categoryService.GetCategoriesAsync(cancellationToken)).Select(c => c.TagId).ToHashSet();
        var selected = (tagIds ?? [])
            .Where(categoryIds.Contains)
            .Distinct()
            .Take(MaxPreferredCategories)
            .ToList();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await context.ProfileCategoryPreferences
            .Where(p => p.ProfileId == profileId)
            .ExecuteDeleteAsync(cancellationToken);

        var now = DateTime.UtcNow;
        context.ProfileCategoryPreferences.AddRange(selected.Select(tagId => new ProfileCategoryPreference
        {
            ProfileId = profileId,
            TagId = tagId,
            CreatedAt = now,
        }));
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    /// <summary>
    /// 投票的剩餘時間或進度
    /// </summary>
    /// <param name="limit">截止依據</param>
    /// <param name="deadLine">期限</param>
    /// <param name="totalNumber">目前人數</param>
    /// <param name="numberLimit">人數上限</param>
    /// <param name="totalCoins">目前 In 幣</param>
    /// <param name="coinLimit">In 幣上限</param>
    /// <param name="now">現在時間</param>
    /// <returns>例如「剩 3 小時」、「12 / 50 人」</returns>
    public static string FormatRemaining(
        LimitType limit,
        DateTime? deadLine,
        int totalNumber,
        int? numberLimit,
        int totalCoins,
        int? coinLimit,
        DateTime now)
    {
        switch (limit)
        {
            case LimitType.Time when deadLine is { } end:
                var left = end - now;
                if (left <= TimeSpan.Zero)
                {
                    return "即將截止";
                }

                if (left.TotalHours < 1)
                {
                    return $"剩 {Math.Max(1, (int)Math.Ceiling(left.TotalMinutes))} 分鐘";
                }

                return left.TotalDays < 1
                    ? $"剩 {(int)Math.Ceiling(left.TotalHours)} 小時"
                    : $"剩 {(int)Math.Ceiling(left.TotalDays)} 天";
            case LimitType.Number when numberLimit is { } maxNumber:
                return $"{totalNumber:N0} / {maxNumber:N0} 人";
            case LimitType.Value when coinLimit is { } maxCoins:
                return $"{totalCoins:N0} / {maxCoins:N0} In 幣";
            default:
                return $"{totalNumber:N0} 人參與";
        }
    }

    /// <summary>
    /// 未刪除、未封鎖、至少有一個未刪除章節的作品
    /// </summary>
    private IQueryable<Novel> ReadableNovels() => context.Novels
        .Where(n => !n.IsDeleted && !n.Block && n.Episodes!.Any(e => !e.IsDeleted))
        .AsNoTracking();

    /// <summary>
    /// 繼續閱讀：追蹤中的作品裡，上次看過之後更新、而且還沒讀過的章節
    /// </summary>
    private async Task<List<ContentCardViewModel>> GetContinueReadingAsync(
        string profileId,
        List<int> followedNovelIds,
        CancellationToken cancellationToken)
    {
        if (followedNovelIds.Count == 0)
        {
            return [];
        }

        // 上次看到這部作品的時間：作品頁、章節閱讀、開始追蹤，取最晚的
        var baselines = await context.Follows
            .Where(f => f.FollowerID == profileId && f.NovelID != null)
            .Select(f => new
            {
                NovelId = f.NovelID!.Value,
                FollowedAt = f.UpdateTime,
                LastNovelView = f.Novel!.NovelViewers!
                    .Where(v => v.ProfileID == profileId)
                    .Max(v => (DateTime?)v.ViewDate),
                LastEpisodeRead = context.EpisodeReads
                    .Where(r => r.ProfileId == profileId && r.Episode!.NovelID == f.NovelID)
                    .Max(r => (DateTime?)r.LastReadAt),
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var since = DateTime.Now.AddDays(-ContinueReadingDays);
        var episodes = await context.Episodes
            .Where(e => followedNovelIds.Contains(e.NovelID) && !e.IsDeleted && e.UpdateTime >= since)
            .Where(e => !context.EpisodeReads.Any(r => r.ProfileId == profileId && r.EpisodeId == e.EpisodeID))
            .Select(e => new { e.EpisodeID, e.NovelID, e.Title, e.UpdateTime })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var unreadByNovel = baselines
            .Select(b =>
            {
                var seen = new[] { b.FollowedAt, b.LastNovelView ?? DateTime.MinValue, b.LastEpisodeRead ?? DateTime.MinValue }.Max();
                var unread = episodes
                    .Where(e => e.NovelID == b.NovelId && e.UpdateTime > seen)
                    .OrderBy(e => e.EpisodeID)
                    .ToList();
                return new { b.NovelId, Unread = unread };
            })
            .Where(x => x.Unread.Count > 0)
            .OrderByDescending(x => x.Unread.Max(e => e.UpdateTime))
            .ThenBy(x => x.NovelId)
            .Take(ContinueReadingCount)
            .ToList();

        var cards = await LoadNovelCardsAsync(unreadByNovel.Select(x => x.NovelId), cancellationToken);
        return unreadByNovel
            .Where(x => cards.ContainsKey(x.NovelId))
            .Select(x =>
            {
                var card = cards[x.NovelId];
                var next = x.Unread[0];
                return new ContentCardViewModel
                {
                    ContentType = card.ContentType,
                    ContentId = card.ContentId,
                    Title = card.Title,
                    ImageUrl = card.ImageUrl,
                    AuthorId = card.AuthorId,
                    AuthorName = card.AuthorName,
                    Category = card.Category,
                    Tags = card.Tags,
                    UpdatedAt = x.Unread.Max(e => e.UpdateTime),
                    Badge = $"{x.Unread.Count} 集未讀",
                    EpisodeId = next.EpisodeID,
                    EpisodeTitle = next.Title,
                    Compact = true,
                };
            })
            .ToList();
    }

    /// <summary>
    /// 為你推薦：讀排程算好的結果，再排除之後才刪除、封鎖、看過、追蹤或按了不感興趣的
    /// </summary>
    private async Task<List<ContentCardViewModel>> GetRecommendedAsync(
        string profileId,
        List<int> followedNovelIds,
        CancellationToken cancellationToken)
    {
        var rows = await context.UserRecommendations
            .Where(r => r.ProfileId == profileId)
            .OrderBy(r => r.Rank)
            .Take(RecommendedCount * 3)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return [];
        }

        var dismissed = (await context.RecommendationFeedbacks
                .Where(f => f.ProfileId == profileId)
                .Select(f => new { f.ContentType, f.ContentId })
                .ToListAsync(cancellationToken))
            .Select(f => (f.ContentType, f.ContentId))
            .ToHashSet();

        var novelIds = rows.Where(r => r.ContentType == RecommendationContentType.Novel).Select(r => r.ContentId).ToList();
        var articleIds = rows.Where(r => r.ContentType == RecommendationContentType.Article).Select(r => r.ContentId).ToList();

        var viewedNovels = (await context.Set<NovelViewer>()
                .Where(v => v.ProfileID == profileId && novelIds.Contains(v.NovelID))
                .Select(v => v.NovelID)
                .ToListAsync(cancellationToken))
            .Concat(followedNovelIds)
            .ToHashSet();
        var viewedArticles = (await context.Set<DataAccess.Models.ArticleModel.ArticleViewer>()
                .Where(v => v.ProfileID == profileId && articleIds.Contains(v.ArticleID))
                .Select(v => v.ArticleID)
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var novels = await LoadNovelCardsAsync(novelIds, cancellationToken);
        var articles = await LoadArticleCardsAsync(articleIds, cancellationToken);

        var result = new List<ContentCardViewModel>();
        foreach (var row in rows)
        {
            if (dismissed.Contains((row.ContentType, row.ContentId)))
            {
                continue;
            }

            ContentCardViewModel card;
            if (row.ContentType == RecommendationContentType.Novel)
            {
                if (viewedNovels.Contains(row.ContentId) || !novels.TryGetValue(row.ContentId, out card))
                {
                    continue;
                }
            }
            else if (viewedArticles.Contains(row.ContentId) || !articles.TryGetValue(row.ContentId, out card))
            {
                continue;
            }

            if (card.AuthorId == profileId)
            {
                continue;
            }

            result.Add(WithReason(card, row.Reason, row.SettingVersion));
            if (result.Count >= RecommendedCount)
            {
                break;
            }
        }

        return result;
    }

    /// <summary>
    /// 排程還沒算到的新會員：依自選的分類，找還沒看過的小說與文章（依更新時間）
    /// </summary>
    private async Task<List<ContentCardViewModel>> GetByPreferredCategoriesAsync(
        string profileId,
        List<int> categoryIds,
        CancellationToken cancellationToken)
    {
        var novels = await ReadableNovels()
            .Where(n => n.ProfileID != profileId && n.Tags!.Any(t => categoryIds.Contains(t.TagId)))
            .Where(n => !n.NovelViewers!.Any(v => v.ProfileID == profileId))
            .Where(n => !n.Follows!.Any(f => f.FollowerID == profileId))
            .OrderByDescending(n => n.UpdateTime)
            .ThenByDescending(n => n.NovelID)
            .Select(n => new
            {
                n.NovelID,
                n.UpdateTime,
                Category = n.Tags!.Where(t => categoryIds.Contains(t.TagId)).OrderBy(t => t.CategoryOrder).Select(t => t.Name).First(),
            })
            .Take(RecommendedCount)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var articles = await context.Articles
            .Where(a => !a.IsDeleted && !a.Block && a.Type == ArticleType.General && a.ProfileID != profileId)
            .Where(a => a.Tags!.Any(t => categoryIds.Contains(t.TagId)))
            .Where(a => !a.ArticleViewers!.Any(v => v.ProfileID == profileId))
            .OrderByDescending(a => a.UpdateTime)
            .ThenByDescending(a => a.ArticleID)
            .Select(a => new
            {
                a.ArticleID,
                a.UpdateTime,
                Category = a.Tags!.Where(t => categoryIds.Contains(t.TagId)).OrderBy(t => t.CategoryOrder).Select(t => t.Name).First(),
            })
            .Take(RecommendedCount)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var novelCards = await LoadNovelCardsAsync(novels.Select(n => n.NovelID), cancellationToken);
        var articleCards = await LoadArticleCardsAsync(articles.Select(a => a.ArticleID), cancellationToken);

        return novels
            .Where(n => novelCards.ContainsKey(n.NovelID))
            .Select(n => (n.UpdateTime, Card: WithReason(novelCards[n.NovelID], $"你喜歡{n.Category}類作品", null)))
            .Concat(articles
                .Where(a => articleCards.ContainsKey(a.ArticleID))
                .Select(a => (a.UpdateTime, Card: WithReason(articleCards[a.ArticleID], $"你喜歡{a.Category}類作品", null))))
            .OrderByDescending(x => x.UpdateTime)
            .Take(RecommendedCount)
            .Select(x => x.Card)
            .ToList();
    }

    /// <summary>
    /// 投票進行中：追蹤或推薦作品裡快截止的投票；都沒有時改列全站的
    /// </summary>
    private async Task<List<FeedVotingViewModel>> GetVotingsAsync(
        List<int> followedNovelIds,
        List<int> recommendedNovelIds,
        CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var open = context.Votings
            .Where(v => !v.End
                && !v.Episode!.IsDeleted
                && !v.Episode.Novel!.IsDeleted
                && !v.Episode.Novel.Block)
            .Where(v => (v.Limit != LimitType.Time || v.DeadLine > now)
                && (v.Limit != LimitType.Number || v.NumberLimit == null || v.TotalNumber < v.NumberLimit)
                && (v.Limit != LimitType.Value || v.CoinLimit == null || v.TotalCoins < v.CoinLimit));

        var scopeIds = followedNovelIds.Concat(recommendedNovelIds).Distinct().ToList();
        var rows = await Project(open.Where(v => scopeIds.Contains(v.Episode!.NovelID)), VotingCount, cancellationToken);
        if (rows.Count == 0)
        {
            rows = await Project(open, VotingCount / 2, cancellationToken);
        }

        var followed = followedNovelIds.ToHashSet();
        return rows
            .Select(v => new FeedVotingViewModel
            {
                VotingId = v.VotingID,
                Title = v.Title,
                EpisodeId = v.EpisodeID,
                EpisodeTitle = v.EpisodeTitle,
                NovelId = v.NovelID,
                NovelTitle = v.NovelTitle,
                DeadLine = v.DeadLine,
                Remaining = FormatRemaining(v.Limit, v.DeadLine, v.TotalNumber, v.NumberLimit, v.TotalCoins, v.CoinLimit, now),
                Urgent = v.Limit == LimitType.Time && v.DeadLine is { } d && d - now < TimeSpan.FromDays(1),
                Participants = v.TotalNumber,
                FromFollowed = followed.Contains(v.NovelID),
            })
            .ToList();

        static Task<List<VotingRow>> Project(IQueryable<Voting> query, int count, CancellationToken cancellationToken) => query
            .OrderBy(v => v.DeadLine == null ? 1 : 0)
            .ThenBy(v => v.DeadLine)
            .ThenByDescending(v => v.CreateTime)
            .Select(v => new VotingRow(
                v.VotingID,
                v.Title,
                v.EpisodeID,
                v.Episode!.Title,
                v.Episode.NovelID,
                v.Episode.Novel!.Title,
                v.Limit,
                v.DeadLine,
                v.TotalNumber,
                v.NumberLimit,
                v.TotalCoins,
                v.CoinLimit))
            .Take(count)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 新作：最近建立、已有章節的作品（快取幾分鐘）
    /// </summary>
    private async Task<List<ContentCardViewModel>> GetNewestAsync(CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(NewestCacheKey, out List<ContentCardViewModel> cached))
        {
            return cached;
        }

        var ids = await ReadableNovels()
            .OrderByDescending(n => n.CreateTime)
            .ThenByDescending(n => n.NovelID)
            .Select(n => n.NovelID)
            .Take(DiscoverCount)
            .ToListAsync(cancellationToken);

        var cards = await LoadNovelCardsAsync(ids, cancellationToken);
        var result = ids.Where(cards.ContainsKey).Select(id => cards[id]).ToList();
        cache.Set(NewestCacheKey, result, DiscoverCacheDuration);
        return result;
    }

    /// <summary>
    /// 讀取作品卡片需要的欄位（排除刪除、封鎖）
    /// </summary>
    private async Task<Dictionary<int, ContentCardViewModel>> LoadNovelCardsAsync(IEnumerable<int> ids, CancellationToken cancellationToken)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        var rows = await context.Novels
            .Where(n => idList.Contains(n.NovelID) && !n.IsDeleted && !n.Block)
            .Select(n => new
            {
                n.NovelID,
                n.Title,
                n.ImageUrl,
                n.ProfileID,
                AuthorName = n.Profile!.UserName,
                n.UpdateTime,
                Category = n.Tags!
                    .Where(t => t.IsCategory && (t.CategoryScope & CategoryScope.Novel) != 0)
                    .OrderBy(t => t.CategoryOrder)
                    .Select(t => t.Name)
                    .FirstOrDefault(),
                Tags = n.Tags!
                    .Where(t => t.Status == TagStatus.Approved && !(t.IsCategory && (t.CategoryScope & CategoryScope.Novel) != 0))
                    .OrderBy(t => t.TagId)
                    .Select(t => t.Name)
                    .Take(3)
                    .ToList(),
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(n => n.NovelID, n => new ContentCardViewModel
        {
            ContentType = RecommendationContentType.Novel,
            ContentId = n.NovelID,
            Title = n.Title,
            ImageUrl = n.ImageUrl,
            AuthorId = n.ProfileID,
            AuthorName = n.AuthorName,
            Category = n.Category ?? "創作",
            Tags = n.Tags,
            UpdatedAt = n.UpdateTime,
        });
    }

    /// <summary>
    /// 讀取文章卡片需要的欄位（只限一般文章，排除刪除、封鎖）
    /// </summary>
    private async Task<Dictionary<int, ContentCardViewModel>> LoadArticleCardsAsync(IEnumerable<int> ids, CancellationToken cancellationToken)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        var rows = await context.Articles
            .Where(a => idList.Contains(a.ArticleID) && !a.IsDeleted && !a.Block && a.Type == ArticleType.General)
            .Select(a => new
            {
                a.ArticleID,
                a.Title,
                a.ImageUrl,
                a.ProfileID,
                AuthorName = a.Profile!.UserName,
                a.UpdateTime,
                Category = a.Tags!
                    .Where(t => t.IsCategory && (t.CategoryScope & CategoryScope.Article) != 0)
                    .OrderBy(t => t.CategoryOrder)
                    .Select(t => t.Name)
                    .FirstOrDefault(),
                Tags = a.Tags!
                    .Where(t => t.Status == TagStatus.Approved && !(t.IsCategory && (t.CategoryScope & CategoryScope.Article) != 0))
                    .OrderBy(t => t.TagId)
                    .Select(t => t.Name)
                    .Take(3)
                    .ToList(),
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(a => a.ArticleID, a => new ContentCardViewModel
        {
            ContentType = RecommendationContentType.Article,
            ContentId = a.ArticleID,
            Title = a.Title,
            ImageUrl = a.ImageUrl,
            AuthorId = a.ProfileID,
            AuthorName = a.AuthorName,
            Category = a.Category ?? "文章",
            Tags = a.Tags,
            UpdatedAt = a.UpdateTime,
        });
    }

    private static ContentCardViewModel WithReason(ContentCardViewModel card, string reason, int? settingVersion) => new()
    {
        ContentType = card.ContentType,
        ContentId = card.ContentId,
        Title = card.Title,
        ImageUrl = card.ImageUrl,
        AuthorId = card.AuthorId,
        AuthorName = card.AuthorName,
        Category = card.Category,
        Tags = card.Tags,
        UpdatedAt = card.UpdatedAt,
        Reason = reason,
        SettingVersion = settingVersion,
        Dismissible = true,
    };

    /// <summary>
    /// 投票查詢的投影
    /// </summary>
    private sealed record VotingRow(
        int VotingID,
        string Title,
        int EpisodeID,
        string EpisodeTitle,
        int NovelID,
        string NovelTitle,
        LimitType Limit,
        DateTime? DeadLine,
        int TotalNumber,
        int? NumberLimit,
        int TotalCoins,
        int? CoinLimit);
}
