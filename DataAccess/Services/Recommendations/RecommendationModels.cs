using DataAccess.Models.Recommendation;

namespace DataAccess.Services.Recommendations
{
    /// <summary>
    /// 內容的識別：類型 + ID
    /// </summary>
    /// <param name="Type">內容類型</param>
    /// <param name="Id">作品或文章 ID</param>
    public readonly record struct ContentKey(RecommendationContentType Type, int Id) : IComparable<ContentKey>
    {
        /// <summary>
        /// 小說
        /// </summary>
        /// <param name="id">作品 ID</param>
        /// <returns>內容識別</returns>
        public static ContentKey Novel(int id) => new(RecommendationContentType.Novel, id);

        /// <summary>
        /// 文章
        /// </summary>
        /// <param name="id">文章 ID</param>
        /// <returns>內容識別</returns>
        public static ContentKey Article(int id) => new(RecommendationContentType.Article, id);

        /// <inheritdoc />
        public int CompareTo(ContentKey other)
        {
            var byType = Type.CompareTo(other.Type);
            return byType != 0 ? byType : Id.CompareTo(other.Id);
        }
    }

    /// <summary>
    /// 可以被推薦的內容（已排除刪除、封鎖）
    /// </summary>
    /// <param name="Key">內容識別</param>
    /// <param name="Title">標題</param>
    /// <param name="AuthorId">作者 ProfileID</param>
    /// <param name="AuthorName">作者用戶名</param>
    /// <param name="TagIds">正式標籤（包含分類標籤）</param>
    /// <param name="CreatedAt">建立時間</param>
    /// <param name="UpdatedAt">更新時間</param>
    public sealed record ContentItem(
        ContentKey Key,
        string Title,
        string AuthorId,
        string AuthorName,
        IReadOnlyList<int> TagIds,
        DateTime CreatedAt,
        DateTime UpdatedAt);

    /// <summary>
    /// 互動的種類
    /// </summary>
    public enum InteractionKind
    {
        /// <summary>
        /// 看過
        /// </summary>
        View,

        /// <summary>
        /// 追蹤作品
        /// </summary>
        Follow,

        /// <summary>
        /// 付費支持
        /// </summary>
        Involve,
    }

    /// <summary>
    /// 一位會員對一個內容的互動。同一種互動重複出現時，以最新的時間為準
    /// </summary>
    /// <param name="ProfileId">會員</param>
    /// <param name="Content">內容</param>
    /// <param name="Kind">互動種類</param>
    /// <param name="At">時間</param>
    public sealed record Interaction(string ProfileId, ContentKey Content, InteractionKind Kind, DateTime At);

    /// <summary>
    /// 章節閱讀（用來算讀完率）
    /// </summary>
    /// <param name="ProfileId">讀者</param>
    /// <param name="NovelId">作品 ID</param>
    /// <param name="ReachedEnd">是否讀到結尾</param>
    public sealed record ReadingRecord(string ProfileId, int NovelId, bool ReachedEnd);

    /// <summary>
    /// 會員帳號資訊（用來算可信度）
    /// </summary>
    /// <param name="ProfileId">會員</param>
    /// <param name="EnrolledAt">註冊時間</param>
    /// <param name="Banned">是否被封鎖</param>
    /// <param name="HasPaid">是否付費過（Prime 或儲值過）</param>
    public sealed record ReaderAccount(string ProfileId, DateTime EnrolledAt, bool Banned, bool HasPaid);

    /// <summary>
    /// 計算推薦所需的全部資料。全部是純資料，評分過程不碰資料庫，方便測試
    /// </summary>
    public sealed class RecommendationInput
    {
        /// <summary>
        /// 計算時間（與資料庫時間同一時區）
        /// </summary>
        public DateTime Now { get; init; }

        /// <summary>
        /// 可以被推薦的內容
        /// </summary>
        public IReadOnlyList<ContentItem> Contents { get; init; } = [];

        /// <summary>
        /// 正式標籤的名稱
        /// </summary>
        public IReadOnlyDictionary<int, string> TagNames { get; init; } = new Dictionary<int, string>();

        /// <summary>
        /// 分類標籤的 ID（<see cref="Models.Tag.IsCategory"/>）
        /// </summary>
        public IReadOnlySet<int> CategoryTagIds { get; init; } = new HashSet<int>();

        /// <summary>
        /// 會員帳號
        /// </summary>
        public IReadOnlyList<ReaderAccount> Accounts { get; init; } = [];

        /// <summary>
        /// 看過、追蹤、Involve
        /// </summary>
        public IReadOnlyList<Interaction> Interactions { get; init; } = [];

        /// <summary>
        /// 章節閱讀紀錄
        /// </summary>
        public IReadOnlyList<ReadingRecord> Readings { get; init; } = [];

        /// <summary>
        /// 追蹤作者：會員 → 作者 ProfileID
        /// </summary>
        public IReadOnlyList<(string FollowerId, string AuthorId)> AuthorFollows { get; init; } = [];

        /// <summary>
        /// 不感興趣
        /// </summary>
        public IReadOnlyList<(string ProfileId, ContentKey Content)> NotInterested { get; init; } = [];

        /// <summary>
        /// 自選的分類（冷啟動）：會員 → 分類標籤 ID
        /// </summary>
        public IReadOnlyList<(string ProfileId, int TagId)> PreferredCategories { get; init; } = [];
    }

    /// <summary>
    /// 一筆推薦結果
    /// </summary>
    /// <param name="Content">內容</param>
    /// <param name="Score">分數</param>
    /// <param name="Reason">推薦理由</param>
    public sealed record ScoredRecommendation(ContentKey Content, double Score, string Reason);
}
