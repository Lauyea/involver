using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Novels
{
    [AllowAnonymous]
    public class TagsModel : PageModel
    {
        public void OnGet()
        {
        }
    }
}
