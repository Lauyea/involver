using Involver.Models.ViewModels.Help;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Descriptions
{
    /// <summary>
    /// 服務條款
    /// </summary>
    [AllowAnonymous]
    public class TermsModel : PageModel
    {
        /// <summary>
        /// 頁面標頭；修改條款內容時要一併更新日期
        /// </summary>
        public HelpPageHeaderViewModel Header { get; } = new(
            "服務條款",
            "使用 Involver 前請閱讀這份條款。註冊帳號或使用本站，表示你同意本條款、隱私權政策與內容政策。",
            new DateOnly(2026, 11, 25));

        /// <summary>
        /// 顯示服務條款
        /// </summary>
        public void OnGet()
        {
        }
    }
}
