using Involver.Models.ViewModels.Help;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Descriptions
{
    /// <summary>
    /// 內容政策
    /// </summary>
    [AllowAnonymous]
    public class ContentModel : PageModel
    {
        /// <summary>
        /// 頁面標頭；修改政策內容時要一併更新日期
        /// </summary>
        public HelpPageHeaderViewModel Header { get; } = new(
            "內容政策",
            "Involver 是讓讀者參與創作的平台。為了讓大家放心創作與討論，發布內容前請先閱讀以下規範。",
            new DateOnly(2026, 11, 25));

        /// <summary>
        /// 顯示內容政策
        /// </summary>
        public void OnGet()
        {
        }
    }
}
