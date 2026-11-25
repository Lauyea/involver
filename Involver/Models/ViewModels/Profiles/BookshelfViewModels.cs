using Involver.Models.ViewModels.Feed;

namespace Involver.Models.ViewModels.Profiles;

/// <summary>
/// 書架上作品的閱讀狀態
/// </summary>
public enum BookshelfStatus
{
    /// <summary>
    /// 讀過一部分，還有未讀的章節
    /// </summary>
    Reading,

    /// <summary>
    /// 追蹤了但還沒讀過任何章節
    /// </summary>
    NotStarted,

    /// <summary>
    /// 每一集都讀過
    /// </summary>
    Finished,
}

/// <summary>
/// 書架上的一部作品：作品卡片加上閱讀進度
/// </summary>
public class BookshelfItemViewModel
{
    /// <summary>
    /// 作品卡片；EpisodeId 是接下來要讀的章節，讀完時為 null（前往作品頁）
    /// </summary>
    public required ContentCardViewModel Card { get; init; }

    /// <summary>
    /// 閱讀狀態
    /// </summary>
    public BookshelfStatus Status { get; init; }

    /// <summary>
    /// 讀過的章節數
    /// </summary>
    public int ReadCount { get; init; }

    /// <summary>
    /// 章節總數（不含已刪除）
    /// </summary>
    public int TotalCount { get; init; }

    /// <summary>
    /// 最後一次讀這部作品的時間；沒讀過為 null
    /// </summary>
    public DateTime? LastReadAt { get; init; }
}
