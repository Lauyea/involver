using Involver.Helpers;
using NSubstitute;
using NUnit.Framework;
using System;

namespace InvolverTest.Helpers
{
    [TestFixture]
    public class DiceHelperTests
    {


        [SetUp]
        public void SetUp()
        {

        }

        [Test]
        public void ReplaceRollDiceString_StateUnderTest_ExpectedBehavior()
        {
            // Arrange
            string strToDice = "Dice05D10";

            // Act
            var result = DiceHelper.ReplaceRollDiceString(
                ref strToDice);

            // Assert
            Assert.That(result == 1);
            Assert.That("5D10: " == strToDice[..6]);
        }

        [Test]
        public void ReplaceRollDiceString_StateUnderTest_ExpectedBehavior2()
        {
            // Arrange
            string strToDice = "Dice10D10";

            // Act
            var result = DiceHelper.ReplaceRollDiceString(
                ref strToDice);

            // Assert
            Assert.That(result == 1);
            Assert.That("10D10: " == strToDice[..7]);
        }

        [Test]
        public void ReplaceRollDiceString_StateUnderTest_ExpectedBehavior_LongString()
        {
            // Arrange
            string strToDice = "<p>Dice05D10</p><p>Dice05D10</p><p>Dice05D10</p><p>Dice05D10</p><p>Dice05D10</p>";

            // Act
            var result = DiceHelper.ReplaceRollDiceString(
                ref strToDice);

            // Assert
            Assert.That(result == 5);
            Assert.That("<p>5D10: " == strToDice[..9]);
        }

        [Test]
        public void ReplaceRollDiceString_StateUnderTest_ExpectedBehavior_TestEven()
        {
            // Arrange
            int count1 = 0;
            int count2 = 0;
            int count3 = 0;

            // Act
            for(int i = 0; i < 100; i++)
            {
                string strToDice = "Dice01D03";// EX: "1D3: 2"

                var result = DiceHelper.ReplaceRollDiceString(
                    ref strToDice);

                var lastNumber = strToDice[6];

                if (lastNumber == '1')
                {
                    count1++;
                }
                else if(lastNumber == '2')
                {
                    count2++;
                }
                else
                {
                    count3++;
                }
            }

            // Assert
            Assert.That(count1 > 0);
            Assert.That(count2 > 0);
            Assert.That(count3 > 0);
        }
    }
}
