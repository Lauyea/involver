using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Articles
{
    [AllowAnonymous]
    public class TagsModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
