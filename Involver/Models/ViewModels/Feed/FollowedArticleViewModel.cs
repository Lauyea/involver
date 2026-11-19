namespace Involver.Models.ViewModels.Feed;

/// <summary>
/// 追蹤文章列表的一筆文章
/// </summary>
public class FollowedArticleViewModel
{
    /// <summary>
    /// 文章 ID
    /// </summary>
    public int ArticleId { get; init; }

    /// <summary>
    /// 標題
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// 純文字預覽（已去除 HTML）
    /// </summary>
    public required string Preview { get; init; }

    /// <summary>
    /// 封面圖片網址；沒有時為 null
    /// </summary>
    public string ImageUrl { get; init; }

    /// <summary>
    /// 作者 ProfileID
    /// </summary>
    public required string AuthorId { get; init; }

    /// <summary>
    /// 作者名稱
    /// </summary>
    public required string AuthorName { get; init; }

    /// <summary>
    /// 作者頭像網址；沒有時為 null，顯示名稱第一個字
    /// </summary>
    public string AuthorImageUrl { get; init; }

    /// <summary>
    /// 最後更新時間
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// 總觀看數
    /// </summary>
    public int Views { get; init; }

    /// <summary>
    /// 留言數
    /// </summary>
    public int Comments { get; init; }

    /// <summary>
    /// 正式標籤名稱
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// 頭像沒有圖片時顯示的字（名稱第一個字，支援中文與 emoji）
    /// </summary>
    public string AuthorInitial => string.IsNullOrEmpty(AuthorName)
        ? "?"
        : AuthorName[..System.Globalization.StringInfo.GetNextTextElementLength(AuthorName)].ToUpperInvariant();
}
