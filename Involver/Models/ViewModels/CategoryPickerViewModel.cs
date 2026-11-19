using DataAccess.Common;

using Involver.Services;

namespace Involver.Models.ViewModels;

/// <summary>
/// 新增、編輯作品時的分類選擇（_CategoryPickerPartial）
/// </summary>
public class CategoryPickerViewModel
{
    /// <summary>
    /// 可選的分類
    /// </summary>
    public IReadOnlyList<CategoryOption> Options { get; init; } = [];

    /// <summary>
    /// 已選的分類標籤 ID
    /// </summary>
    public IReadOnlyCollection<int> SelectedIds { get; init; } = [];

    /// <summary>
    /// 是否至少要選一個（小說必選、文章可選）
    /// </summary>
    public bool Required { get; init; }

    /// <summary>
    /// 表單欄位名稱（對應 PageModel 的 <c>CategoryIds</c>）
    /// </summary>
    public string Name { get; init; } = "CategoryIds";

    /// <summary>
    /// 最多可選幾個
    /// </summary>
    public int Max { get; init; } = Parameters.CategoryMaxPerWork;
}
