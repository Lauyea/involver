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
    }
}