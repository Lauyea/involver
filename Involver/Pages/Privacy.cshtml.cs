using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages
{
    /// <summary>
    /// 專案範本留下的 /Privacy，改為轉址到正式的隱私權政策
    /// </summary>
    [AllowAnonymous]
    public class PrivacyModel : PageModel
    {
        /// <summary>
        /// 永久轉址到 /Descriptions/Privacy
        /// </summary>
        public IActionResult OnGet()
        {
            return RedirectToPagePermanent("/Descriptions/Privacy");
        }
    }
}
