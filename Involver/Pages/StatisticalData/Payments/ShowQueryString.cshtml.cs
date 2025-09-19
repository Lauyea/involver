using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.StatisticalData.Payments
{
    [AllowAnonymous]
    public class ShowQueryStringModel : PageModel
    {
        public async Task<IActionResult> OnPostAsync()
        {
            string QueryString = "";
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                QueryString = await reader.ReadToEndAsync();
            }
            return Content(QueryString);
        }
    }
}