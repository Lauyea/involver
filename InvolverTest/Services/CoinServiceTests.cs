using System;
using System.Threading.Tasks;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;
using Involver.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InvolverTest.Services
{
    [TestFixture]
    public class CoinServiceTests
    {
        private const string PayerId = "payer";
        private const string CreatorId = "creator";
        private const int NovelId = 1;
        private const int ArticleId = 1;

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

            context.Profiles.Add(new Profile { ProfileID = PayerId, UserName = PayerId, SeqNo = 1, RealCoins = 100, VirtualCoins = 50 });
            context.Profiles.Add(new Profile { ProfileID = CreatorId, UserName = CreatorId, SeqNo = 2 });
            context.Novels.Add(new Novel { NovelID = NovelId, Title = "測試創作", Introduction = "介紹", ProfileID = CreatorId });
            context.Articles.Add(new Article { ArticleID = ArticleId, Title = "測試文章", Content = "內容", ProfileID = CreatorId });
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
                modelBuilder.Entity<Article>().Property(a => a.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            }
        }

        private async Task<Profile> GetProfileAsync(string profileId)
        {
            using var context = CreateContext();
            return await context.Profiles.AsNoTracking().SingleAsync(p => p.ProfileID == profileId);
        }

        [Test]
        public async Task TrySpendAsync_WithSufficientBalance_DeductsRealCoinsAndAddsUsedCoins()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            var result = await service.TrySpendAsync(PayerId, 30);

            var payer = await GetProfileAsync(PayerId);
            Assert.That(result, Is.True);
            Assert.That(payer.RealCoins, Is.EqualTo(70));
            Assert.That(payer.UsedCoins, Is.EqualTo(30));
            Assert.That(payer.VirtualCoins, Is.EqualTo(50));
        }

        [Test]
        public async Task TrySpendAsync_WithInsufficientBalance_ReturnsFalseAndKeepsBalance()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            var result = await service.TrySpendAsync(PayerId, 101);

            var payer = await GetProfileAsync(PayerId);
            Assert.That(result, Is.False);
            Assert.That(payer.RealCoins, Is.EqualTo(100));
            Assert.That(payer.UsedCoins, Is.EqualTo(0));
        }

        [Test]
        public async Task TrySpendAsync_WithExactBalance_DeductsToZero()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            var result = await service.TrySpendAsync(PayerId, 100);

            var payer = await GetProfileAsync(PayerId);
            Assert.That(result, Is.True);
            Assert.That(payer.RealCoins, Is.EqualTo(0));
        }

        [Test]
        public async Task TrySpendAsync_RepeatedSpendBeyondBalance_NeverGoesNegative()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            var first = await service.TrySpendAsync(PayerId, 60);
            var second = await service.TrySpendAsync(PayerId, 60);

            var payer = await GetProfileAsync(PayerId);
            Assert.That(first, Is.True);
            Assert.That(second, Is.False);
            Assert.That(payer.RealCoins, Is.EqualTo(40));
            Assert.That(payer.UsedCoins, Is.EqualTo(60));
        }

        [Test]
        public async Task TrySpendAsync_WhenUsingVirtualCoins_DeductsVirtualCoinsOnly()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            var result = await service.TrySpendAsync(PayerId, 20, isVirtual: true);

            var payer = await GetProfileAsync(PayerId);
            Assert.That(result, Is.True);
            Assert.That(payer.VirtualCoins, Is.EqualTo(30));
            Assert.That(payer.RealCoins, Is.EqualTo(100));
            Assert.That(payer.UsedCoins, Is.EqualTo(20));
        }

        [Test]
        public async Task TrySpendAsync_WhenProfileNotFound_ReturnsFalse()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            var result = await service.TrySpendAsync("non-existent", 10);

            Assert.That(result, Is.False);
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void TrySpendAsync_WithNonPositiveAmount_ThrowsArgumentOutOfRangeException(decimal amount)
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.TrySpendAsync(PayerId, amount));
        }

        [Test]
        public async Task TrySpendAsync_WhenTransactionNotCommitted_RestoresBalance()
        {
            using (var context = CreateContext())
            {
                var service = new CoinService(context);
                await using var transaction = await context.Database.BeginTransactionAsync();

                await service.TrySpendAsync(PayerId, 30);
                await service.AddCreatorIncomeAsync(CreatorId, 15);
            }

            var payer = await GetProfileAsync(PayerId);
            var creator = await GetProfileAsync(CreatorId);
            Assert.That(payer.RealCoins, Is.EqualTo(100));
            Assert.That(creator.MonthlyCoins, Is.EqualTo(0));
        }

        [Test]
        public async Task TrySpendAsync_WithStaleTrackedProfile_IsNotOverwrittenBySaveChanges()
        {
            // 模擬頁面先讀出 Profile 並修改其他欄位，期間另一個請求扣了款
            using var pageContext = CreateContext();
            var trackedPayer = await pageContext.Profiles.SingleAsync(p => p.ProfileID == PayerId);

            using (var otherRequestContext = CreateContext())
            {
                await new CoinService(otherRequestContext).TrySpendAsync(PayerId, 30);
            }

            trackedPayer.Introduction = "更新自我介紹";
            await pageContext.SaveChangesAsync();

            var payer = await GetProfileAsync(PayerId);
            Assert.That(payer.RealCoins, Is.EqualTo(70));
            Assert.That(payer.Introduction, Is.EqualTo("更新自我介紹"));
        }

        [Test]
        public async Task AddRealCoinsAsync_WhenProfileExists_AddsRealCoins()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            var result = await service.AddRealCoinsAsync(PayerId, 500);

            var payer = await GetProfileAsync(PayerId);
            Assert.That(result, Is.True);
            Assert.That(payer.RealCoins, Is.EqualTo(600));
        }

        [Test]
        public async Task AddRealCoinsAsync_WhenProfileNotFound_ReturnsFalse()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            var result = await service.AddRealCoinsAsync("non-existent", 500);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task AddVirtualCoinsAsync_WithValidAmount_AddsVirtualCoins()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            await service.AddVirtualCoinsAsync(PayerId, 5);

            var payer = await GetProfileAsync(PayerId);
            Assert.That(payer.VirtualCoins, Is.EqualTo(55));
        }

        [Test]
        public async Task AddCreatorIncomeAsync_CalledTwice_AccumulatesMonthlyCoins()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            await service.AddCreatorIncomeAsync(CreatorId, 15m);
            await service.AddCreatorIncomeAsync(CreatorId, 0.6m);

            var creator = await GetProfileAsync(CreatorId);
            Assert.That(creator.MonthlyCoins, Is.EqualTo(15.6m));
        }

        [Test]
        public async Task AddNovelCoinsAsync_WithValidAmount_AddsMonthlyAndTotalCoins()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            await service.AddNovelCoinsAsync(NovelId, 150);

            var novel = await context.Novels.AsNoTracking().SingleAsync(n => n.NovelID == NovelId);
            Assert.That(novel.MonthlyCoins, Is.EqualTo(150));
            Assert.That(novel.TotalCoins, Is.EqualTo(150));
        }

        [Test]
        public async Task AddArticleCoinsAsync_WithValidAmount_AddsMonthlyAndTotalCoins()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            await service.AddArticleCoinsAsync(ArticleId, 30);

            var article = await context.Articles.AsNoTracking().SingleAsync(a => a.ArticleID == ArticleId);
            Assert.That(article.MonthlyCoins, Is.EqualTo(30));
            Assert.That(article.TotalCoins, Is.EqualTo(30));
        }
    }
}
