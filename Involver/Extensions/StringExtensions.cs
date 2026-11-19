using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using HtmlAgilityPack;

namespace Involver.Extensions
{
    public static class StringExtensions
    {
        public static string ToMd5(this string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            var loweredBytes = Encoding.Default.GetBytes(s.ToLower());

            using var md5 = MD5.Create();

            var buffer = md5.ComputeHash(loweredBytes);

            var sb = new StringBuilder(buffer.Length * 2);

            for (var i = 0; i < buffer.Length; i++)
            {
                sb.Append(buffer[i].ToString("X2"));
            }

            return sb.ToString().ToLower();
        }

        public static string StripHTML(this string s)
        {
            return Regex.Replace(s, "<.*?>", String.Empty);
        }

        /// <summary>
        /// 將 HTML 內容轉成純文字預覽：先解析 HTML 取出文字、解碼實體並合併空白，再依長度截斷。
        /// 先去除標籤再截字，避免截在標籤中間造成破損的 HTML。
        /// </summary>
        /// <param name="html">原始 HTML 內容</param>
        /// <param name="maxLength">預覽最多保留的字元數</param>
        /// <returns>純文字預覽；超過長度時結尾加上「...」</returns>
        public static string ToPlainTextPreview(this string html, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(html))
            {
                return string.Empty;
            }

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // script、style 的內容不是給讀者看的文字
            foreach (var node in doc.DocumentNode.SelectNodes("//script|//style") ?? Enumerable.Empty<HtmlNode>())
            {
                node.Remove();
            }

            // 區塊與換行元素前後補空白，避免不同段落的字黏在一起
            var sb = new StringBuilder();
            AppendText(doc.DocumentNode, sb);

            var text = Regex.Replace(HtmlEntity.DeEntitize(sb.ToString()), @"\s+", " ").Trim();

            if (text.Length <= maxLength)
            {
                return text;
            }

            // 避免把 surrogate pair（如 emoji）切成一半
            var cut = maxLength;
            if (cut > 0 && char.IsHighSurrogate(text[cut - 1]))
            {
                cut--;
            }

            return text[..cut] + "...";
        }

        private static readonly HashSet<string> _blockTags =
        [
            "p", "div", "br", "li", "ul", "ol", "h1", "h2", "h3", "h4", "h5", "h6",
            "blockquote", "pre", "tr", "td", "th", "table", "figure", "figcaption", "hr", "section", "article"
        ];

        private static void AppendText(HtmlNode node, StringBuilder sb)
        {
            if (node.NodeType == HtmlNodeType.Text)
            {
                sb.Append(((HtmlTextNode)node).Text);
                return;
            }

            if (node.NodeType == HtmlNodeType.Comment)
            {
                return;
            }

            var isBlock = _blockTags.Contains(node.Name);
            if (isBlock)
            {
                sb.Append(' ');
            }

            foreach (var child in node.ChildNodes)
            {
                AppendText(child, sb);
            }

            if (isBlock)
            {
                sb.Append(' ');
            }
        }
    }
}