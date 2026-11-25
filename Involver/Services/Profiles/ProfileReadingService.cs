using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.Recommendation;

using Involver.Models.ViewModels.Feed;
using Involver.Models.ViewModels.Profiles;
using Involver.Services.Recommendations;

using Microsoft.EntityFrameworkCore;

namespace Involver.Services.Profiles;

/// <summary>
/// 個人檔案的閱讀相關資料：作品卡片列表、書架（追蹤作品的閱讀進度）
/// </summary>
public interface IProfileReadingService
{
    /// <summary>
    /// 書架：追蹤中的作品與閱讀進度，依閱讀中、未開始、已讀完分組後，各組依最近的動態排序
    /// </summary>
    /// <param name="profileId">讀者</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>書架上的作品</returns>
    Task<List<BookshelfItemViewModel>> GetBookshelfAsync(string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 某位會員發表的創作或一般文章，依更新時間由新到舊分頁
    /// </summary>
    /// <param name="profileId">作者</param>
    /// <param name="type">創作或文章</param>
    /// <param name="pageIndex">頁碼，從 1 開始</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>作品卡片</returns>
    Task<List<ContentCardViewModel>> GetWorksAsync(string profileId, RecommendationContentType type, int pageIndex, CancellationToken cancellationToken = default);

    /// <summary>
    /// 某位會員最近更新的創作與文章（混合）
    /// </summary>
    /// <param name="profileId">作者</param>
    /// <param name="count">筆數</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>作品卡片，依更新時間由新到舊</returns>
    Task<List<ContentCardViewModel>> GetLatestWorksAsync(string profileId, int count, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
/// <param name="context">資料庫內容</param>
/// <param name="homeFeedService">組作品卡片</param>
public class ProfileReadingService(ApplicationDbContext context, IHomeFeedService homeFeedService) : IProfileReadingService
{
    /// <summary>
    /// 書架最多顯示幾部作品（依作品更新時間取最新的）
    /// </summary>
    public const int MaxBookshelfCount = 100;

    /// <inheritdoc />
    public async Task<List<BookshelfItemViewModel>> GetBookshelfAsync(string profileId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(profileId);

        var novelIds = await context.Follows
            .Where(f => f.FollowerID == profileId && f.NovelID != null && !f.Novel!.IsDeleted && !f.Novel.Block)
            .OrderByDescending(f => f.Novel!.UpdateTime)
            .Select(f => f.NovelID!.Value)
            .Take(MaxBookshelfCount)
            .ToListAsync(cancellationToken);
        novelIds = novelIds.Distinct().ToList();
        if (novelIds.Count == 0)
        {
            return [];
        }

        var cards = await homeFeedService.LoadNovelCardsAsync(novelIds, cancellationToken);

        var episodes = await context.Episodes
            .Where(e => novelIds.Contains(e.NovelID) && !e.IsDeleted)
            .Select(e => new { e.EpisodeID, e.NovelID, e.Title, e.UpdateTime })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var reads = await context.EpisodeReads
            .Where(r => r.ProfileId == profileId && novelIds.Contains(r.Episode!.NovelID) && !r.Episode.IsDeleted)
            .Select(r => new { r.EpisodeId, r.LastReadAt, r.ReachedEnd })
            .AsNoTracking()
            .ToDictionaryAsync(r => r.EpisodeId, cancellationToken);

        var episodesByNovel = episodes.ToLookup(e => e.NovelID);
        var items = new List<(BookshelfItemViewModel Item, DateTime Activity)>();
        foreach (var novelId in novelIds)
        {
            if (!cards.TryGetValue(novelId, out var card))
            {
                continue;
            }

            var ordered = episodesByNovel[novelId].OrderBy(e => e.EpisodeID).ToList();
            if (ordered.Count == 0)
            {
                // 追蹤了但作者還沒發表章節：放在「還沒開始」，點了前往作品頁
                items.Add((new BookshelfItemViewModel
                {
                    Card = WithProgress(card, card.UpdatedAt, "尚無章節", null),
                    Status = BookshelfStatus.NotStarted,
                }, card.UpdatedAt));
                continue;
            }

            var readEpisodes = ordered.Where(e => reads.ContainsKey(e.EpisodeID)).ToList();
            var lastRead = readEpisodes
                .OrderByDescending(e => reads[e.EpisodeID].LastReadAt)
                .FirstOrDefault();

            // 接下來要讀的：上次沒讀完就回到那一集；讀完了就接下一集；後面都讀過時，找前面漏掉的
            var next = lastRead == null
                ? ordered[0]
                : !reads[lastRead.EpisodeID].ReachedEnd
                    ? lastRead
                    : ordered.FirstOrDefault(e => e.EpisodeID > lastRead.EpisodeID && !reads.ContainsKey(e.EpisodeID))
                        ?? ordered.FirstOrDefault(e => !reads.ContainsKey(e.EpisodeID));

            var unread = ordered.Count - readEpisodes.Count;
            var status = readEpisodes.Count == 0
                ? BookshelfStatus.NotStarted
                : next == null ? BookshelfStatus.Finished : BookshelfStatus.Reading;
            var latestUpdate = ordered.Max(e => e.UpdateTime);
            DateTime? lastReadAt = lastRead == null ? null : reads[lastRead.EpisodeID].LastReadAt;

            var badge = status switch
            {
                BookshelfStatus.Finished => "已讀完",
                BookshelfStatus.NotStarted => $"共 {ordered.Count} 集",
                _ => unread > 0 ? $"{unread} 集未讀" : "讀到一半",
            };

            items.Add((new BookshelfItemViewModel
            {
                Card = WithProgress(card, latestUpdate, badge, status == BookshelfStatus.Finished || next == null ? null : (next.EpisodeID, next.Title)),
                Status = status,
                ReadCount = readEpisodes.Count,
                TotalCount = ordered.Count,
                LastReadAt = lastReadAt,
            }, lastReadAt > latestUpdate ? lastReadAt.Value : latestUpdate));
        }

        return items
            .OrderBy(x => x.Item.Status)
            .ThenByDescending(x => x.Activity)
            .ThenBy(x => x.Item.Card.ContentId)
            .Select(x => x.Item)
            .ToList();
    }

    /// <summary>
    /// 複製作品卡片，加上閱讀進度的角標與接下來要讀的章節
    /// </summary>
    /// <param name="card">原本的卡片</param>
    /// <param name="updatedAt">最新章節的更新時間</param>
    /// <param name="badge">角標</param>
    /// <param name="next">接下來要讀的章節；null 時前往作品頁</param>
    /// <returns>新的卡片</returns>
    private static ContentCardViewModel WithProgress(ContentCardViewModel card, DateTime updatedAt, string badge, (int EpisodeID, string Title)? next) => new()
    {
        ContentType = card.ContentType,
        ContentId = card.ContentId,
        Title = card.Title,
        ImageUrl = card.ImageUrl,
        AuthorId = card.AuthorId,
        AuthorName = card.AuthorName,
        Category = card.Category,
        Tags = card.Tags,
        UpdatedAt = updatedAt,
        Badge = badge,
        EpisodeId = next?.EpisodeID,
        EpisodeTitle = next?.Title,
    };

    /// <inheritdoc />
    public async Task<List<ContentCardViewModel>> GetWorksAsync(string profileId, RecommendationContentType type, int pageIndex, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(profileId);
        var skip = (Math.Max(pageIndex, 1) - 1) * Parameters.PageSize;

        if (type == RecommendationContentType.Novel)
        {
            var novelIds = await context.Novels
                .Where(n => n.ProfileID == profileId && !n.IsDeleted && !n.Block)
                .OrderByDescending(n => n.UpdateTime)
                .ThenByDescending(n => n.NovelID)
                .Skip(skip)
                .Take(Parameters.PageSize)
                .Select(n => n.NovelID)
                .ToListAsync(cancellationToken);
            var novels = await homeFeedService.LoadNovelCardsAsync(novelIds, cancellationToken);
            return novelIds.Where(novels.ContainsKey).Select(id => novels[id]).ToList();
        }

        var articleIds = await context.Articles
            .Where(a => a.ProfileID == profileId && !a.IsDeleted && !a.Block && a.Type == ArticleType.General)
            .OrderByDescending(a => a.UpdateTime)
            .ThenByDescending(a => a.ArticleID)
            .Skip(skip)
            .Take(Parameters.PageSize)
            .Select(a => a.ArticleID)
            .ToListAsync(cancellationToken);
        var articles = await homeFeedService.LoadArticleCardsAsync(articleIds, cancellationToken);
        return articleIds.Where(articles.ContainsKey).Select(id => articles[id]).ToList();
    }

    /// <inheritdoc />
    public async Task<List<ContentCardViewModel>> GetLatestWorksAsync(string profileId, int count, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(profileId);

        var novelIds = await context.Novels
            .Where(n => n.ProfileID == profileId && !n.IsDeleted && !n.Block)
            .OrderByDescending(n => n.UpdateTime)
            .Take(count)
            .Select(n => n.NovelID)
            .ToListAsync(cancellationToken);
        var articleIds = await context.Articles
            .Where(a => a.ProfileID == profileId && !a.IsDeleted && !a.Block && a.Type == ArticleType.General)
            .OrderByDescending(a => a.UpdateTime)
            .Take(count)
            .Select(a => a.ArticleID)
            .ToListAsync(cancellationToken);

        var novels = await homeFeedService.LoadNovelCardsAsync(novelIds, cancellationToken);
        var articles = await homeFeedService.LoadArticleCardsAsync(articleIds, cancellationToken);
        return novels.Values
            .Concat(articles.Values)
            .OrderByDescending(c => c.UpdatedAt)
            .Take(count)
            .ToList();
    }
}
