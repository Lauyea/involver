using System;
using DataAccess.Models.StatisticalData;
using Involver.Services;
using NUnit.Framework;

namespace InvolverTest.Services
{
    [TestFixture]
    public class CoinTransactionInfoTests
    {
        [TestCase(PaymentProvider.ECPay, "IN20261111001", "ECPay:IN20261111001")]
        [TestCase(PaymentProvider.Stripe, "pi_3QxYz", "Stripe:pi_3QxYz")]
        [TestCase(PaymentProvider.PayPal, "8AB12345CD678901E", "PayPal:8AB12345CD678901E")]
        public void ForTopUp_WithValidPayment_BuildsProviderQualifiedReferenceAndKey(PaymentProvider provider, string paymentId, string expectedReference)
        {
            var info = CoinTransactionInfo.ForTopUp(provider, paymentId, "備註");

            Assert.That(info.Type, Is.EqualTo(CoinTransactionType.TopUp));
            Assert.That(info.ReferenceType, Is.EqualTo(CoinReferenceType.Payment));
            Assert.That(info.ReferenceId, Is.EqualTo(expectedReference));
            Assert.That(info.IdempotencyKey, Is.EqualTo($"TopUp:{expectedReference}"));
            Assert.That(info.Note, Is.EqualTo("備註"));
        }

        [Test]
        public void ForTopUp_WithLongestPaymentId_FitsInColumns()
        {
            var info = CoinTransactionInfo.ForTopUp(PaymentProvider.CreditCard, new string('x', CoinTransactionInfo.PaymentIdMaxLength));

            // CoinTransactions.ReferenceId 與 IdempotencyKey 的長度上限都是 300
            Assert.That(info.ReferenceId!.Length, Is.LessThanOrEqualTo(300));
            Assert.That(info.IdempotencyKey!.Length, Is.LessThanOrEqualTo(300));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void ForTopUp_WhenPaymentIdIsBlank_ThrowsArgumentException(string? paymentId)
        {
            Assert.Catch<ArgumentException>(() => CoinTransactionInfo.ForTopUp(PaymentProvider.ECPay, paymentId!));
        }

        [Test]
        public void ForTopUp_WhenPaymentIdTooLong_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                CoinTransactionInfo.ForTopUp(PaymentProvider.ECPay, new string('x', CoinTransactionInfo.PaymentIdMaxLength + 1)));
        }

        [Test]
        public void ForTopUp_WhenProviderUndefined_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => CoinTransactionInfo.ForTopUp((PaymentProvider)999, "T001"));
        }
    }
}
