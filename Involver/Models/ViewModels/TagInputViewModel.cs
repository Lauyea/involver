using DataAccess.Common;

using Involver.Services;

namespace Involver.Models.ViewModels;

/// <summary>
/// 小說、文章 Create/Edit 頁面共用的標籤輸入框（_TagInputPartial）
/// </summary>
public class TagInputViewModel
{
    /// <summary>
    /// 表單欄位名稱（對應 PageModel 的 BindProperty）
    /// </summary>
    public string FieldName { get; init; } = "TagString";

    /// <summary>
    /// 目前的值（Tagify JSON）
    /// </summary>
    public string Value { get; init; }

    /// <summary>
    /// 作品類型
    /// </summary>
    public TagTarget Target { get; init; }

    /// <summary>
    /// 使用者的權限等級
    /// </summary>
    public TagAuthorLevel Level { get; init; }

    /// <summary>
    /// 每部作品最多的標籤數
    /// </summary>
    public int MaxTags => TagService.GetMaxTags(Target);

    /// <summary>
    /// 給 API 用的作品類型字串
    /// </summary>
    public string TargetName => Target == TagTarget.Novel ? "novel" : "article";

    /// <summary>
    /// 新增的標籤是否直接生效（特約作家、管理員）
    /// </summary>
    public bool CanCreateDirectly => Level == TagAuthorLevel.Trusted;
}
