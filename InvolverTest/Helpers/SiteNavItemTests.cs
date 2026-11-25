using System.Linq;

using Involver.Common;
using Involver.Models.ViewModels.Navigation;

using NUnit.Framework;

namespace InvolverTest.Helpers
{
    /// <summary>
    /// 頁首與底部導覽列標示目前所在入口的規則
    /// </summary>
    [TestFixture]
    public class SiteNavItemTests
    {
        private static SiteNavItem Item(string label) => SiteNavigation.PrimaryItems.Single(i => i.Label == label);

        [TestCase("/")]
        [TestCase("")]
        [TestCase("/Index")]
        [TestCase("/Feed/TrendingCreations")]
        public void IsActive_HomePaths_ReturnsTrueForHome(string path)
        {
            Assert.That(Item("首頁").IsActive(path), Is.True);
        }

        [TestCase("/Novels/Details/3")]
        [TestCase("/Functions/CreateCover")]
        [TestCase("/Feed/Notifications")]
        public void IsActive_OtherPaths_ReturnsFalseForHome(string path)
        {
            Assert.That(Item("首頁").IsActive(path), Is.False);
        }

        [TestCase("/Novels", true)]
        [TestCase("/novels/index", true)]
        [TestCase("/Episodes/Details", true)]
        [TestCase("/NovelsArchive", false)]
        [TestCase("/Articles/Index", false)]
        public void IsActive_CreationPaths_MatchesWholeSegment(string path, bool expected)
        {
            Assert.That(Item("創作").IsActive(path), Is.EqualTo(expected));
        }

        [Test]
        public void Tools_EveryToolIsUnderToolsEntry()
        {
            var tools = Item("工具");

            Assert.That(SiteNavigation.Tools.Select(t => t.PagePath), Is.All.Matches<string>(tools.IsActive));
        }
    }
}
