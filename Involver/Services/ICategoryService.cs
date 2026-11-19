using DataAccess.Common;
using DataAccess.Models;

namespace Involver.Services;

/// <summary>
/// 分類（<see cref="Tag.IsCategory"/> 的標籤）
/// </summary>
/// <param name="TagId">標籤 ID</param>
/// <param name="Name">名稱</param>
/// <param name="Icon">Font Awesome 圖示（例如 <c>fa-hat-wizard</c>）</param>
/// <param name="Order">顯示順序</param>
/// <param name="Scope">適用的作品類型</param>
public sealed record CategoryOption(int TagId, string Name, string Icon, int Order, CategoryScope Scope)
{
    /// <summary>
    /// 是否適用於指定的作品類型
    /// </summary>
    /// <param name="target">作品類型</param>
    /// <returns>是否適用</returns>
    public bool AppliesTo(TagTarget target) => (Scope & target.ToCategoryScope()) != 0;
}

/// <summary>
/// 作品選定的分類
/// </summary>
public sealed class CategorySelectionResult
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Succeeded => ErrorMessage == null;

    /// <summary>
    /// 錯誤訊息
    /// </summary>
    public string ErrorMessage { get; init; }

    /// <summary>
    /// 選定的分類標籤（同一個 DbContext 追蹤中的實體，可以直接加到作品上）
    /// </summary>
    public List<Tag> Categories { get; init; } = [];

    /// <summary>
    /// 失敗
    /// </summary>
    /// <param name="message">錯誤訊息</param>
    /// <returns>結果</returns>
    public static CategorySelectionResult Fail(string message) => new() { ErrorMessage = message };
}

/// <summary>
/// 分類標籤：由管理員維護的固定分類，各自設定適用小說、文章或兩者。分類本身就是標籤，和一般標籤分開選，不佔一般標籤的名額。
/// 分類用在不適用的作品類型上時（例如文章用了只適用小說的「奇幻」），只是一般標籤
/// </summary>
public interface ICategoryService
{
    /// <summary>
    /// 預設圖示
    /// </summary>
    const string DefaultIcon = "fa-tag";

    /// <summary>
    /// 所有分類，依顯示順序（有快取）
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>分類</returns>
    Task<IReadOnlyList<CategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 適用於指定作品類型的分類，依顯示順序（有快取）
    /// </summary>
    /// <param name="target">作品類型</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>分類</returns>
    Task<IReadOnlyList<CategoryOption>> GetCategoriesAsync(TagTarget target, CancellationToken cancellationToken = default);

    /// <summary>
    /// 決定作品的分類：表單勾選的分類，加上一般標籤輸入框裡剛好是（適用這個作品類型的）分類的標籤
    /// </summary>
    /// <param name="selectedIds">勾選的分類標籤 ID</param>
    /// <param name="freeTags">一般標籤輸入框解析出的標籤</param>
    /// <param name="target">作品類型</param>
    /// <param name="required">是否至少要一個</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>選定的分類，或錯誤訊息</returns>
    Task<CategorySelectionResult> SelectAsync(
        IEnumerable<int> selectedIds,
        IEnumerable<Tag> freeTags,
        TagTarget target,
        bool required,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增分類：已有同名標籤（或別名）就把它設為分類，否則建立新的正式標籤
    /// </summary>
    /// <param name="name">名稱</param>
    /// <param name="icon">圖示；空白時用預設圖示</param>
    /// <param name="scope">適用的作品類型，至少一種</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>新增的分類，或錯誤訊息</returns>
    Task<(CategoryOption Category, string ErrorMessage)> AddAsync(string name, string icon, CategoryScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// 修改分類的圖示與適用範圍
    /// </summary>
    /// <param name="tagId">標籤 ID</param>
    /// <param name="icon">圖示；空白時用預設圖示</param>
    /// <param name="scope">適用的作品類型，至少一種</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>修改前後的摘要，或錯誤訊息</returns>
    Task<(string Name, string Before, string After, string ErrorMessage)> UpdateAsync(int tagId, string icon, CategoryScope scope, CancellationToken cancellationToken = default);

    /// <summary>
    /// 調整分類順序（和前一個或後一個交換）
    /// </summary>
    /// <param name="tagId">標籤 ID</param>
    /// <param name="offset">-1 往前、1 往後</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>交換位置的另一個分類名稱；找不到或已在最前、最後時為 null</returns>
    Task<string> MoveAsync(int tagId, int offset, CancellationToken cancellationToken = default);

    /// <summary>
    /// 移除分類：標籤和作品上的關聯都保留，只是不再是分類；會員選過的這個分類偏好會刪除
    /// </summary>
    /// <param name="tagId">標籤 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>分類名稱；找不到時為 null</returns>
    Task<string> RemoveAsync(int tagId, CancellationToken cancellationToken = default);
}
