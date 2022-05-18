using Ganss.XSS;
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
            var sanitizer = new HtmlSanitizer();

            sanitizer.AllowedAttributes.Add("class");

            sanitizer.RemovingTag += FilterTags;

            var sanitized = sanitizer.Sanitize(html);

            return helper.Raw(sanitized);
        }

        static void FilterTags(object sender, RemovingTagEventArgs e)
        {
            List<string> IframeWhitelistBase = new List<string>();

            IframeWhitelistBase.Add("www.youtube.com");

            IframeWhitelistBase.ForEach((baseUrl) =>
            {
                if (e.Tag.TagName.ToLower().Equals("iframe") 
                && e.Tag.GetAttribute("src").StartsWith(@"https://" + baseUrl))
                {
                    e.Cancel = true;
                }
            });
        }

        
    }
}
