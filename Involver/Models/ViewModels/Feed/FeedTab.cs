namespace Involver.Models.ViewModels.Feed;

/// <summary>
/// 動態頁的分頁標籤（首頁、追蹤文章、熱門創作共用 <c>_FeedTabsPartial</c>）
/// </summary>
public enum FeedTab
{
    /// <summary>
    /// 會員首頁（為你推薦）
    /// </summary>
    Home,

    /// <summary>
    /// 追蹤的創作者發表的文章
    /// </summary>
    FollowArticles,

    /// <summary>
    /// 熱門創作
    /// </summary>
    Trending,
}
