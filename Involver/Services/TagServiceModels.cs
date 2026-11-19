using DataAccess.Common;
using DataAccess.Models;

using Involver.Helpers;

namespace Involver.Services;

/// <summary>
/// 使用者在標籤上的權限等級
/// </summary>
public enum TagAuthorLevel
{
    /// <summary>
    /// 被封鎖：不能使用、也不能新增標籤（只能保留作品原有的標籤）
    /// </summary>
    Banned,

    /// <summary>
    /// 一般作者：可以使用現有標籤；新增的標籤會先成為待審
    /// </summary>
    Author,

    /// <summary>
    /// 特約作家、管理員：新增的標籤直接生效
    /// </summary>
    Trusted
}

/// <summary>
/// <see cref="ITagService.ResolveTagsAsync"/> 的結果
/// </summary>
public class TagResolveResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Succeeded => ErrorMessage == null;

    /// <summary>
    /// 失敗原因，可直接顯示給使用者
    /// </summary>
    public string ErrorMessage { get; init; }

    /// <summary>
    /// 要設定到作品上的標籤（現有標籤與新增的標籤；新增的已加入 DbContext，隨作品一起 SaveChanges）
    /// </summary>
    public List<Tag> Tags { get; init; } = [];

    /// <summary>
    /// 這次新增的標籤
    /// </summary>
    public List<Tag> CreatedTags { get; init; } = [];

    /// <summary>
    /// 建立失敗的結果
    /// </summary>
    /// <param name="message">失敗原因</param>
    /// <returns>失敗的結果</returns>
    public static TagResolveResult Fail(string message) => new() { ErrorMessage = message };
}

/// <summary>
/// <see cref="ITagService.MergeAsync"/> 的結果
/// </summary>
public class TagMergeResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Succeeded => ErrorMessage == null;

    /// <summary>
    /// 失敗原因
    /// </summary>
    public string ErrorMessage { get; init; }

    /// <summary>
    /// 被合併的標籤名稱
    /// </summary>
    public string SourceName { get; init; }

    /// <summary>
    /// 保留的標籤名稱
    /// </summary>
    public string TargetName { get; init; }

    /// <summary>
    /// 新加上目標標籤的小說數（原本已有目標標籤的不重複計算）
    /// </summary>
    public int MovedNovels { get; init; }

    /// <summary>
    /// 新加上目標標籤的文章數
    /// </summary>
    public int MovedArticles { get; init; }

    /// <summary>
    /// 建立失敗的結果
    /// </summary>
    /// <param name="message">失敗原因</param>
    /// <returns>失敗的結果</returns>
    public static TagMergeResult Fail(string message) => new() { ErrorMessage = message };
}

/// <summary>
/// 建議清單（Tagify whitelist）的一筆標籤
/// </summary>
public class TagSuggestion
{
    /// <summary>
    /// 標籤 ID
    /// </summary>
    public int TagId { get; init; }

    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// 統一格式後的名稱
    /// </summary>
    public string NormalizedName { get; init; }

    /// <summary>
    /// 狀態
    /// </summary>
    public TagStatus Status { get; init; }

    /// <summary>
    /// 是否為分類標籤
    /// </summary>
    public bool IsCategory { get; init; }

    /// <summary>
    /// 使用中的小說數（不含軟刪除）
    /// </summary>
    public int NovelCount { get; init; }

    /// <summary>
    /// 使用中的文章數（不含軟刪除）
    /// </summary>
    public int ArticleCount { get; init; }

    /// <summary>
    /// 依作品類型取得使用次數
    /// </summary>
    /// <param name="target">作品類型</param>
    /// <returns>該類型的使用次數</returns>
    public int CountOf(TagTarget target) => target == TagTarget.Novel ? NovelCount : ArticleCount;
}

/// <summary>
/// 與輸入名稱相似的一筆現有標籤
/// </summary>
public class TagMatch
{
    /// <summary>
    /// 標籤 ID
    /// </summary>
    public int TagId { get; init; }

    /// <summary>
    /// 顯示名稱
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// 是否為待審標籤
    /// </summary>
    public bool IsPending { get; init; }

    /// <summary>
    /// 相似度（0～1）
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// 這個作品類型的使用次數
    /// </summary>
    public int UsageCount { get; init; }
}

/// <summary>
/// <see cref="ITagService.FindSimilarAsync"/> 的結果
/// </summary>
public class TagSimilarResult
{
    /// <summary>
    /// 整理後的顯示名稱
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// 統一格式後的名稱
    /// </summary>
    public string NormalizedName { get; init; }

    /// <summary>
    /// 名稱不合規定時的原因（太長、只有標點等），此時其他欄位沒有意義
    /// </summary>
    public string ErrorMessage { get; init; }

    /// <summary>
    /// 統一格式後完全相同的現有標籤（就是同一個標籤）
    /// </summary>
    public TagMatch Existing { get; init; }

    /// <summary>
    /// 只差結尾數字或符號的現有標籤：不能新增，必須改用它
    /// </summary>
    public TagMatch Duplicate { get; init; }

    /// <summary>
    /// 相似的現有標籤（依相似度排序）
    /// </summary>
    public List<TagMatch> Similar { get; init; } = [];
}

/// <summary>
/// 現有標籤的重複報表的一列
/// </summary>
public class TagDuplicateReportRow
{
    /// <summary>
    /// 類型：Duplicate（只差結尾數字或符號）、Similar（相似）、NormalizedNameMismatch（NormalizedName 與程式計算結果不同）
    /// </summary>
    public string Kind { get; init; }

    /// <summary>
    /// 標籤 A
    /// </summary>
    public TagSuggestion TagA { get; init; }

    /// <summary>
    /// 標籤 B；NormalizedNameMismatch 時為 null
    /// </summary>
    public TagSuggestion TagB { get; init; }

    /// <summary>
    /// 相似度（0～1）
    /// </summary>
    public double Score { get; init; }

    /// <summary>
    /// 補充說明（NormalizedNameMismatch 時是程式計算出的值）
    /// </summary>
    public string Note { get; init; }

    /// <summary>
    /// 由比對結果決定報表類型
    /// </summary>
    /// <param name="kind">比對結果</param>
    /// <returns>報表類型字串</returns>
    public static string KindOf(TagMatchKind kind) => kind switch
    {
        TagMatchKind.Same => "Same",
        TagMatchKind.Duplicate => "Duplicate",
        _ => "Similar",
    };
}
