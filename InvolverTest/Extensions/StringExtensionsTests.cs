using System;

using Involver.Extensions;

using NSubstitute;

using NUnit.Framework;

namespace InvolverTest.Extensions
{
    [TestFixture]
    public class StringExtensionsTests
    {


        [SetUp]
        public void SetUp()
        {

        }

        [Test]
        public void ToMd5_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            string s = "lauyea@gmail.com";

            // Act
            var result = StringExtensions.ToMd5(
                s);

            // Assert
            Assert.That("aaa0890957be5148be6b9066461cf9b4" == result);
        }

        [Test]
        public void ToMd5_StateUnderTest_ExpectedBehavior2()
        {
            // Arrange
            string s = "yanrei@gmail.com";

            // Act
            var result = StringExtensions.ToMd5(
                s);

            // Assert
            Assert.That("312112d06c5f78cb50ca55a1bbe5bb60" == result);
        }

        [TestCase("<p>一二三四五六七八九十</p>", 20, "一二三四五六七八九十")]
        [TestCase("<p><strong>粗體</strong>文字很長很長很長很長很長很長很長很長</p>", 6, "粗體文字很長...")]
        [TestCase("<p>第一段</p><p>第二段</p>", 20, "第一段 第二段")]
        [TestCase("<p>A &amp; B&nbsp;C</p>", 20, "A & B C")]
        [TestCase("<figure class=\"image\"><img src=\"x.png\"></figure><p>內文</p>", 20, "內文")]
        [TestCase("<style>.a{}</style><script>alert(1)</script><p>安全</p>", 20, "安全")]
        [TestCase(null, 20, "")]
        [TestCase("", 20, "")]
        public void ToPlainTextPreview_WithHtml_ReturnsTruncatedPlainText(string html, int maxLength, string expected)
        {
            var result = html.ToPlainTextPreview(maxLength);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void ToPlainTextPreview_WhenCutInsideLongTag_DoesNotLeakMarkup()
        {
            var html = "<p><a href=\"https://example.com/a/very/long/url\">連結</a>之後的文字</p>";

            var result = html.ToPlainTextPreview(4);

            Assert.That(result, Is.EqualTo("連結之後..."));
            Assert.That(result, Does.Not.Contain("<"));
        }

        [Test]
        public void ToPlainTextPreview_WhenCutInsideSurrogatePair_DoesNotSplitIt()
        {
            var html = "<p>ab😀cd</p>";

            var result = html.ToPlainTextPreview(3);

            Assert.That(result, Is.EqualTo("ab..."));
        }
}
}