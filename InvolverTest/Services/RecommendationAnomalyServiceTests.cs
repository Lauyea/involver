using System;
using System.Linq;

using DataAccess.Common;
using DataAccess.Services.Recommendations;

using Involver.Services.Recommendations;

using NUnit.Framework;

namespace InvolverTest.Services
{
    /// <summary>
    /// Issue #238：異常警示與投票剩餘時間
    /// </summary>
    [TestFixture]
    public class RecommendationAnomalyServiceTests
    {
        [Test]
        public void FindSpikes_WhenReadersJump_ReturnsContent()
        {
            var settings = new RecommendationSettings { SpikeWindowHours = 24, SpikeMultiplier = 5, SpikeMinReaders = 20 };

            // 過去 7 天共 14 人（平均每天 2 人），最近 24 小時 30 人
            var spikes = RecommendationAnomalyService.FindSpikes(
                [new ReaderSample(ContentKey.Novel(1), 30, 14)],
                settings);

            Assert.That(spikes.Select(s => s.Content), Is.EqualTo(new[] { ContentKey.Novel(1) }));
            Assert.That(spikes[0].BaselineAverage, Is.EqualTo(2).Within(1e-9));
        }

        [Test]
        public void FindSpikes_WhenBelowMinimumOrSteady_ReturnsEmpty()
        {
            var settings = new RecommendationSettings { SpikeWindowHours = 24, SpikeMultiplier = 5, SpikeMinReaders = 20 };

            var spikes = RecommendationAnomalyService.FindSpikes(
                [
                    new ReaderSample(ContentKey.Novel(1), 10, 0),   // 人數不夠
                    new ReaderSample(ContentKey.Novel(2), 40, 210), // 平常每天 30 人
                ],
                settings);

            Assert.That(spikes, Is.Empty);
        }

        [Test]
        public void FormatRemaining_WithTimeLimit_ReturnsHoursOrDays()
        {
            var now = new DateTime(2026, 11, 19, 12, 0, 0);

            Assert.That(HomeFeedService.FormatRemaining(LimitType.Time, now.AddMinutes(30), 0, null, 0, null, now), Is.EqualTo("剩 30 分鐘"));
            Assert.That(HomeFeedService.FormatRemaining(LimitType.Time, now.AddHours(5).AddMinutes(10), 0, null, 0, null, now), Is.EqualTo("剩 6 小時"));
            Assert.That(HomeFeedService.FormatRemaining(LimitType.Time, now.AddDays(2), 0, null, 0, null, now), Is.EqualTo("剩 2 天"));
        }

        [Test]
        public void FormatRemaining_WithNumberLimit_ReturnsProgress()
        {
            var now = new DateTime(2026, 11, 19, 12, 0, 0);

            Assert.That(HomeFeedService.FormatRemaining(LimitType.Number, null, 12, 50, 0, null, now), Is.EqualTo("12 / 50 人"));
        }
    }
}
