using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;
using DataAccess.Models.StatisticalData;
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

        private static readonly CoinTransactionInfo SpendInfo = new(CoinTransactionType.Vote, CoinReferenceType.Voting, "1", CreatorId);
        private static readonly CoinTransactionInfo TopUpInfo = new(CoinTransactionType.TopUp, Note: "儲值");
        private static readonly CoinTransactionInfo RewardInfo = new(CoinTransactionType.MissionReward);
        private static readonly CoinTransactionInfo IncomeInfo = new(CoinTransactionType.RevenueShare, CounterpartyProfileId: PayerId);

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

        private async Task<List<CoinTransaction>> GetTransactionsAsync()
        {
            using var context = CreateContext();
            return await context.CoinTransactions.AsNoTracking().OrderBy(t => t.Id).ToListAsync();
        }

        [Test]
        public async Task TrySpendAsync_WithSufficientBalance_DeductsRealCoinsAndAddsUsedCoins()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            var result = await service.TrySpendAsync(PayerId, 30, SpendInfo);

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

            var result = await service.TrySpendAsync(PayerId, 101, SpendInfo);

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

            var result = await service.TrySpendAsync(PayerId, 100, SpendInfo);

            var payer = await GetProfileAsync(PayerId);
            Assert.That(result, Is.True);
            Assert.That(payer.RealCoins, Is.EqualTo(0));
        }

        [Test]
        public async Task TrySpendAsync_RepeatedSpendBeyondBalance_NeverGoesNegative()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            var first = await service.TrySpendAsync(PayerId, 60, SpendInfo);
            var second = await service.TrySpendAsync(PayerId, 60, SpendInfo);

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

            var result = await service.TrySpendAsync(PayerId, 20, SpendInfo, isVirtual: true);

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

            var result = await service.TrySpendAsync("non-existent", 10, SpendInfo);

            Assert.That(result, Is.False);
        }

        [TestCase(0)]
        [TestCase(-10)]
        public void TrySpendAsync_WithNonPositiveAmount_ThrowsArgumentOutOfRangeException(decimal amount)
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.TrySpendAsync(PayerId, amount, SpendInfo));
        }

        [Test]
        public async Task TrySpendAsync_WhenTransactionNotCommitted_RestoresBalance()
        {
            using (var context = CreateContext())
            {
                var service = new CoinService(context);
                await using var transaction = await context.Database.BeginTransactionAsync();

                await service.TrySpendAsync(PayerId, 30, SpendInfo);
                await service.AddCreatorIncomeAsync(CreatorId, 15, IncomeInfo);
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
                await new CoinService(otherRequestContext).TrySpendAsync(PayerId, 30, SpendInfo);
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

            var result = await service.AddRealCoinsAsync(PayerId, 500, TopUpInfo);

            var payer = await GetProfileAsync(PayerId);
            Assert.That(result, Is.True);
            Assert.That(payer.RealCoins, Is.EqualTo(600));
        }

        [Test]
        public async Task AddRealCoinsAsync_WhenProfileNotFound_ReturnsFalse()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            var result = await service.AddRealCoinsAsync("non-existent", 500, TopUpInfo);

            Assert.That(result, Is.False);
        }

        [Test]
        public async Task AddVirtualCoinsAsync_WithValidAmount_AddsVirtualCoins()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            await service.AddVirtualCoinsAsync(PayerId, 5, RewardInfo);

            var payer = await GetProfileAsync(PayerId);
            Assert.That(payer.VirtualCoins, Is.EqualTo(55));
        }

        [Test]
        public async Task AddCreatorIncomeAsync_CalledTwice_AccumulatesMonthlyCoins()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            await service.AddCreatorIncomeAsync(CreatorId, 15m, IncomeInfo);
            await service.AddCreatorIncomeAsync(CreatorId, 0.6m, IncomeInfo);

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

        [Test]
        public async Task TrySpendAsync_WithSufficientBalance_WritesNegativeTransactionWithBalanceAfter()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            await service.TrySpendAsync(PayerId, 30, SpendInfo);

            var transaction = (await GetTransactionsAsync()).Single();
            Assert.That(transaction.ProfileId, Is.EqualTo(PayerId));
            Assert.That(transaction.CoinType, Is.EqualTo(CoinType.Real));
            Assert.That(transaction.TransactionType, Is.EqualTo(CoinTransactionType.Vote));
            Assert.That(transaction.Amount, Is.EqualTo(-30));
            Assert.That(transaction.BalanceAfter, Is.EqualTo(70));
            Assert.That(transaction.ReferenceType, Is.EqualTo(CoinReferenceType.Voting));
            Assert.That(transaction.ReferenceId, Is.EqualTo("1"));
            Assert.That(transaction.CounterpartyProfileId, Is.EqualTo(CreatorId));
            Assert.That(transaction.CorrelationId, Is.EqualTo(service.CorrelationId));
        }

        [Test]
        public async Task TrySpendAsync_WhenUsingVirtualCoins_WritesVirtualTransaction()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            await service.TrySpendAsync(PayerId, 20, SpendInfo, isVirtual: true);

            var transaction = (await GetTransactionsAsync()).Single();
            Assert.That(transaction.CoinType, Is.EqualTo(CoinType.Virtual));
            Assert.That(transaction.Amount, Is.EqualTo(-20));
            Assert.That(transaction.BalanceAfter, Is.EqualTo(30));
        }

        [Test]
        public async Task TrySpendAsync_WithInsufficientBalance_WritesNoTransaction()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            await service.TrySpendAsync(PayerId, 101, SpendInfo);

            Assert.That(await GetTransactionsAsync(), Is.Empty);
        }

        [Test]
        public async Task AddCoinsAsync_ForEachCoinType_WritesPositiveTransactions()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            await service.AddRealCoinsAsync(PayerId, 500, TopUpInfo);
            await service.AddVirtualCoinsAsync(PayerId, 5, RewardInfo);
            await service.AddCreatorIncomeAsync(CreatorId, 15.5m, IncomeInfo);

            var transactions = await GetTransactionsAsync();
            Assert.That(transactions.Select(t => (t.ProfileId, t.CoinType, t.Amount, t.BalanceAfter)), Is.EqualTo(new[]
            {
                (PayerId, CoinType.Real, 500m, 600m),
                (PayerId, CoinType.Virtual, 5m, 55m),
                (CreatorId, CoinType.CreatorIncome, 15.5m, 15.5m),
            }));
            Assert.That(transactions[0].Note, Is.EqualTo("儲值"));
        }

        [Test]
        public async Task AddRealCoinsAsync_WhenProfileNotFound_WritesNoTransaction()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            await service.AddRealCoinsAsync("non-existent", 500, TopUpInfo);

            Assert.That(await GetTransactionsAsync(), Is.Empty);
        }

        [Test]
        public async Task AddRealCoinsAsync_WithDuplicateIdempotencyKey_ThrowsAndKeepsBalance()
        {
            var info = CoinTransactionInfo.ForTopUp(PaymentProvider.ECPay, "T001");
            using (var context = CreateContext())
            {
                await new CoinService(context).AddRealCoinsAsync(PayerId, 500, info);
            }

            using (var context = CreateContext())
            {
                await Assert.CatchAsync<System.Data.Common.DbException>(() => new CoinService(context).AddRealCoinsAsync(PayerId, 500, info));
            }

            var payer = await GetProfileAsync(PayerId);
            Assert.That(payer.RealCoins, Is.EqualTo(600));
            Assert.That(await GetTransactionsAsync(), Has.Count.EqualTo(1));
        }

        [Test]
        public async Task AddRealCoinsAsync_WithSamePaymentIdFromDifferentProviders_CreditsBoth()
        {
            using (var context = CreateContext())
            {
                await new CoinService(context).AddRealCoinsAsync(PayerId, 100, CoinTransactionInfo.ForTopUp(PaymentProvider.ECPay, "T001"));
            }

            using (var context = CreateContext())
            {
                await new CoinService(context).AddRealCoinsAsync(PayerId, 100, CoinTransactionInfo.ForTopUp(PaymentProvider.Stripe, "T001"));
            }

            var payer = await GetProfileAsync(PayerId);
            var transactions = await GetTransactionsAsync();
            Assert.That(payer.RealCoins, Is.EqualTo(300));
            Assert.That(transactions.Select(t => t.ReferenceId), Is.EqualTo(new[] { "ECPay:T001", "Stripe:T001" }));
            Assert.That(transactions.Select(t => t.ReferenceType), Is.All.EqualTo(CoinReferenceType.Payment));
        }

        [Test]
        public async Task AddVirtualCoinsAsync_WithLongNote_TruncatesNoteTo200Characters()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            await service.AddVirtualCoinsAsync(PayerId, 5, RewardInfo with { Note = new string('a', 250) });

            var transaction = (await GetTransactionsAsync()).Single();
            Assert.That(transaction.Note, Has.Length.EqualTo(200));
        }

        [Test]
        public async Task TrySpendAsync_WhenTransactionNotCommitted_RollsBackTransactions()
        {
            using (var context = CreateContext())
            {
                var service = new CoinService(context);
                await using var transaction = await context.Database.BeginTransactionAsync();

                await service.TrySpendAsync(PayerId, 30, SpendInfo);
                await service.AddCreatorIncomeAsync(CreatorId, 15, IncomeInfo);
            }

            Assert.That(await GetTransactionsAsync(), Is.Empty);
        }

        [Test]
        public async Task TrySpendAsync_WithPendingTrackedChanges_DoesNotSaveThem()
        {
            // 流水帳以 SQL 直接寫入，不能順便把呼叫端還沒存的追蹤實體存進資料庫
            using var context = CreateContext();
            var tracked = await context.Profiles.SingleAsync(p => p.ProfileID == PayerId);
            tracked.Introduction = "尚未存檔";

            await new CoinService(context).TrySpendAsync(PayerId, 30, SpendInfo);

            var payer = await GetProfileAsync(PayerId);
            Assert.That(payer.Introduction, Is.Not.EqualTo("尚未存檔"));
            Assert.That(payer.RealCoins, Is.EqualTo(70));
        }

        [Test]
        public async Task CoinTransactions_SumOfAmounts_EqualsBalanceChange()
        {
            using var context = CreateContext();
            var service = new CoinService(context);

            await service.AddRealCoinsAsync(PayerId, 200, TopUpInfo);
            await service.TrySpendAsync(PayerId, 50, SpendInfo);
            await service.TrySpendAsync(PayerId, 1000, SpendInfo);
            await service.TrySpendAsync(PayerId, 25, SpendInfo);

            var transactions = await GetTransactionsAsync();
            var payer = await GetProfileAsync(PayerId);
            var realTransactions = transactions.Where(t => t.CoinType == CoinType.Real).ToList();
            Assert.That(100 + realTransactions.Sum(t => t.Amount), Is.EqualTo(payer.RealCoins));
            Assert.That(realTransactions.Last().BalanceAfter, Is.EqualTo(payer.RealCoins));
        }

    }
}
