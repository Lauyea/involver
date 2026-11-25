using Involver.Models.ViewModels.Help;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Descriptions
{
    /// <summary>
    /// In 幣說明
    /// </summary>
    [AllowAnonymous]
    public class InCoinModel : PageModel
    {
        /// <summary>
        /// 頁面標頭
        /// </summary>
        public HelpPageHeaderViewModel Header { get; } = new(
            "In 幣說明",
            "In 幣怎麼取得、有什麼用途，以及創作分潤的比例。");

        /// <summary>
        /// 顯示In 幣說明
        /// </summary>
        public void OnGet()
        {
        }
    }
}
