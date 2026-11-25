using Involver.Models.ViewModels.Help;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Descriptions
{
    /// <summary>
    /// 隱私權政策
    /// </summary>
    [AllowAnonymous]
    public class PrivacyModel : PageModel
    {
        /// <summary>
        /// 頁面標頭；修改政策內容時要一併更新日期
        /// </summary>
        public HelpPageHeaderViewModel Header { get; } = new(
            "隱私權政策",
            "這份政策說明 Involver 蒐集哪些資料、用在哪裡、會交給誰，以及你可以怎麼管理自己的資料。",
            new DateOnly(2026, 11, 25));

        /// <summary>
        /// 顯示隱私權政策
        /// </summary>
        public void OnGet()
        {
        }
    }
}
