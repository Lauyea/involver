using Involver.Models.ViewModels.Help;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Descriptions
{
    /// <summary>
    /// 投票說明
    /// </summary>
    [AllowAnonymous]
    public class VotingModel : PageModel
    {
        /// <summary>
        /// 頁面標頭
        /// </summary>
        public HelpPageHeaderViewModel Header { get; } = new(
            "投票說明",
            "投票的形式與模式。");

        /// <summary>
        /// 顯示投票說明
        /// </summary>
        public void OnGet()
        {
        }
    }
}
