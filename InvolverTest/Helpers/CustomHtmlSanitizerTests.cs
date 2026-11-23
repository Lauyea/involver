using Involver.Helpers;

using NUnit.Framework;

namespace InvolverTest.Helpers
{
    /// <summary>
    /// <see cref="CustomHtmlSanitizer"/> 的測試：確認允許的標籤與 YouTube iframe 會保留，
    /// 危險的標籤與屬性會被移除。升級 HtmlSanitizer 時用來確認消毒結果沒有改變。
    /// </summary>
    [TestFixture]
    public class CustomHtmlSanitizerTests
    {
        [TestCase(null)]
        [TestCase("")]
        public void SanitizeHtml_WhenInputIsNullOrEmpty_ReturnsInput(string? html)
        {
            var result = CustomHtmlSanitizer.SanitizeHtml(html!);

            Assert.That(result, Is.EqualTo(html));
        }

        [Test]
        public void SanitizeHtml_WithAllowedTags_KeepsTagsAndAttributes()
        {
            const string html = "<p class=\"lead\" id=\"intro\">Hello <strong>world</strong> "
                + "<a href=\"https://involver.tw/Novels\">link</a><br></p>";

            var result = CustomHtmlSanitizer.SanitizeHtml(html);

            Assert.That(result, Does.Contain("<p class=\"lead\" id=\"intro\">"));
            Assert.That(result, Does.Contain("<strong>world</strong>"));
            Assert.That(result, Does.Contain("<a href=\"https://involver.tw/Novels\">link</a>"));
            Assert.That(result, Does.Contain("<br>"));
        }

        [Test]
        public void SanitizeHtml_WithYouTubeIframe_KeepsIframe()
        {
            const string html = "<iframe src=\"https://www.youtube.com/embed/dQw4w9WgXcQ\" width=\"560\" height=\"315\"></iframe>";

            var result = CustomHtmlSanitizer.SanitizeHtml(html);

            Assert.That(result, Does.Contain("<iframe"));
            Assert.That(result, Does.Contain("src=\"https://www.youtube.com/embed/dQw4w9WgXcQ\""));
        }

        [Test]
        public void SanitizeHtml_WithNonYouTubeIframe_RemovesIframe()
        {
            const string html = "<p>text</p><iframe src=\"https://evil.example.com/embed\"></iframe>";

            var result = CustomHtmlSanitizer.SanitizeHtml(html);

            Assert.That(result, Does.Not.Contain("<iframe"));
            Assert.That(result, Does.Contain("<p>text</p>"));
        }

        [Test]
        public void SanitizeHtml_WithScriptTag_RemovesScript()
        {
            const string html = "<p>safe</p><script>alert('xss')</script>";

            var result = CustomHtmlSanitizer.SanitizeHtml(html);

            Assert.That(result, Does.Not.Contain("<script"));
            Assert.That(result, Does.Not.Contain("alert"));
            Assert.That(result, Does.Contain("<p>safe</p>"));
        }

        [Test]
        public void SanitizeHtml_WithEventHandlerAttribute_RemovesAttribute()
        {
            const string html = "<a href=\"https://involver.tw\" onclick=\"alert('xss')\">link</a>"
                + "<img src=\"https://involver.tw/a.png\" onerror=\"alert('xss')\">";

            var result = CustomHtmlSanitizer.SanitizeHtml(html);

            Assert.That(result, Does.Not.Contain("onclick"));
            Assert.That(result, Does.Not.Contain("onerror"));
            Assert.That(result, Does.Contain("<a href=\"https://involver.tw\">link</a>"));
        }

        [Test]
        public void SanitizeHtml_WithJavaScriptUrl_RemovesHref()
        {
            const string html = "<a href=\"javascript:alert('xss')\">link</a>";

            var result = CustomHtmlSanitizer.SanitizeHtml(html);

            Assert.That(result, Does.Not.Contain("javascript:"));
        }

        [Test]
        public void SanitizeHtml_WithImage_AddsImgFluidClass()
        {
            const string html = "<img src=\"https://involver.tw/a.png\" alt=\"cover\">";

            var result = CustomHtmlSanitizer.SanitizeHtml(html);

            Assert.That(result, Does.Contain("src=\"https://involver.tw/a.png\""));
            Assert.That(result, Does.Contain("class=\"img-fluid\""));
        }

        [Test]
        public void SanitizeHtml_WithNamedImage_RemovesNameAttribute()
        {
            // HtmlSanitizer 11 起預設移除 img、iframe 等元素的 name，避免 DOM clobbering（例如蓋掉 document.getElementById）
            const string html = "<img name=\"getElementById\" src=\"https://involver.tw/a.png\">";

            var result = CustomHtmlSanitizer.SanitizeHtml(html);

            Assert.That(result, Does.Not.Contain("name="));
            Assert.That(result, Does.Contain("src=\"https://involver.tw/a.png\""));
        }

        [Test]
        public void SanitizeHtml_WithContentEditable_RemovesAttribute()
        {
            // HtmlSanitizer 11 起預設不允許 contenteditable
            const string html = "<div contenteditable=\"true\">text</div>";

            var result = CustomHtmlSanitizer.SanitizeHtml(html);

            Assert.That(result, Does.Not.Contain("contenteditable"));
            Assert.That(result, Does.Contain("<div>text</div>"));
        }
    }
}
