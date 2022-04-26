using Ganss.XSS;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Involver.Helpers
{
    public static class HtmlHelperExtensions
    {
        public static string AntiXssRaw(this IHtmlHelper helper, string html) 
        {
            var sanitizer = new HtmlSanitizer();

            var sanitized = sanitizer.Sanitize(html);

            return sanitized;
        }
    }
}
