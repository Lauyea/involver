using System;

using Involver.Helpers;

using NUnit.Framework;

namespace InvolverTest.Helpers
{
    [TestFixture]
    public class TimePeriodHelperTests
    {


        [SetUp]
        public void SetUp()
        {

        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_MonthAgo()
        {
            // Arrange
            DateTime time = DateTime.Now.AddMonths(-1);

            // Act
            var result = TimePeriodHelper.Get(
                time);

            // Assert
            Assert.That("1個月前" == result);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_12MonthAgo()
        {
            // Arrange
            DateTime time = DateTime.Now.AddMonths(-12);

            // Act
            var result = TimePeriodHelper.Get(
                time);

            // Assert
            Assert.That("1年前" == result);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_YearAgo()
        {
            // Arrange
            DateTime time = DateTime.Now.AddYears(-1);

            // Act
            var result = TimePeriodHelper.Get(
                time);

            // Assert
            Assert.That("1年前" == result);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_3YearAgo()
        {
            // Arrange
            DateTime time = DateTime.Now.AddYears(-3);

            // Act
            var result = TimePeriodHelper.Get(
                time);

            // Assert
            Assert.That("3年前" == result);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_MinuteAgo()
        {
            // Arrange
            DateTime time = DateTime.Now.AddMinutes(-1);

            // Act
            var result = TimePeriodHelper.Get(
                time);

            // Assert
            Assert.That("1分鐘前" == result);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_60MinuteAgo()
        {
            // Arrange
            DateTime time = DateTime.Now.AddMinutes(-60);

            // Act
            var result = TimePeriodHelper.Get(
                time);

            // Assert
            Assert.That("1小時前" == result);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_HourAgo()
        {
            // Arrange
            DateTime time = DateTime.Now.AddHours(-1);

            // Act
            var result = TimePeriodHelper.Get(
                time);

            // Assert
            Assert.That("1小時前" == result);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_24HourAgo()
        {
            // Arrange
            DateTime time = DateTime.Now.AddHours(-24);

            // Act
            var result = TimePeriodHelper.Get(
                time);

            // Assert
            Assert.That("1天前" == result);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_DayAgo()
        {
            // Arrange
            DateTime time = DateTime.Now.AddDays(-1);

            // Act
            var result = TimePeriodHelper.Get(
                time);

            // Assert
            Assert.That("1天前" == result);
        }

        [Test]
        public void Get_StateUnderTest_ExpectedBehavior_30DayAgo()
        {
            // Arrange
            DateTime time = DateTime.Now.AddDays(-30);

            // Act
            var result = TimePeriodHelper.Get(
                time);

            // Assert
            Assert.That("1個月前" == result);
        }


    }
}