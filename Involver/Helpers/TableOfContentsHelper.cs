using HtmlAgilityPack;
using Involver.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Involver.Helpers
{
    public static class TableOfContentsHelper
    {
        public static List<TocItem> Generate(string htmlContent, out string processedHtml)
        {
            var toc = new List<TocItem>();
            if (string.IsNullOrEmpty(htmlContent))
            {
                processedHtml = htmlContent;
                return toc;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(htmlContent);

            var headings = doc.DocumentNode.SelectNodes("//h1|//h2|//h3|//h4|//h5|//h6");
            if (headings == null)
            {
                processedHtml = htmlContent;
                return toc;
            }

            var usedIds = new HashSet<string>();

            foreach (var heading in headings)
            {
                var text = heading.InnerText.Trim();
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                // Create a URL-friendly ID
                var id = text.ToLower();
                // Replace whitespace with a hyphen
                id = Regex.Replace(id, @"\s+", "-");
                // Remove any character that is not a letter, number, or hyphen.
                // \p{L} matches any unicode letter. \p{N} matches any unicode number.
                id = Regex.Replace(id, @"[^\p{L}\p{N}-]", "");
                id = id.Length > 50 ? id.Substring(0, 50) : id;

                // Ensure ID is unique
                var originalId = id;
                int counter = 1;
                while (usedIds.Contains(id))
                {
                    id = $"{originalId}-{counter++}";
                }
                usedIds.Add(id);

                // Update the heading
                heading.SetAttributeValue("id", id);
                var anchor = doc.CreateElement("a");
                anchor.SetAttributeValue("href", "#" + id);
                anchor.AddClass("anchor-link");
                anchor.InnerHtml = " #";
                heading.AppendChild(anchor);

                // Add to TOC
                toc.Add(new TocItem
                {
                    Id = id,
                    Text = text,
                    Level = int.Parse(heading.Name.Substring(1))
                });
            }

            processedHtml = doc.DocumentNode.OuterHtml;
            return toc;
        }
    }
}
