using Involver.Common;
using Involver.Models.ViewModels.Navigation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Functions
{
    /// <summary>
    /// 工具箱：列出所有免費工具
    /// </summary>
    [AllowAnonymous]
    public class IndexModel : PageModel
    {
        /// <summary>
        /// 工具清單，和頁首的工具選單共用 <see cref="SiteNavigation.Tools"/>
        /// </summary>
        public IReadOnlyList<SiteToolLink> Tools => SiteNavigation.Tools;

        /// <summary>
        /// 顯示工具箱
        /// </summary>
        public void OnGet()
        {
            ViewData["Title"] = "工具";
        }
    }
}
