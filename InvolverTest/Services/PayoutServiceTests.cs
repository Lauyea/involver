using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.Dashboard;
using DataAccess.Models.StatisticalData;

using Involver.Services.Admin;

using InvolverTest.Infrastructure;

using Microsoft.EntityFrameworkCore;

using NSubstitute;

using NUnit.Framework;

namespace InvolverTest.Services
{
    /// <summary>
    /// Issue #237：分潤清單的待撥、已撥金額與標記撥款
    /// </summary>
    [TestFixture]
    public class PayoutServiceTests
    {
        private const string CreatorId = "creator";
        private const string OtherCreatorId = "other";
        private const string AdminId = "admin";

        private static readonly DateOnly September = new(2026, 9, 1);
        private static readonly DateOnly October = new(2026, 10, 1);

        private SqliteTestDatabase _database = null!;
        private IAdminAuditService _auditService = null!;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            using var context = _database.CreateContext();

            var seq = 1;
            foreach (var id in new[] { CreatorId, OtherCreatorId, AdminId })
            {
                context.Users.Add(new InvolverUser { Id = id, UserName = id });
                context.Profiles.Add(new Profile { ProfileID = id, UserName = id, SeqNo = seq++, MonthlyCoins = id == CreatorId ? 7 : 0 });
            }

            context.CoinMonthlyBalances.AddRange(
                Balance(CreatorId, September, 100),
                Balance(CreatorId, October, 60),
                Balance(OtherCreatorId, October, 25),
                // 其他幣別不算創作者收入
                new CoinMonthlyBalance { ProfileId = CreatorId, Month = October, CoinType = CoinType.Real, Credits = 999 });

            context.CreatorPayouts.Add(new CreatorPayout { ProfileId = CreatorId, Month = September, Amount = 100, PaidAt = new DateTime(2026, 10, 5) });
            context.SaveChanges();

            _auditService = Substitute.For<IAdminAuditService>();
            _auditService
                .AddAsync(Arg.Any<AdminAuditAction>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new AdminAuditLog { ActorProfileId = AdminId, TargetType = "User" });
        }

        [TearDown]
        public void TearDown()
        {
            _database?.Dispose();
        }

        private static CoinMonthlyBalance Balance(string profileId, DateOnly month, decimal credits) => new()
        {
            ProfileId = profileId,
            Month = month,
            CoinType = CoinType.CreatorIncome,
            Credits = credits,
        };

        [Test]
        public async Task GetSummariesAsync_WithPaidAndPendingMonths_SeparatesAmounts()
        {
            using var context = _database.CreateContext();
            var service = new PayoutService(context, _auditService);

            var summaries = await service.GetSummariesAsync();
            var creator = summaries.Single(s => s.ProfileId == CreatorId);
            var other = summaries.Single(s => s.ProfileId == OtherCreatorId);

            Assert.Multiple(() =>
            {
                Assert.That(creator.PendingMonths, Is.EqualTo(new[] { October }));
                Assert.That(creator.PendingAmount, Is.EqualTo(60m));
                Assert.That(creator.PaidAmount, Is.EqualTo(100m));
                Assert.That(creator.CurrentMonthIncome, Is.EqualTo(7m));
                Assert.That(other.PendingAmount, Is.EqualTo(25m));
                Assert.That(summaries.Any(s => s.ProfileId == AdminId), Is.False, "沒有收入的用戶不列出");
                Assert.That(summaries[0].ProfileId, Is.EqualTo(CreatorId), "待撥多的排前面");
            });
        }

        [Test]
        public async Task MarkPaidAsync_WithPendingMonths_WritesPayoutsAndAuditLog()
        {
            using (var context = _database.CreateContext())
            {
                var service = new PayoutService(context, _auditService);

                var result = await service.MarkPaidAsync(CreatorId, "轉帳 123");

                Assert.That(result, Is.EqualTo(new PayoutMarkResult(1, 60m)));
            }

            using var verify = _database.CreateContext();
            var payout = await verify.CreatorPayouts.SingleAsync(p => p.ProfileId == CreatorId && p.Month == October);

            Assert.Multiple(async () =>
            {
                Assert.That(payout.Amount, Is.EqualTo(60m));
                Assert.That(payout.PaidByProfileId, Is.EqualTo(AdminId));
                Assert.That(payout.Note, Is.EqualTo("轉帳 123"));
                await _auditService.Received(1).AddAsync(
                    AdminAuditAction.PayoutMarkedPaid, "User", CreatorId, Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            });
        }

        [Test]
        public async Task MarkPaidAsync_WhenAlreadyPaid_ReturnsZeroMonths()
        {
            using var context = _database.CreateContext();
            var service = new PayoutService(context, _auditService);
            await service.MarkPaidAsync(CreatorId, null);

            var second = await service.MarkPaidAsync(CreatorId, null);

            Assert.That(second.Months, Is.EqualTo(0));
            Assert.That(await context.CreatorPayouts.CountAsync(p => p.ProfileId == CreatorId), Is.EqualTo(2));
        }
    }
}
