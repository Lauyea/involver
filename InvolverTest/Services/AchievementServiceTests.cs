using System.Linq;
using System.Threading.Tasks;
using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.AchievementModel;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.StatisticalData;
using Involver.Common;
using Involver.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InvolverTest.Services
{
    [TestFixture]
    public class AchievementServiceTests
    {
        private const string UserId = "user";

        private SqliteConnection _connection = null!;
        private DbContextOptions<ApplicationDbContext> _options = null!;

        [SetUp]
        public void SetUp()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = CreateContext();
            context.Database.EnsureCreated();

            context.Profiles.Add(new Profile { ProfileID = UserId, UserName = UserId, SeqNo = 1, VirtualCoins = 10 });
            context.Achievements.Add(new Achievement { AchievementID = 1, Code = AchievementNames.BetaInvolver, Title = "Beta 用戶", Rank = 1 });
            context.Achievements.Add(new Achievement { AchievementID = 2, Code = AchievementNames.Professional, Title = "專業創作者", Rank = 2 });
            context.Achievements.Add(new Achievement { AchievementID = 3, Code = AchievementNames.Editor, Title = "編輯", Rank = 3 });
            context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _connection?.Dispose();
        }

        private ApplicationDbContext CreateContext() => new TestDbContext(_options);

        /// <summary>
        /// 調整只有 SQL Server 支援的設定，讓模型可以在 SQLite 建立
        /// </summary>
        private class TestDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                // SeqNo 在 SQL Server 由資料庫產生，測試中改由程式指定
                modelBuilder.Entity<Profile>().Property(p => p.SeqNo).ValueGeneratedNever();
                modelBuilder.Entity<ProfileAchievement>().Property(p => p.SeqNo).ValueGeneratedNever();
                modelBuilder.Entity<ProfileAchievement>().HasIndex(p => p.SeqNo).IsUnique(false);
                // SQLite 沒有 getdate()
                modelBuilder.Entity<Article>().Property(a => a.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            }
        }

        private static AchievementService CreateService(ApplicationDbContext context) => new(context, new CoinService(context));

        private async Task<Profile> GetProfileAsync()
        {
            using var context = CreateContext();
            return await context.Profiles
                .Include(p => p.Achievements)
                .AsNoTracking()
                .SingleAsync(p => p.ProfileID == UserId);
        }

        private async Task<CoinTransaction[]> GetTransactionsAsync()
        {
            using var context = CreateContext();
            return await context.CoinTransactions.AsNoTracking().OrderBy(t => t.Id).ToArrayAsync();
        }

        // 測試資料中成就的 Rank 與 AchievementID 相同：1 銅、2 銀、3 金
        [TestCase(1, nameof(AchievementService.BeBetaInvolverAsync))]
        [TestCase(2, nameof(AchievementService.BeProfessionalAsync))]
        [TestCase(3, nameof(AchievementService.FirstTimeEditAsync))]
        public async Task GetToasts_WhenAchievementUnlocked_AwardsCoinsByRank(int achievementId, string method)
        {
            decimal expectedAward = achievementId switch
            {
                1 => Parameters.BronzeBadgeAward,
                2 => Parameters.SilverBadgeAward,
                _ => Parameters.GoldBadgeAward,
            };

            using var context = CreateContext();
            var service = CreateService(context);

            var toasts = method switch
            {
                nameof(AchievementService.BeBetaInvolverAsync) => await service.BeBetaInvolverAsync(UserId),
                nameof(AchievementService.BeProfessionalAsync) => await service.BeProfessionalAsync(UserId),
                _ => await service.FirstTimeEditAsync(UserId),
            };

            var profile = await GetProfileAsync();
            var transaction = (await GetTransactionsAsync()).Single();
            Assert.That(toasts.Single().Award, Is.EqualTo(expectedAward));
            Assert.That(profile.VirtualCoins, Is.EqualTo(10 + expectedAward));
            Assert.That(profile.Achievements!.Select(a => a.AchievementID), Is.EqualTo(new[] { achievementId }));
            Assert.That(transaction.TransactionType, Is.EqualTo(CoinTransactionType.AchievementReward));
            Assert.That(transaction.CoinType, Is.EqualTo(CoinType.Virtual));
            Assert.That(transaction.Amount, Is.EqualTo(expectedAward));
            Assert.That(transaction.ReferenceType, Is.EqualTo(CoinReferenceType.Achievement));
            Assert.That(transaction.ReferenceId, Is.EqualTo(achievementId.ToString()));
        }

        [Test]
        public async Task GetToasts_WhenAchievementAlreadyUnlocked_DoesNotAwardAgain()
        {
            using (var context = CreateContext())
            {
                await CreateService(context).BeBetaInvolverAsync(UserId);
            }

            using (var context = CreateContext())
            {
                var toasts = await CreateService(context).BeBetaInvolverAsync(UserId);
                Assert.That(toasts, Is.Empty);
            }

            var profile = await GetProfileAsync();
            Assert.That(profile.VirtualCoins, Is.EqualTo(10 + Parameters.BronzeBadgeAward));
            Assert.That(await GetTransactionsAsync(), Has.Length.EqualTo(1));
        }

        [Test]
        public async Task GetToasts_WhenCallerTransactionRolledBack_RollsBackAchievementAndAward()
        {
            using (var context = CreateContext())
            {
                await using var transaction = await context.Database.BeginTransactionAsync();
                await CreateService(context).BeBetaInvolverAsync(UserId);
            }

            var profile = await GetProfileAsync();
            Assert.That(profile.VirtualCoins, Is.EqualTo(10));
            Assert.That(profile.Achievements, Is.Empty);
            Assert.That(await GetTransactionsAsync(), Is.Empty);
        }
    }
}
