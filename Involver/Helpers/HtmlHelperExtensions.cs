using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Involver.Helpers
{
    /// <summary>
    /// HtmlHelper 的擴充
    /// </summary>
    public static class HtmlHelperExtensions
    {
        /// <summary>
        /// 內嵌 HTML 時的 XSS 防護
        /// </summary>
        /// <param name="helper"></param>
        /// <param name="html"></param>
        /// <returns></returns>
        public static IHtmlContent AntiXssRaw(this IHtmlHelper helper, string html)
        {
            var sanitized = CustomHtmlSanitizer.SanitizeHtml(html);
            return helper.Raw(sanitized);
        }
    }
}