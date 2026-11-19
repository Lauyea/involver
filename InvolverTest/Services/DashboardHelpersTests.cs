using System;
using System.Collections.Generic;
using System.Linq;

using DataAccess.Models.Dashboard;
using DataAccess.Models.StatisticalData;

using Involver.Pages.Creator;
using Involver.Services.Admin;

using NUnit.Framework;

namespace InvolverTest.Services
{
    /// <summary>
    /// Issue #237：儀表板的期間、數列與 KPI 目標計算
    /// </summary>
    [TestFixture]
    public class DashboardHelpersTests
    {
        [TestCase(7, ExpectedResult = 7)]
        [TestCase(90, ExpectedResult = 90)]
        [TestCase(0, ExpectedResult = 30)]
        [TestCase(365, ExpectedResult = 30)]
        public int Normalize_WithDays_ReturnsSupportedOption(int days)
        {
            return DashboardPeriod.Normalize(days);
        }

        [Test]
        public void Values_WithMissingDays_FillsZero()
        {
            var dates = DashboardSeries.Dates(new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 3));
            var stats = new[] { new DailySiteStat { Date = new DateOnly(2026, 11, 2), Views = 12 } };

            var values = DashboardSeries.Values(stats, dates, s => s.Views);

            Assert.That(values, Is.EqualTo(new decimal[] { 0, 12, 0 }));
        }

        [Test]
        public void Deltas_WithConsecutiveTotals_ReturnsDailyIncrease()
        {
            var dates = DashboardSeries.Dates(new DateOnly(2026, 11, 2), new DateOnly(2026, 11, 4));
            var stats = new[]
            {
                new DailySiteStat { Date = new DateOnly(2026, 11, 1), Episodes = 10 },
                new DailySiteStat { Date = new DateOnly(2026, 11, 2), Episodes = 13 },
                new DailySiteStat { Date = new DateOnly(2026, 11, 3), Episodes = 12 },
            };

            var values = DashboardSeries.Deltas(stats, dates, s => s.Episodes);

            // 11/3 被刪除而減少，以 0 表示；11/4 沒有快照，無法計算
            Assert.That(values, Is.EqualTo(new decimal[] { 3, 0, 0 }));
        }

        [TestCase(KpiPeriod.Month, "2026-11-19", "2026-11-01", "2026-12-01", "2026/11")]
        [TestCase(KpiPeriod.Quarter, "2026-11-19", "2026-10-01", "2027-01-01", "2026 Q4")]
        [TestCase(KpiPeriod.Quarter, "2026-02-28", "2026-01-01", "2026-04-01", "2026 Q1")]
        [TestCase(KpiPeriod.Year, "2026-11-19", "2026-01-01", "2027-01-01", "2026 年")]
        public void GetStart_WithDate_ReturnsPeriodRangeAndLabel(KpiPeriod period, string date, string start, string end, string label)
        {
            var periodStart = KpiTargetPeriods.GetStart(period, DateOnly.Parse(date));

            Assert.Multiple(() =>
            {
                Assert.That(periodStart, Is.EqualTo(DateOnly.Parse(start)));
                Assert.That(KpiTargetPeriods.GetEnd(period, periodStart), Is.EqualTo(DateOnly.Parse(end)));
                Assert.That(KpiTargetPeriods.GetLabel(period, periodStart), Is.EqualTo(label));
            });
        }

        [Test]
        public void SelectCurrent_WithOverlappingTargets_PrefersShortestCurrentPeriod()
        {
            var today = new DateOnly(2026, 11, 19);
            var targets = new List<KpiTarget>
            {
                new() { Metric = KpiMetric.MonthlyActiveUsers, Period = KpiPeriod.Year, PeriodStart = new DateOnly(2026, 1, 1), TargetValue = 2000 },
                new() { Metric = KpiMetric.MonthlyActiveUsers, Period = KpiPeriod.Quarter, PeriodStart = new DateOnly(2026, 10, 1), TargetValue = 500 },
                new() { Metric = KpiMetric.WeeklyVoters, Period = KpiPeriod.Month, PeriodStart = new DateOnly(2026, 10, 1), TargetValue = 50 },
            };

            var current = KpiTargetPeriods.SelectCurrent(targets, today);

            Assert.That(current[KpiMetric.MonthlyActiveUsers].TargetValue, Is.EqualTo(500));
            Assert.That(current.ContainsKey(KpiMetric.WeeklyVoters), Is.False, "十月的目標已經結束");
        }

        [Test]
        public void ToProgress_WithTarget_ReturnsPercent()
        {
            var target = new KpiTarget { Metric = KpiMetric.MonthlyActiveUsers, Period = KpiPeriod.Quarter, PeriodStart = new DateOnly(2026, 10, 1), TargetValue = 500 };

            var progress = KpiTargetPeriods.ToProgress(target, 320);

            Assert.Multiple(() =>
            {
                Assert.That(progress.Percent, Is.EqualTo(64));
                Assert.That(progress.PeriodLabel, Is.EqualTo("2026 Q4"));
                Assert.That(progress.TargetValue, Is.EqualTo(500));
            });
        }

        [Test]
        public void ToProgress_WithoutTarget_ReturnsNull()
        {
            Assert.That(KpiTargetPeriods.ToProgress(null, 320), Is.Null);
        }

        [TestCase(CoinTransactionType.Vote, ExpectedResult = "投票")]
        [TestCase(CoinTransactionType.InvolveNovel, ExpectedResult = "直接贊助作品")]
        [TestCase(CoinTransactionType.InvolveArticle, ExpectedResult = "直接贊助作品")]
        [TestCase(CoinTransactionType.InvolveCreator, ExpectedResult = "直接贊助創作者")]
        [TestCase(CoinTransactionType.MonthlyInvolveCreator, ExpectedResult = "每月贊助")]
        [TestCase(null, ExpectedResult = "其他")]
        public string Classify_WithSpendType_ReturnsIncomeSource(CoinTransactionType? spendType)
        {
            var source = CreatorIncomeSource.Classify(spendType);

            Assert.That(CreatorIncomeSource.Names, Does.Contain(source));
            return source;
        }
    }
}
