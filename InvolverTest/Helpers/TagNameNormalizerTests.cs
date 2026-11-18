using Involver.Helpers;

using NUnit.Framework;

namespace InvolverTest.Helpers
{
    /// <summary>
    /// Issue #234：標籤名稱的格式統一
    /// </summary>
    [TestFixture]
    public class TagNameNormalizerTests
    {
        [TestCase("奇幻", "奇幻")]
        [TestCase("  奇幻  ", "奇幻")]
        [TestCase("奇 幻", "奇幻")]
        [TestCase("奇　幻", "奇幻")]
        [TestCase("Fate", "fate")]
        [TestCase("ＦＡＴＥ", "fate")]
        [TestCase("Ｆａｔｅ２", "fate2")]
        [TestCase("Type-Moon", "typemoon")]
        [TestCase("Type Moon", "typemoon")]
        [TestCase("崩壞：星穹鐵道", "崩壞星穹鐵道")]
        [TestCase("「讀書心得」", "讀書心得")]
        [TestCase("讀書心得！", "讀書心得")]
        [TestCase("臺灣", "台灣")]
        [TestCase("台灣", "台灣")]
        [TestCase("C++", "c++")]
        [TestCase("零​寬", "零寬")]
        public void Normalize_WithVariousInputs_ReturnsUnifiedName(string input, string expected)
        {
            Assert.That(TagNameNormalizer.Normalize(input), Is.EqualTo(expected));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("!!!")]
        [TestCase("、。")]
        public void Normalize_WhenOnlyWhitespaceOrPunctuation_ReturnsEmpty(string? input)
        {
            Assert.That(TagNameNormalizer.Normalize(input!), Is.Empty);
        }

        [Test]
        public void Normalize_WhenFullWidthAndHalfWidth_ReturnsSameValue()
        {
            Assert.That(TagNameNormalizer.Normalize("ＡＩ　創作"), Is.EqualTo(TagNameNormalizer.Normalize("ai創作")));
        }

        [TestCase("  奇幻  ", "奇幻")]
        [TestCase("Type   Moon", "Type Moon")]
        [TestCase("ＦＡＴＥ", "ＦＡＴＥ")]
        [TestCase("奇\t幻", "奇 幻")]
        public void CleanDisplayName_WithVariousInputs_KeepsOriginalFormExceptSpaces(string input, string expected)
        {
            Assert.That(TagNameNormalizer.CleanDisplayName(input), Is.EqualTo(expected));
        }
    }
}
