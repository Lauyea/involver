using DataAccess.Models.Recommendation;

namespace Involver.Models.ViewModels.Feed;

/// <summary>
/// 作品卡片（_ContentCardPartial）。首頁 Feed 的各區塊與作品列表共用
/// </summary>
public class ContentCardViewModel
{
    /// <summary>
    /// 內容類型
    /// </summary>
    public RecommendationContentType ContentType { get; init; }

    /// <summary>
    /// 作品或文章 ID
    /// </summary>
    public int ContentId { get; init; }

    /// <summary>
    /// 標題
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// 封面圖片網址；沒有時顯示預設圖
    /// </summary>
    public string ImageUrl { get; init; }

    /// <summary>
    /// 作者 ProfileID
    /// </summary>
    public string AuthorId { get; init; }

    /// <summary>
    /// 作者用戶名
    /// </summary>
    public string AuthorName { get; init; }

    /// <summary>
    /// 主分類名稱；文章為「文章」
    /// </summary>
    public string Category { get; init; }

    /// <summary>
    /// 正式標籤名稱（最多 3 個）
    /// </summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>
    /// 推薦理由，例如「因為你讀過《X》」
    /// </summary>
    public string Reason { get; init; }

    /// <summary>
    /// 角標，例如「3 集未讀」
    /// </summary>
    public string Badge { get; init; }

    /// <summary>
    /// 更新時間
    /// </summary>
    public DateTime UpdatedAt { get; init; }

    /// <summary>
    /// 點擊後前往的章節；null 時前往作品頁
    /// </summary>
    public int? EpisodeId { get; init; }

    /// <summary>
    /// 點擊後前往的章節標題（繼續閱讀用）
    /// </summary>
    public string EpisodeTitle { get; init; }

    /// <summary>
    /// 所在區塊；null 表示不在 Feed（不記錄曝光）
    /// </summary>
    public FeedSection? Section { get; init; }

    /// <summary>
    /// 在區塊內的位置
    /// </summary>
    public int Position { get; init; }

    /// <summary>
    /// 產生推薦時的設定版本
    /// </summary>
    public int? SettingVersion { get; init; }

    /// <summary>
    /// 是否顯示「不感興趣」
    /// </summary>
    public bool Dismissible { get; init; }

    /// <summary>
    /// 卡片的版型：直式（網格）或橫式（橫向捲動）
    /// </summary>
    public bool Compact { get; init; }

    /// <summary>
    /// 複製一份並設定區塊與位置
    /// </summary>
    /// <param name="section">區塊</param>
    /// <param name="position">位置</param>
    /// <returns>新的卡片</returns>
    public ContentCardViewModel At(FeedSection section, int position) => new()
    {
        ContentType = ContentType,
        ContentId = ContentId,
        Title = Title,
        ImageUrl = ImageUrl,
        AuthorId = AuthorId,
        AuthorName = AuthorName,
        Category = Category,
        Tags = Tags,
        Reason = Reason,
        Badge = Badge,
        UpdatedAt = UpdatedAt,
        EpisodeId = EpisodeId,
        EpisodeTitle = EpisodeTitle,
        Section = section,
        Position = position,
        SettingVersion = SettingVersion,
        Dismissible = Dismissible,
        Compact = Compact,
    };
}

/// <summary>
/// 進行中的投票（Feed「投票進行中」）
/// </summary>
public class FeedVotingViewModel
{
    /// <summary>
    /// 投票 ID
    /// </summary>
    public int VotingId { get; init; }

    /// <summary>
    /// 投票標題
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// 章節 ID
    /// </summary>
    public int EpisodeId { get; init; }

    /// <summary>
    /// 章節標題
    /// </summary>
    public string EpisodeTitle { get; init; }

    /// <summary>
    /// 作品 ID
    /// </summary>
    public int NovelId { get; init; }

    /// <summary>
    /// 作品標題
    /// </summary>
    public string NovelTitle { get; init; }

    /// <summary>
    /// 期限（限時投票）
    /// </summary>
    public DateTime? DeadLine { get; init; }

    /// <summary>
    /// 剩餘時間或進度，例如「剩 3 小時」、「12 / 50 人」
    /// </summary>
    public string Remaining { get; init; }

    /// <summary>
    /// 是否即將截止（24 小時內）
    /// </summary>
    public bool Urgent { get; init; }

    /// <summary>
    /// 目前參與人數
    /// </summary>
    public int Participants { get; init; }

    /// <summary>
    /// 是否來自追蹤中的作品（否則來自推薦或全站）
    /// </summary>
    public bool FromFollowed { get; init; }
}

/// <summary>
/// 會員首頁 Feed
/// </summary>
public class HomeFeedViewModel
{
    /// <summary>
    /// 這次頁面載入的識別碼，曝光與點擊用它對應
    /// </summary>
    public Guid FeedRequestId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// 繼續閱讀：追蹤中有新章節的作品
    /// </summary>
    public List<ContentCardViewModel> ContinueReading { get; init; } = [];

    /// <summary>
    /// 是否有追蹤任何作品（沒有新章節時顯示不同的提示）
    /// </summary>
    public bool HasFollowedNovels { get; init; }

    /// <summary>
    /// 投票進行中
    /// </summary>
    public List<FeedVotingViewModel> Votings { get; init; } = [];

    /// <summary>
    /// 為你推薦
    /// </summary>
    public List<ContentCardViewModel> Recommended { get; init; } = [];

    /// <summary>
    /// 是否顯示「選 3 個喜歡的分類」
    /// </summary>
    public bool NeedsCategoryPicker { get; init; }

    /// <summary>
    /// 可選的分類
    /// </summary>
    public IReadOnlyList<Involver.Services.CategoryOption> CategoryOptions { get; init; } = [];

    /// <summary>
    /// 已選的分類標籤 ID
    /// </summary>
    public IReadOnlyList<int> PreferredCategoryIds { get; init; } = [];

    /// <summary>
    /// 熱門
    /// </summary>
    public List<ContentCardViewModel> Trending { get; init; } = [];

    /// <summary>
    /// 新作
    /// </summary>
    public List<ContentCardViewModel> Newest { get; init; } = [];
}
