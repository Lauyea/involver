using Ganss.Xss;

using HtmlAgilityPack;

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

            sanitized = ParseImage(sanitized);

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

        /// <summary>
        /// 把html中的img class加上img-fluid
        /// </summary>
        /// <param name="pHtml">html</param>
        /// <returns></returns>
        static string ParseImage(string pHtml)
        {
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(pHtml);
            var imgs = doc.DocumentNode.SelectNodes("//img");

            if (imgs != null)
            {
                foreach (var item in imgs)
                {
                    item.SetAttributeValue("class", "img-fluid");
                }
                using (StringWriter tw = new StringWriter())
                {
                    doc.Save(tw);
                    return tw.ToString();
                }
            }
            else
            {
                return pHtml;
            }
        }
    }
}