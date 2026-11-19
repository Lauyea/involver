using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;
using DataAccess.Models.Recommendation;
using DataAccess.Models.StatisticalData;

using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services.Recommendations
{
    /// <summary>
    /// 目前啟用的推薦設定
    /// </summary>
    /// <param name="Settings">設定</param>
    /// <param name="Version">版本；資料庫沒有啟用的設定（或內容無效）時為 null，使用程式預設值</param>
    public sealed record ActiveRecommendationSettings(RecommendationSettings Settings, int? Version);

    /// <summary>
    /// 一次推薦計算的結果
    /// </summary>
    /// <param name="Users">有寫入推薦的會員數</param>
    /// <param name="Rows">寫入的推薦筆數</param>
    /// <param name="SettingVersion">使用的設定版本</param>
    /// <param name="ComputedAt">計算時間（UTC）</param>
    public sealed record RecommendationRunResult(int Users, int Rows, int? SettingVersion, DateTime ComputedAt);

    /// <summary>
    /// 推薦排程：讀取行為資料 → 建立訊號 → 為每位活躍會員評分 → 整批取代 <see cref="UserRecommendation"/>。
    /// 由 Azure Function 定時呼叫，管理員也可以在後台立即執行。
    /// </summary>
    /// <param name="context">資料庫內容</param>
    /// <param name="scorer">評分實作；預設為 <see cref="WeightedRecommendationScorer"/></param>
    public class RecommendationEngine(ApplicationDbContext context, IRecommendationScorer? scorer = null)
    {
        private readonly IRecommendationScorer _scorer = scorer ?? new WeightedRecommendationScorer();

        /// <summary>
        /// 讀取目前啟用的設定；沒有啟用的版本或 JSON 無效時使用程式預設值
        /// </summary>
        /// <param name="context">資料庫內容</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>設定與版本</returns>
        public static async Task<ActiveRecommendationSettings> LoadActiveSettingsAsync(
            ApplicationDbContext context,
            CancellationToken cancellationToken = default)
        {
            var active = await context.RecommendationSettings
                .Where(s => s.IsActive)
                .Select(s => new { s.Version, s.SettingsJson })
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            var settings = RecommendationSettings.FromJson(active?.SettingsJson);
            return settings is null
                ? new ActiveRecommendationSettings(new RecommendationSettings(), null)
                : new ActiveRecommendationSettings(settings, active!.Version);
        }

        /// <summary>
        /// 計算並寫入推薦
        /// </summary>
        /// <param name="now">現在時間（與資料庫時間同一時區，通常是 <see cref="DateTime.Now"/>）</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>結果</returns>
        public async Task<RecommendationRunResult> RunAsync(DateTime now, CancellationToken cancellationToken = default)
        {
            var active = await LoadActiveSettingsAsync(context, cancellationToken);
            var settings = active.Settings;
            var input = await LoadInputAsync(now, cancellationToken);
            var corpus = new RecommendationCorpus(input, settings);

            // 活躍會員：最近登入、最近看過作品，或剛選完喜歡的分類
            var activeSince = now.AddDays(-settings.ActiveUserDays);
            var activeSinceUtc = DateTime.UtcNow.AddDays(-settings.ActiveUserDays);
            var recentLogins = context.Profiles
                .Where(p => p.LastTimeLogin >= activeSince)
                .Select(p => p.ProfileID);
            var recentViewers = context.Views
                .Where(v => v.UserId != null && v.CreateTime >= activeSince)
                .Select(v => v.UserId!);
            var newPreferences = context.ProfileCategoryPreferences
                .Where(t => t.CreatedAt >= activeSinceUtc)
                .Select(t => t.ProfileId);
            var activeIds = await recentLogins
                .Union(recentViewers)
                .Union(newPreferences)
                .ToListAsync(cancellationToken);

            var banned = (await context.Profiles
                    .Where(p => p.Banned)
                    .Select(p => p.ProfileID)
                    .ToListAsync(cancellationToken))
                .ToHashSet(StringComparer.Ordinal);
            var targets = activeIds
                .Where(id => !banned.Contains(id))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList();

            var computedAt = DateTime.UtcNow;
            var rows = new List<UserRecommendation>();
            var users = 0;
            foreach (var profileId in targets)
            {
                var results = _scorer.Recommend(corpus, profileId);
                if (results.Count == 0)
                {
                    continue;
                }

                users++;
                rows.AddRange(results.Select((r, rank) => new UserRecommendation
                {
                    ProfileId = profileId,
                    ContentType = r.Content.Type,
                    ContentId = r.Content.Id,
                    Rank = rank,
                    Score = r.Score,
                    Reason = r.Reason.Length <= 100 ? r.Reason : r.Reason[..100],
                    SettingVersion = active.Version,
                    ComputedAt = computedAt,
                }));
            }

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
            await context.UserRecommendations.ExecuteDeleteAsync(cancellationToken);
            context.UserRecommendations.AddRange(rows);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            context.ChangeTracker.Clear();

            return new RecommendationRunResult(users, rows.Count, active.Version, computedAt);
        }

        /// <summary>
        /// 讀取計算推薦所需的資料。只讀取未刪除、未封鎖的作品；小說至少要有一個未刪除的章節
        /// </summary>
        /// <param name="now">現在時間</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>原始資料</returns>
        public async Task<RecommendationInput> LoadInputAsync(DateTime now, CancellationToken cancellationToken = default)
        {
            var novels = await context.Novels
                .Where(n => !n.IsDeleted && !n.Block && n.Episodes!.Any(e => !e.IsDeleted))
                .Select(n => new
                {
                    n.NovelID,
                    n.Title,
                    n.ProfileID,
                    AuthorName = n.Profile!.UserName,
                    n.CreateTime,
                    n.UpdateTime,
                    TagIds = n.Tags!.Where(t => t.Status == TagStatus.Approved).Select(t => t.TagId).ToList(),
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var articles = await context.Articles
                .Where(a => !a.IsDeleted && !a.Block && a.Type == ArticleType.General)
                .Select(a => new
                {
                    a.ArticleID,
                    a.Title,
                    a.ProfileID,
                    AuthorName = a.Profile!.UserName,
                    a.CreateTime,
                    a.UpdateTime,
                    TagIds = a.Tags!.Where(t => t.Status == TagStatus.Approved).Select(t => t.TagId).ToList(),
                })
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var contents = novels
                .Select(n => new ContentItem(ContentKey.Novel(n.NovelID), n.Title, n.ProfileID, n.AuthorName, n.TagIds, n.CreateTime, n.UpdateTime))
                .Concat(articles.Select(a => new ContentItem(ContentKey.Article(a.ArticleID), a.Title, a.ProfileID, a.AuthorName, a.TagIds, a.CreateTime, a.UpdateTime)))
                .ToList();

            var tags = await context.Tags
                .Where(t => t.Status == TagStatus.Approved)
                .Select(t => new { t.TagId, t.Name, t.IsCategory })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            var tagNames = tags.ToDictionary(t => t.TagId, t => t.Name);
            var categoryTagIds = tags.Where(t => t.IsCategory).Select(t => t.TagId).ToHashSet();

            var paidIds = await context.CoinTransactions
                .Where(t => t.TransactionType == CoinTransactionType.TopUp && t.Amount > 0)
                .Select(t => t.ProfileId)
                .Distinct()
                .ToListAsync(cancellationToken);
            var paid = paidIds.ToHashSet(StringComparer.Ordinal);

            var accounts = (await context.Profiles
                    .Select(p => new { p.ProfileID, p.EnrollmentDate, p.Banned, p.Prime })
                    .AsNoTracking()
                    .ToListAsync(cancellationToken))
                .Select(p => new ReaderAccount(p.ProfileID, p.EnrollmentDate, p.Banned, p.Prime || paid.Contains(p.ProfileID)))
                .ToList();

            var interactions = new List<Interaction>();

            var novelViewers = await context.Set<NovelViewer>()
                .Where(v => v.ProfileID != null)
                .Select(v => new { v.ProfileID, v.NovelID, v.ViewDate })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            interactions.AddRange(novelViewers.Select(v => new Interaction(v.ProfileID!, ContentKey.Novel(v.NovelID), InteractionKind.View, v.ViewDate)));

            var articleViewers = await context.Set<ArticleViewer>()
                .Where(v => v.ProfileID != null)
                .Select(v => new { v.ProfileID, v.ArticleID, v.ViewDate })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            interactions.AddRange(articleViewers.Select(v => new Interaction(v.ProfileID!, ContentKey.Article(v.ArticleID), InteractionKind.View, v.ViewDate)));

            // 近 30 天的觀看紀錄（含只讀章節、沒有進作品頁的讀者），每人每部只取最後一次
            var recentViews = await context.Views
                .Where(v => v.UserId != null)
                .GroupBy(v => new { v.UserId, v.NovelId, v.ArticleId })
                .Select(g => new { g.Key.UserId, g.Key.NovelId, g.Key.ArticleId, Last = g.Max(v => v.CreateTime) })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            interactions.AddRange(recentViews.Select(v => new Interaction(
                v.UserId!,
                v.NovelId is { } novelId ? ContentKey.Novel(novelId) : ContentKey.Article(v.ArticleId!.Value),
                InteractionKind.View,
                v.Last)));

            var follows = await context.Follows
                .Select(f => new { f.FollowerID, f.NovelID, f.ProfileID, f.UpdateTime })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            interactions.AddRange(follows
                .Where(f => f.NovelID != null)
                .Select(f => new Interaction(f.FollowerID, ContentKey.Novel(f.NovelID!.Value), InteractionKind.Follow, f.UpdateTime)));

            var involvings = await context.Involvings
                .Where(i => i.NovelID != null || i.ArticleID != null)
                .Select(i => new { i.InvolverID, i.NovelID, i.ArticleID, i.LastTime })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            interactions.AddRange(involvings.Select(i => new Interaction(
                i.InvolverID,
                i.NovelID is { } novelId ? ContentKey.Novel(novelId) : ContentKey.Article(i.ArticleID!.Value),
                InteractionKind.Involve,
                i.LastTime)));

            var readings = await context.EpisodeReads
                .Where(r => !r.Episode!.IsDeleted)
                .Select(r => new ReadingRecord(r.ProfileId, r.Episode!.NovelID, r.ReachedEnd))
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            var notInterested = (await context.RecommendationFeedbacks
                    .Select(f => new { f.ProfileId, f.ContentType, f.ContentId })
                    .AsNoTracking()
                    .ToListAsync(cancellationToken))
                .Select(f => (f.ProfileId, new ContentKey(f.ContentType, f.ContentId)))
                .ToList();

            var preferredCategories = (await context.ProfileCategoryPreferences
                    .Where(p => p.Tag!.IsCategory)
                    .Select(p => new { p.ProfileId, p.TagId })
                    .AsNoTracking()
                    .ToListAsync(cancellationToken))
                .Select(p => (p.ProfileId, p.TagId))
                .ToList();

            return new RecommendationInput
            {
                Now = now,
                Contents = contents,
                TagNames = tagNames,
                CategoryTagIds = categoryTagIds,
                Accounts = accounts,
                Interactions = interactions,
                Readings = readings,
                AuthorFollows = follows
                    .Where(f => f.ProfileID != null)
                    .Select(f => (f.FollowerID, f.ProfileID!))
                    .ToList(),
                NotInterested = notInterested,
                PreferredCategories = preferredCategories,
            };
        }
    }
}
