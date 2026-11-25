using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;

using Microsoft.EntityFrameworkCore;

namespace Involver.Services.NotificationSetterService;

/// <summary>
/// 評論所在的頁面（創作、章節、文章、公告或意見回饋）
/// </summary>
/// <param name="Section">頁面所在的資料夾，例如 <see cref="Parameters.Novels"/></param>
/// <param name="Id">頁面的 ID</param>
/// <param name="KindLabel">給讀者看的種類名稱，例如「創作」</param>
/// <param name="Title">作品或文章的標題</param>
/// <param name="OwnerId">作品或文章的作者</param>
public sealed record CommentTarget(string Section, int Id, string KindLabel, string Title, string OwnerId)
{
    /// <summary>
    /// 頁面的站內網址
    /// </summary>
    /// <remarks>
    /// 小說頁的路由是 <c>@page "{id}"</c>，只接受 /Novels/Details/123；其他頁面用查詢字串 ?id=123。
    /// </remarks>
    public string DetailsUrl => Section == Parameters.Novels
        ? $"/{Section}/Details/{Id}"
        : $"/{Section}/Details?id={Id}";

    /// <summary>
    /// 捲動到指定評論的站內網址
    /// </summary>
    /// <param name="commentId">評論 ID</param>
    public string CommentUrl(int commentId) => $"{DetailsUrl}#comment-{commentId}";

    /// <summary>
    /// 捲動到指定評論並打開留言的站內網址
    /// </summary>
    /// <param name="commentId">評論 ID</param>
    public string RepliesUrl(int commentId) => $"{DetailsUrl}#comment-{commentId}-messages";
}

/// <summary>
/// 找出評論所在的頁面，讓通知可以連到正確的位置
/// </summary>
/// <remarks>
/// 評論只記錄 ArticleID／NovelID／EpisodeID，公告與意見回饋也是 Article，
/// 要看 <see cref="Article.Type"/> 才知道該連到 /Announcements 或 /Feedbacks。
/// </remarks>
public static class CommentTargets
{
    /// <summary>
    /// 依評論找出所在的頁面
    /// </summary>
    /// <param name="context">資料庫</param>
    /// <param name="comment">評論（只用到 ArticleID、NovelID、EpisodeID）</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>所在的頁面；作品已不存在時為 null</returns>
    public static async Task<CommentTarget> ResolveAsync(ApplicationDbContext context, Comment comment, CancellationToken cancellationToken = default)
    {
        if (comment.ArticleID is int articleId)
        {
            var article = await context.Articles
                .Where(a => a.ArticleID == articleId)
                .Select(a => new { a.Title, a.ProfileID, a.Type })
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (article == null)
            {
                return null;
            }

            var (section, label) = article.Type switch
            {
                ArticleType.Announcement => (Parameters.Announcements, "公告"),
                ArticleType.Feedback => (Parameters.Feedbacks, "意見回饋"),
                _ => (Parameters.Articles, "文章"),
            };

            return new CommentTarget(section, articleId, label, article.Title, article.ProfileID);
        }

        if (comment.NovelID is int novelId)
        {
            var novel = await context.Novels
                .Where(n => n.NovelID == novelId)
                .Select(n => new { n.Title, n.ProfileID })
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            return novel == null
                ? null
                : new CommentTarget(Parameters.Novels, novelId, "創作", novel.Title, novel.ProfileID);
        }

        if (comment.EpisodeID is int episodeId)
        {
            var episode = await context.Episodes
                .Where(e => e.EpisodeID == episodeId)
                .Select(e => new { e.Title, e.OwnerID })
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            return episode == null
                ? null
                : new CommentTarget(Parameters.Episodes, episodeId, "章節", episode.Title, episode.OwnerID);
        }

        return null;
    }

    /// <summary>
    /// 依評論 ID 找出所在的頁面
    /// </summary>
    /// <param name="context">資料庫</param>
    /// <param name="commentId">評論 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>所在的頁面；評論或作品已不存在時為 null</returns>
    public static async Task<CommentTarget> ResolveAsync(ApplicationDbContext context, int commentId, CancellationToken cancellationToken = default)
    {
        var comment = await context.Comments
            .Where(c => c.CommentID == commentId)
            .Select(c => new Comment
            {
                CommentID = c.CommentID,
                ArticleID = c.ArticleID,
                NovelID = c.NovelID,
                EpisodeID = c.EpisodeID,
                Content = string.Empty,
                ProfileID = c.ProfileID,
            })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        return comment == null ? null : await ResolveAsync(context, comment, cancellationToken);
    }
}
