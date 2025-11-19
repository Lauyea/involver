using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;

namespace Involver.Services;

/// <summary>
/// 一個自訂的 IHtmlGenerator 實作，
/// 它將 DataAnnotations 驗證屬性轉譯為 HTML5 原生的驗證屬性 (constraint validation attributes)，
/// 而不是 ASP.NET Core 預設的 data-val-* 屬性（for jquery-validation）。
/// </summary>
public class Html5ValidationHtmlGenerator : DefaultHtmlGenerator
{
    private readonly IModelMetadataProvider _metadataProvider;

    /// <summary>
    /// 初始化 Html5ValidationHtmlGenerator
    /// </summary>
    public Html5ValidationHtmlGenerator(
        IAntiforgery antiforgery,
        IOptions<MvcViewOptions> optionsAccessor,
        IModelMetadataProvider metadataProvider,
        IUrlHelperFactory urlHelperFactory,
        HtmlEncoder htmlEncoder,
        ValidationHtmlAttributeProvider validationAttributeProvider)
        : base(antiforgery, optionsAccessor, metadataProvider, urlHelperFactory, htmlEncoder, validationAttributeProvider)
    {
        _metadataProvider = metadataProvider;
    }

    /// <summary>
    /// 覆寫 AddValidationAttributes 方法，以產生 HTML5 原生屬性
    /// </summary>
    protected override void AddValidationAttributes(
        ViewContext viewContext,
        TagBuilder tagBuilder,
        ModelExplorer modelExplorer,
        string expression)
    {
        // 處理 modelExplorer 為 null 的情況
        if (modelExplorer == null)
        {
            // 用公開的 GetExplorerForProperty 來嘗試解析運算式。
            modelExplorer = viewContext.ViewData.ModelExplorer.GetExplorerForProperty(expression);

            if (modelExplorer == null)
            {
                // 如果運算式太複雜，GetExplorerForProperty 可能會回傳 null。
                // 在這種情況下，我們回退到使用 string 的中繼資料，
                // 這模仿了原始 ExpressionMetadataProvider 的回退邏輯。
                // 這將導致不會產生任何驗證屬性 (因為 string 類型上沒有驗證)。
                var stringMetadata = _metadataProvider.GetMetadataForType(typeof(string));
                modelExplorer = viewContext.ViewData.ModelExplorer.GetExplorerForExpression(stringMetadata, modelAccessor: null);
            }
        }

        // 獲取顯示名稱，用於格式化錯誤訊息
        //var displayName = modelExplorer.Metadata.DisplayName ?? modelExplorer.Metadata.PropertyName ?? expression;

        // 遍歷模型上的所有驗證中繼資料
        foreach (var attribute in modelExplorer.Metadata.ValidatorMetadata)
        {
            // 我們只關心 ValidationAttribute
            if (attribute is ValidationAttribute validationAttribute)
            {
                // --- 屬性對應 ---

                if (attribute is RequiredAttribute)
                {
                    // 對應： [Required] -> required
                    tagBuilder.MergeAttribute("required", "required");
                }
                else if (attribute is StringLengthAttribute stringLengthAttr)
                {
                    // maxlength 已經由基底類別的 AddMaxLengthAttribute 處理
                    if (stringLengthAttr.MinimumLength > 0)
                    {
                        // 對應： [StringLength(MinimumLength = N)] -> minlength="N"
                        tagBuilder.MergeAttribute("minlength", stringLengthAttr.MinimumLength.ToString());
                    }
                }
                else if (attribute is MinLengthAttribute minLengthAttr)
                {
                    // 對應： [MinLength(N)] -> minlength="N"
                    tagBuilder.MergeAttribute("minlength", minLengthAttr.Length.ToString());
                }
                else if (attribute is RangeAttribute rangeAttr)
                {
                    // 對應： [Range(min, max)] -> min="min" max="max"
                    tagBuilder.MergeAttribute("min", rangeAttr.Minimum.ToString());
                    tagBuilder.MergeAttribute("max", rangeAttr.Maximum.ToString());
                }
                else if (attribute is RegularExpressionAttribute regexAttr)
                {
                    // 對應： [RegularExpression(pattern)] -> pattern="pattern"
                    tagBuilder.MergeAttribute("pattern", regexAttr.Pattern);
                }

                // --- 錯誤訊息對應 ---
                // 將 DataAnnotations 的 ErrorMessage 放入 'title' 屬性
                //AddTitleAttribute(tagBuilder, validationAttribute, displayName);
            }
        }

        // **重要**：
        // 我們 *不要* 呼叫基底類別的實作
        // base.AddValidationAttributes(viewContext, tagBuilder, modelExplorer, expression);
    }

    /// <summary>
    /// 輔助方法：將驗證錯誤訊息添加到 'title' 屬性中
    /// </summary>
    private static void AddTitleAttribute(TagBuilder tagBuilder, ValidationAttribute attribute, string displayName)
    {
        if (string.IsNullOrEmpty(attribute.ErrorMessage))
        {
            return;
        }

        string errorMessage = attribute.FormatErrorMessage(displayName);

        if (tagBuilder.Attributes.TryGetValue("title", out var existingTitle))
        {
            tagBuilder.Attributes["title"] = $"{existingTitle}\n{errorMessage}";
        }
        else
        {
            tagBuilder.Attributes["title"] = errorMessage;
        }
    }
}