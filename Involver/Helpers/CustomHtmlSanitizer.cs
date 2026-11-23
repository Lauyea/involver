using Ganss.Xss;

using HtmlAgilityPack;

namespace Involver.Helpers
{
    public static class CustomHtmlSanitizer
    {
        public static string SanitizeHtml(string html)
        {
            if (string.IsNullOrEmpty(html))
            {
                return html;
            }

            var sanitizer = new HtmlSanitizer();

            sanitizer.AllowedAttributes.Add("class");
            sanitizer.AllowedAttributes.Add("id");

            sanitizer.RemovingTag += FilterTags;

            var sanitized = sanitizer.Sanitize(html);

            sanitized = ParseImage(sanitized);

            return sanitized;
        }

        /// <summary>
        /// 允許嵌入的 iframe 主機（必須完全相同，不比對前綴）
        /// </summary>
        private static readonly HashSet<string> IframeAllowedHosts = new(StringComparer.OrdinalIgnoreCase)
        {
            "www.youtube.com",
        };

        /// <summary>
        /// 保留指向允許主機的 https iframe，其餘照預設移除。
        /// </summary>
        /// <param name="sender">HtmlSanitizer</param>
        /// <param name="e">要移除的標籤</param>
        private static void FilterTags(object sender, RemovingTagEventArgs e)
        {
            if (e.Tag.TagName.Equals("iframe", StringComparison.OrdinalIgnoreCase)
                && IsAllowedIframeSource(e.Tag.GetAttribute("src")))
            {
                e.Cancel = true;
            }
        }

        /// <summary>
        /// 判斷 iframe 的 src 是否為允許的來源：要能解析成絕對網址、使用 https，而且主機完全符合白名單。
        /// 用字串前綴比對會讓 https://www.youtube.com.evil.example 之類的網址通過。
        /// </summary>
        /// <param name="src">iframe 的 src 屬性，可能為 null</param>
        /// <returns>允許時為 true</returns>
        private static bool IsAllowedIframeSource(string src)
        {
            return Uri.TryCreate(src, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && IframeAllowedHosts.Contains(uri.Host);
        }

        private static string ParseImage(string pHtml)
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