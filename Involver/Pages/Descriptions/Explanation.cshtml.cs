using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Descriptions
{
    /// <summary>
    /// 舊的「說明目錄」網址，已改為說明中心（/Descriptions）
    /// </summary>
    [AllowAnonymous]
    public class ExplanationModel : PageModel
    {
        /// <summary>
        /// 永久轉址到說明中心
        /// </summary>
        public IActionResult OnGet()
        {
            return RedirectToPagePermanent("./Index");
        }
    }
}
