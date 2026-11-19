using DataAccess.Common;
using DataAccess.Models;

namespace Involver.Models.ViewModels;

/// <summary>
/// 作品上的標籤徽章（_TagBadgesPartial）
/// </summary>
public class TagBadgesViewModel
{
    /// <summary>
    /// 作品的標籤
    /// </summary>
    public IEnumerable<Tag> Tags { get; init; } = [];

    /// <summary>
    /// 作品類型：只有適用這個類型的分類才用分類樣式顯示
    /// </summary>
    public TagTarget Target { get; init; }

    /// <summary>
    /// 徽章的 CSS class（不含顏色），例如 "badge me-1"
    /// </summary>
    public string CssClass { get; init; } = "badge me-1";
}
