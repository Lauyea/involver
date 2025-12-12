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
            public string Url { get; set; }
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
                    Url = "./CreateCover",
                    IconClass = "fa-solid fa-image"
                },
                new ToolInfo
                {
                    Title = "Markdown 轉換器",
                    Description = "將 Markdown 語法轉換為 HTML，並提供即時預覽。",
                    Url = "./MarkdownToHtml",
                    IconClass = "fa-solid fa-code"
                }
            };
        }
    }
}
