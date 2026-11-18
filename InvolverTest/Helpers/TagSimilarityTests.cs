using Involver.Helpers;

using NUnit.Framework;

namespace InvolverTest.Helpers
{
    /// <summary>
    /// Issue #234：標籤相似度判斷（輸入都是統一格式後的名稱）
    /// </summary>
    [TestFixture]
    public class TagSimilarityTests
    {
        [TestCase("讀書心得01", "讀書心得")]
        [TestCase("讀書心得1", "讀書心得")]
        [TestCase("讀書心得", "讀書心得")]
        [TestCase("fate2", "fate")]
        [TestCase("c++", "c")]
        [TestCase("2024", "")]
        [TestCase("", "")]
        public void StripTrailingNumber_WithVariousInputs_RemovesTrailingDigitsAndSymbols(string input, string expected)
        {
            Assert.That(TagSimilarity.StripTrailingNumber(input), Is.EqualTo(expected));
        }

        [TestCase("讀書心得1", "讀書心得")]
        [TestCase("讀書心得2", "讀書心得1")]
        [TestCase("fate2", "fate")]
        public void Compare_WhenOnlyTrailingNumberDiffers_ReturnsDuplicate(string newName, string existingName)
        {
            var (kind, _) = TagSimilarity.Compare(newName, existingName);

            Assert.That(kind, Is.EqualTo(TagMatchKind.Duplicate));
        }

        [Test]
        public void Compare_WhenSameName_ReturnsSame()
        {
            var (kind, score) = TagSimilarity.Compare("讀書心得", "讀書心得");

            Assert.Multiple(() =>
            {
                Assert.That(kind, Is.EqualTo(TagMatchKind.Same));
                Assert.That(score, Is.EqualTo(1));
            });
        }

        [Test]
        public void Compare_WhenNewNameIsBaseOfNumberedTag_ReturnsSimilar()
        {
            // 「讀書心得」本身是乾淨的名稱，不擋，但提示現有的「讀書心得1」
            var (kind, _) = TagSimilarity.Compare("讀書心得", "讀書心得1");

            Assert.That(kind, Is.EqualTo(TagMatchKind.Similar));
        }

        [TestCase("讀書心德", "讀書心得")]
        [TestCase("讀書心得分享", "讀書心得")]
        [TestCase("崩壞星穹鐵道", "崩壞星穹鐵道2")]
        public void Compare_WhenNamesAreClose_ReturnsSimilar(string newName, string existingName)
        {
            var (kind, _) = TagSimilarity.Compare(newName, existingName);

            Assert.That(kind, Is.AnyOf(TagMatchKind.Similar, TagMatchKind.Duplicate));
        }

        [TestCase("奇幻", "科幻")]
        [TestCase("讀書會", "讀書心得")]
        [TestCase("愛情", "親情")]
        [TestCase("2023", "2024")]
        [TestCase("fate", "fgo")]
        public void Compare_WhenNamesAreDifferent_ReturnsNone(string newName, string existingName)
        {
            var (kind, _) = TagSimilarity.Compare(newName, existingName);

            Assert.That(kind, Is.EqualTo(TagMatchKind.None));
        }

        [TestCase("abc", "abc", 1.0)]
        [TestCase("abc", "abd", 1.0 / 3)]
        [TestCase("ab", "cd", 0.0)]
        [TestCase("a", "a", 1.0)]
        public void BigramJaccard_WithVariousInputs_ReturnsOverlapRatio(string a, string b, double expected)
        {
            Assert.That(TagSimilarity.BigramJaccard(a, b), Is.EqualTo(expected).Within(0.0001));
        }

        [TestCase("kitten", "sitting", 3)]
        [TestCase("", "abc", 3)]
        [TestCase("讀書心得", "讀書心德", 1)]
        [TestCase("same", "same", 0)]
        public void LevenshteinDistance_WithVariousInputs_ReturnsEditCount(string a, string b, int expected)
        {
            Assert.That(TagSimilarity.LevenshteinDistance(a, b), Is.EqualTo(expected));
        }
    }
}
