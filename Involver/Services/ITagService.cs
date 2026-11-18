using System.Security.Claims;

using DataAccess.Common;

namespace Involver.Services;

/// <summary>
/// 標籤服務。小說、文章的 Create/Edit 頁面與標籤 API 共用的標籤邏輯都集中在這裡：
/// 解析 Tagify 的值 → 統一格式 → 檢查數量和長度 → 比對相似 → 找到或新增標籤。
/// </summary>
public interface ITagService
{
    /// <summary>
    /// 取得使用者在標籤上的權限等級
    /// </summary>
    /// <param name="user">目前的使用者（用來判斷管理角色）</param>
    /// <param name="profileId">使用者 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>權限等級</returns>
    Task<TagAuthorLevel> GetAuthorLevelAsync(ClaimsPrincipal user, string profileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 把表單送出的標籤字串轉成要設定到作品上的標籤。
    /// 找不到的標籤會新增並加入 DbContext（一般作者新增的是待審標籤），由呼叫端和作品一起 SaveChanges。
    /// </summary>
    /// <param name="tagString">Tagify 的值（JSON），或以逗號分隔的標籤</param>
    /// <param name="target">作品類型</param>
    /// <param name="profileId">送出表單的使用者 ID（新增標籤的建立者）</param>
    /// <param name="level">使用者的權限等級</param>
    /// <param name="currentTagIds">作品目前的標籤 ID（編輯時）。被封鎖的使用者仍可保留這些標籤</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>處理結果；失敗時含可顯示給使用者的原因</returns>
    Task<TagResolveResult> ResolveTagsAsync(
        string tagString,
        TagTarget target,
        string profileId,
        TagAuthorLevel level,
        IReadOnlyCollection<int> currentTagIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 待審標籤被足夠多位不同作者使用時自動轉正。在作品 SaveChanges 之後呼叫。
    /// </summary>
    /// <param name="tagIds">剛設定到作品上的標籤 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>這次轉正的標籤數</returns>
    Task<int> PromotePendingTagsAsync(IEnumerable<int> tagIds, CancellationToken cancellationToken = default);

    /// <summary>
    /// 變更標籤狀態（管理員核准待審標籤）
    /// </summary>
    /// <param name="tagId">標籤 ID</param>
    /// <param name="status">新狀態</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>找不到標籤時為 false</returns>
    Task<bool> UpdateStatusAsync(int tagId, TagStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// 建議清單（Tagify whitelist）：只列正式標籤，該作品類型用過的優先，依使用次數排序
    /// </summary>
    /// <param name="target">作品類型</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>排序後的標籤</returns>
    Task<List<TagSuggestion>> GetSuggestionsAsync(TagTarget target, CancellationToken cancellationToken = default);

    /// <summary>
    /// 找出與輸入名稱相同、只差結尾數字或符號、或相似的現有標籤（含待審標籤）
    /// </summary>
    /// <param name="name">作者輸入的名稱</param>
    /// <param name="target">作品類型（影響排序）</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>比對結果</returns>
    Task<TagSimilarResult> FindSimilarAsync(string name, TagTarget target, CancellationToken cancellationToken = default);

    /// <summary>
    /// 掃描現有標籤，列出可能重複的組合與 NormalizedName 不一致的標籤，給人確認後再合併
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>報表，依類型與相似度排序</returns>
    Task<List<TagDuplicateReportRow>> GetDuplicateReportAsync(CancellationToken cancellationToken = default);
}
