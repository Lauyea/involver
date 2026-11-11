using System.Linq;
using System.Threading.Tasks;
using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.StatisticalData;
using Involver.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InvolverTest.Services
{
    [TestFixture]
    public class MissionServiceTests
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

            context.Profiles.Add(new Profile
            {
                ProfileID = UserId,
                UserName = UserId,
                SeqNo = 1,
                VirtualCoins = 10,
                Missions = new Missions { ProfileID = UserId },
            });
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
                // SQLite 沒有 getdate()
                modelBuilder.Entity<DataAccess.Models.ArticleModel.Article>().Property(a => a.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            }
        }

        private static MissionService CreateService(ApplicationDbContext context) => new(context, new CoinService(context));

        private async Task<Profile> GetProfileAsync()
        {
            using var context = CreateContext();
            return await context.Profiles
                .Include(p => p.Missions)
                .AsNoTracking()
                .SingleAsync(p => p.ProfileID == UserId);
        }

        private async Task<CoinTransaction[]> GetTransactionsAsync()
        {
            using var context = CreateContext();
            return await context.CoinTransactions.AsNoTracking().OrderBy(t => t.Id).ToArrayAsync();
        }

        [Test]
        public async Task CompleteAsync_FirstTime_SetsFlagAndAwardsCoins()
        {
            using var context = CreateContext();

            var result = await CreateService(context).CompleteAsync(UserId, MissionType.LeaveComment);

            var profile = await GetProfileAsync();
            var transaction = (await GetTransactionsAsync()).Single();
            Assert.That(result, Is.True);
            Assert.That(profile.Missions!.LeaveComment, Is.True);
            Assert.That(profile.VirtualCoins, Is.EqualTo(10 + Parameters.MissionAwardCoins));
            Assert.That(transaction.TransactionType, Is.EqualTo(CoinTransactionType.MissionReward));
            Assert.That(transaction.CoinType, Is.EqualTo(CoinType.Virtual));
            Assert.That(transaction.BalanceAfter, Is.EqualTo(profile.VirtualCoins));
        }

        [Test]
        public async Task CompleteAsync_WhenAlreadyCompleted_ReturnsFalseWithoutAward()
        {
            using (var context = CreateContext())
            {
                await CreateService(context).CompleteAsync(UserId, MissionType.Vote);
            }

            bool second;
            using (var context = CreateContext())
            {
                second = await CreateService(context).CompleteAsync(UserId, MissionType.Vote);
            }

            var profile = await GetProfileAsync();
            Assert.That(second, Is.False);
            Assert.That(profile.VirtualCoins, Is.EqualTo(10 + Parameters.MissionAwardCoins));
            Assert.That(await GetTransactionsAsync(), Has.Length.EqualTo(1));
        }

        [Test]
        public async Task CompleteAsync_WithDailyLogin_WritesLoginRewardAndNoAllMissionsBonus()
        {
            using var context = CreateContext();

            await CreateService(context).CompleteAsync(UserId, MissionType.DailyLogin);

            var transaction = (await GetTransactionsAsync()).Single();
            Assert.That(transaction.TransactionType, Is.EqualTo(CoinTransactionType.LoginReward));
        }

        [Test]
        public async Task CompleteAsync_WhenLastWeeklyMissionCompleted_AwardsAllMissionsBonusOnce()
        {
            var weeklyMissions = new[]
            {
                MissionType.WatchArticle,
                MissionType.Vote,
                MissionType.LeaveComment,
                MissionType.ViewAnnouncement,
                MissionType.ShareCreation,
                MissionType.BeAgreed,
            };

            using (var context = CreateContext())
            {
                var service = CreateService(context);
                foreach (var mission in weeklyMissions)
                {
                    await service.CompleteAsync(UserId, mission);
                }

                // 已完成的任務再觸發一次，不能再拿到全勤獎勵
                await service.CompleteAsync(UserId, MissionType.BeAgreed);
            }

            var profile = await GetProfileAsync();
            Assert.That(profile.Missions!.CompleteOtherMissions, Is.True);
            Assert.That(await GetTransactionsAsync(), Has.Length.EqualTo(weeklyMissions.Length + 1));
            Assert.That(profile.VirtualCoins, Is.EqualTo(10 + Parameters.MissionAwardCoins * (weeklyMissions.Length + 1)));
        }

        [Test]
        public async Task CompleteAsync_WhenProfileNotFound_ReturnsFalse()
        {
            using var context = CreateContext();

            var result = await CreateService(context).CompleteAsync("non-existent", MissionType.Vote);

            Assert.That(result, Is.False);
            Assert.That(await GetTransactionsAsync(), Is.Empty);
        }

        [Test]
        public async Task CompleteAsync_WithStaleTrackedMissions_IsNotOverwrittenBySaveChanges()
        {
            // 頁面先讀出 Profile 與 Missions，任務服務直接改資料庫，之後存檔不能把旗標或餘額蓋回舊值
            using var context = CreateContext();
            var tracked = await context.Profiles.Include(p => p.Missions).SingleAsync(p => p.ProfileID == UserId);

            await CreateService(context).CompleteAsync(UserId, MissionType.ShareCreation);

            tracked.Introduction = "更新自我介紹";
            await context.SaveChangesAsync();

            var profile = await GetProfileAsync();
            Assert.That(profile.Missions!.ShareCreation, Is.True);
            Assert.That(profile.VirtualCoins, Is.EqualTo(10 + Parameters.MissionAwardCoins));
        }
    }
}
