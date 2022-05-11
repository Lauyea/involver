using Involver.Helpers;
using NUnit.Framework;

namespace InvolverTest.Helpers
{
    [TestFixture]
    public class PageSizeHelperTests
    {


        [SetUp]
        public void SetUp()
        {

        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_1to1()
        {
            // Arrange
            int pageIndex = 1;
            int totalPages = 1;
            int startPage = 1;
            int endPage = 1;

            // Act
            PageSizeHelper.Get(
                pageIndex,
                totalPages,
                ref startPage,
                ref endPage);

            // Assert
            Assert.AreEqual(1, startPage);
            Assert.AreEqual(1, endPage);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_1to3()
        {
            // Arrange
            int pageIndex = 1;
            int totalPages = 3;
            int startPage = 1;
            int endPage = 1;

            // Act
            PageSizeHelper.Get(
                pageIndex,
                totalPages,
                ref startPage,
                ref endPage);

            // Assert
            Assert.AreEqual(1, startPage);
            Assert.AreEqual(3, endPage);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_1to5()
        {
            // Arrange
            int pageIndex = 1;
            int totalPages = 5;
            int startPage = 1;
            int endPage = 1;

            // Act
            PageSizeHelper.Get(
                pageIndex,
                totalPages,
                ref startPage,
                ref endPage);

            // Assert
            Assert.AreEqual(1, startPage);
            Assert.AreEqual(5, endPage);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_1to7()
        {
            // Arrange
            int pageIndex = 1;
            int totalPages = 7;
            int startPage = 1;
            int endPage = 1;

            // Act
            PageSizeHelper.Get(
                pageIndex,
                totalPages,
                ref startPage,
                ref endPage);

            // Assert
            Assert.AreEqual(1, startPage);
            Assert.AreEqual(5, endPage);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_3to3()
        {
            // Arrange
            int pageIndex = 3;
            int totalPages = 3;
            int startPage = 1;
            int endPage = 1;

            // Act
            PageSizeHelper.Get(
                pageIndex,
                totalPages,
                ref startPage,
                ref endPage);

            // Assert
            Assert.AreEqual(1, startPage);
            Assert.AreEqual(3, endPage);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_3to5()
        {
            // Arrange
            int pageIndex = 3;
            int totalPages = 5;
            int startPage = 1;
            int endPage = 1;

            // Act
            PageSizeHelper.Get(
                pageIndex,
                totalPages,
                ref startPage,
                ref endPage);

            // Assert
            Assert.AreEqual(1, startPage);
            Assert.AreEqual(5, endPage);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_3to7()
        {
            // Arrange
            int pageIndex = 3;
            int totalPages = 7;
            int startPage = 1;
            int endPage = 1;

            // Act
            PageSizeHelper.Get(
                pageIndex,
                totalPages,
                ref startPage,
                ref endPage);

            // Assert
            Assert.AreEqual(1, startPage);
            Assert.AreEqual(5, endPage);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_5to5()
        {
            // Arrange
            int pageIndex = 5;
            int totalPages = 5;
            int startPage = 1;
            int endPage = 1;

            // Act
            PageSizeHelper.Get(
                pageIndex,
                totalPages,
                ref startPage,
                ref endPage);

            // Assert
            Assert.AreEqual(3, startPage);
            Assert.AreEqual(5, endPage);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_5to7()
        {
            // Arrange
            int pageIndex = 5;
            int totalPages = 7;
            int startPage = 1;
            int endPage = 1;

            // Act
            PageSizeHelper.Get(
                pageIndex,
                totalPages,
                ref startPage,
                ref endPage);

            // Assert
            Assert.AreEqual(3, startPage);
            Assert.AreEqual(7, endPage);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_7to7()
        {
            // Arrange
            int pageIndex = 7;
            int totalPages = 7;
            int startPage = 1;
            int endPage = 1;

            // Act
            PageSizeHelper.Get(
                pageIndex,
                totalPages,
                ref startPage,
                ref endPage);

            // Assert
            Assert.AreEqual(5, startPage);
            Assert.AreEqual(7, endPage);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_2to4()
        {
            // Arrange
            int pageIndex = 2;
            int totalPages = 4;
            int startPage = 1;
            int endPage = 1;

            // Act
            PageSizeHelper.Get(
                pageIndex,
                totalPages,
                ref startPage,
                ref endPage);

            // Assert
            Assert.AreEqual(1, startPage);
            Assert.AreEqual(4, endPage);
        }
    }
}
