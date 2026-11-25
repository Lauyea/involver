using Involver.Models.ViewModels.Help;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Descriptions
{
    /// <summary>
    /// 說明中心：列出所有說明頁與政策頁
    /// </summary>
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        /// <summary>
        /// 頁面標頭
        /// </summary>
        public HelpPageHeaderViewModel Header { get; } = new(
            "說明中心",
            "Involver 的使用說明與各項政策都在這裡。");

        /// <summary>
        /// 依分組列出的說明主題
        /// </summary>
        public IReadOnlyList<HelpTopicGroupViewModel> Groups { get; } =
        [
            new("使用說明",
            [
                new("In 幣說明", "In 幣怎麼取得、有什麼用途，以及創作分潤的比例。", "fa-solid fa-coins", "/Descriptions/InCoin"),
                new("投票說明", "限時、限量、限值等投票形式與投票模式的差別。", "fa-solid fa-square-poll-vertical", "/Descriptions/Voting"),
                new("工具箱", "製作封面、賽程表等免費工具，不用登入也能使用。", "fa-solid fa-toolbox", "/Functions/Index"),
            ]),
            new("政策",
            [
                new("隱私權政策", "我們蒐集哪些資料、用在哪裡，以及你可以怎麼管理。", "fa-solid fa-user-shield", "/Descriptions/Privacy"),
                new("服務條款", "使用本站的規則、你的內容授權與違規處理。", "fa-solid fa-file-contract", "/Descriptions/Terms"),
                new("內容政策", "哪些內容可以發布、哪些不行，以及如何檢舉。", "fa-solid fa-scale-balanced", "/Descriptions/Content"),
            ]),
        ];

        /// <summary>
        /// 顯示說明中心
        /// </summary>
        public void OnGet()
        {
        }
    }
}
