using DataAccess.Models;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;

namespace Involver.Services;

/// <summary>
/// 觀看數累加服務。
/// 觀看數以單一 SQL UPDATE 原子累加，不經過 EF 追蹤實體的「讀出、改值、存回」，避免併發瀏覽互相覆寫而掉計數。
/// 傳入的實體若被 DbContext 追蹤，會同步把記憶體中的值 +1 供頁面顯示，且不會被標記為已修改，
/// 之後同一個 DbContext 的 SaveChanges 不會用舊值蓋回資料庫。
/// </summary>
public interface IViewCountService
{
    /// <summary>
    /// 累加創作的總觀看數與每日觀看數
    /// </summary>
    /// <param name="novel">被觀看的創作</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task AddNovelViewAsync(Novel novel, CancellationToken cancellationToken = default);

    /// <summary>
    /// 累加文章的總觀看數，並視需要累加每日觀看數
    /// </summary>
    /// <param name="article">被觀看的文章（含公告）</param>
    /// <param name="countDaily">是否一併累加每日觀看數</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task AddArticleViewAsync(Article article, bool countDaily = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// 累加章節的觀看數
    /// </summary>
    /// <param name="episode">被觀看的章節</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task AddEpisodeViewAsync(Episode episode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 累加個人頁面的觀看數
    /// </summary>
    /// <param name="profile">被觀看的個人頁面</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task AddProfileViewAsync(Profile profile, CancellationToken cancellationToken = default);
}
