using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Functions
{
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        public List<ToolInfo> Tools { get; set; }

        public class ToolInfo
        {
            public string Title { get; set; }
            public string Description { get; set; }
            /// <summary>
            /// 工具頁面的 Razor Page 名稱（絕對路徑），由 asp-page 產生網址；
            /// 不用相對網址，否則從 /Functions（結尾沒有斜線）點進去會變成 /CreateCover 這種少了一層的路徑。
            /// </summary>
            public string PageName { get; set; }
            public string IconClass { get; set; }
        }

        public void OnGet()
        {
            ViewData["Title"] = "工具";
            Tools = new List<ToolInfo>
            {
                new ToolInfo
                {
                    Title = "製作小說封面",
                    Description = "為您的小說生成獨一無二的封面。",
                    PageName = "/Functions/CreateCover",
                    IconClass = "fa-solid fa-image"
                },
                new ToolInfo
                {
                    Title = "Markdown 轉換器",
                    Description = "將 Markdown 語法轉換為 HTML，並提供即時預覽。",
                    PageName = "/Functions/MarkdownToHtml",
                    IconClass = "fa-solid fa-code"
                },
                new ToolInfo
                {
                    Title = "建立小說試閱",
                    Description = "設定閱讀密碼與有效期限，建立專屬的受保護小說試閱連結。",
                    PageName = "/Functions/Preview",
                    IconClass = "fa-solid fa-lock"
                },
                new ToolInfo
                {
                    Title = "賽程表產生器",
                    Description = "建立單淘汰賽程，支援種子排位、比分與季軍戰，並可匯出 HTML。",
                    PageName = "/Functions/BracketBuilder",
                    IconClass = "fa-solid fa-trophy"
                }
            };
        }
    }
}
