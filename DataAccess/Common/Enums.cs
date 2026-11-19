using System.ComponentModel.DataAnnotations;

namespace DataAccess.Common;

// Voting enums
/// <summary>
/// 投票政策。
/// 自由模式可以自由選擇投票的價值。平等模式每張選票價值相同。
/// </summary>
public enum PolicyType
{
    Equality, Liberty
}
/// <summary>
/// 是否為競標模式。
/// 競標模式的話，可以出錢設置選項。
/// </summary>
public enum ModeType
{
    Normal, Bidding
}
/// <summary>
/// 投票截止的依據。
/// 限時：有限定時間內才能投票。
/// 限量：有限定投票數量。
/// 限值：有限定數值的投票。
/// </summary>
public enum LimitType
{
    Time, Number, Value
}

// 文章類型
public enum ArticleType
{
    [Display(Name = "一般文章")]
    General,

    [Display(Name = "公告")]
    Announcement,

    [Display(Name = "意見回饋")]
    Feedback
}

// 標籤 Enums
/// <summary>
/// 標籤狀態。
/// 待審標籤會顯示在作品上，但不會出現在建議清單、標籤搜尋和標籤雲裡。
/// </summary>
public enum TagStatus
{
    /// <summary>
    /// 正式
    /// </summary>
    [Display(Name = "正式")]
    Approved = 0,

    /// <summary>
    /// 待審
    /// </summary>
    [Display(Name = "待審")]
    Pending = 1
}

/// <summary>
/// 標籤使用的作品類型。小說和文章共用同一張標籤表，但使用次數、上限分開計算。
/// </summary>
public enum TagTarget
{
    /// <summary>
    /// 小說
    /// </summary>
    Novel,

    /// <summary>
    /// 文章
    /// </summary>
    Article
}

/// <summary>
/// 分類適用的作品類型（可複選）。例如奇幻適用小說，散文、評論只適用文章
/// </summary>
[Flags]
public enum CategoryScope : byte
{
    /// <summary>
    /// 不是分類
    /// </summary>
    [Display(Name = "無")]
    None = 0,

    /// <summary>
    /// 小說
    /// </summary>
    [Display(Name = "小說")]
    Novel = 1,

    /// <summary>
    /// 文章
    /// </summary>
    [Display(Name = "文章")]
    Article = 2,

    /// <summary>
    /// 小說與文章
    /// </summary>
    [Display(Name = "小說與文章")]
    All = Novel | Article,
}

/// <summary>
/// <see cref="CategoryScope"/> 的輔助方法
/// </summary>
public static class CategoryScopeExtensions
{
    /// <summary>
    /// 作品類型對應的分類範圍
    /// </summary>
    /// <param name="target">作品類型</param>
    /// <returns>分類範圍</returns>
    public static CategoryScope ToCategoryScope(this TagTarget target) =>
        target == TagTarget.Novel ? CategoryScope.Novel : CategoryScope.Article;
}