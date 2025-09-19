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

            sanitizer.RemovingTag += FilterTags;

            var sanitized = sanitizer.Sanitize(html);

            sanitized = ParseImage(sanitized);

            return sanitized;
        }

        private static void FilterTags(object sender, RemovingTagEventArgs e)
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