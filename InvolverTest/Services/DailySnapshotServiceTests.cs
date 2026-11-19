using System;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.Dashboard;
using DataAccess.Models.NovelModel;
using DataAccess.Models.StatisticalData;
using DataAccess.Services;

using InvolverTest.Infrastructure;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

namespace InvolverTest.Services
{
    /// <summary>
    /// Issue #237：每日快照的計算與寫入
    /// </summary>
    [TestFixture]
    public class DailySnapshotServiceTests
    {
        private const string CreatorId = "creator";
        private const string ReaderId = "reader";
        private const string Reader2Id = "reader2";
        private const string OldUserId = "old";

        /// <summary>
        /// 快照的日期；測試以 UTC 當作伺服器時區，伺服器時間與 UTC 相同
        /// </summary>
        private static readonly DateOnly Day = new(2026, 11, 18);

        private static readonly DateTime DayStart = Day.ToDateTime(TimeOnly.MinValue);

        private SqliteTestDatabase _database = null!;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            using var context = _database.CreateContext();

            AddUser(context, CreatorId, 1, enrolled: new DateTime(2026, 1, 1), lastLogin: DayStart.AddHours(10), professional: true);
            AddUser(context, ReaderId, 2, enrolled: DayStart.AddHours(9), lastLogin: DayStart.AddHours(9), prime: true);
            AddUser(context, Reader2Id, 3, enrolled: new DateTime(2026, 10, 1), lastLogin: DayStart.AddDays(-10));
            AddUser(context, OldUserId, 4, enrolled: new DateTime(2025, 1, 1), lastLogin: DayStart.AddDays(-60));

            context.Novels.AddRange(
                new Novel { NovelID = 1, Title = "今天的作品", Introduction = "介紹介紹", ProfileID = CreatorId, CreateTime = DayStart.AddHours(8), DailyView = 5 },
                new Novel { NovelID = 2, Title = "已刪除的作品", Introduction = "介紹介紹", ProfileID = CreatorId, CreateTime = new DateTime(2026, 1, 1), IsDeleted = true });
            context.Articles.AddRange(
                new Article { ArticleID = 1, Title = "今天的文章", Content = "內容", ProfileID = CreatorId, Type = ArticleType.General, CreateTime = DayStart.AddHours(12), DailyView = 3 },
                new Article { ArticleID = 2, Title = "意見回饋", Content = "內容", ProfileID = ReaderId, Type = ArticleType.Feedback, CreateTime = DayStart.AddDays(-3), DailyView = 2 });
            context.Episodes.AddRange(
                new Episode { EpisodeID = 1, Title = "第一章", Content = "內容", NovelID = 1, OwnerID = CreatorId, UpdateTime = DayStart.AddHours(8).AddMinutes(30) },
                new Episode { EpisodeID = 2, Title = "刪掉的章節", Content = "內容", NovelID = 1, OwnerID = CreatorId, UpdateTime = DayStart.AddHours(9), IsDeleted = true });
            context.Votings.Add(new Voting { VotingID = 1, Title = "要去哪裡", EpisodeID = 1, OwnerID = CreatorId, Policy = PolicyType.Liberty, CreateTime = DayStart });

            context.Comments.AddRange(
                new Comment { CommentID = 1, Content = "作品留言", NovelID = 1, ProfileID = ReaderId, UpdateTime = DayStart.AddHours(11) },
                new Comment { CommentID = 2, Content = "章節留言", EpisodeID = 1, ProfileID = ReaderId, UpdateTime = DayStart.AddHours(12) },
                new Comment { CommentID = 3, Content = "舊的文章留言", ArticleID = 1, ProfileID = ReaderId, UpdateTime = DayStart.AddDays(-5) });

            context.Views.AddRange(
                new View { NovelId = 1, UserId = ReaderId, CreateTime = DayStart.AddHours(9) },
                new View { NovelId = 1, UserId = ReaderId, CreateTime = DayStart.AddHours(10) },
                new View { NovelId = 1, SessionId = "session-1", CreateTime = DayStart.AddHours(11) },
                new View { NovelId = 1, UserId = Reader2Id, CreateTime = DayStart.AddHours(-1) },
                new View { ArticleId = 1, UserId = ReaderId, CreateTime = DayStart.AddHours(13) });

            context.Follows.AddRange(
                new Follow { FollowerID = ReaderId, NovelID = 1, UpdateTime = DayStart.AddHours(9) },
                new Follow { FollowerID = Reader2Id, ProfileID = CreatorId, UpdateTime = DayStart.AddHours(9) });

            AddTransaction(context, ReaderId, CoinType.Real, CoinTransactionType.TopUp, 100, DayStart.AddHours(9).AddMinutes(10));
            AddTransaction(context, ReaderId, CoinType.Real, CoinTransactionType.Vote, -30, DayStart.AddHours(10), CoinReferenceType.Voting, "1");
            AddTransaction(context, CreatorId, CoinType.CreatorIncome, CoinTransactionType.RevenueShare, 18, DayStart.AddHours(10), CoinReferenceType.Voting, "1");
            AddTransaction(context, ReaderId, CoinType.Real, CoinTransactionType.InvolveArticle, -20, DayStart.AddHours(11), CoinReferenceType.Article, "1");
            AddTransaction(context, Reader2Id, CoinType.Virtual, CoinTransactionType.LoginReward, 5, DayStart.AddHours(12));
            // 前幾天：只影響每週、每月的滾動指標
            AddTransaction(context, Reader2Id, CoinType.Virtual, CoinTransactionType.Vote, -10, DayStart.AddDays(-3), CoinReferenceType.Voting, "1");
            AddTransaction(context, ReaderId, CoinType.Real, CoinTransactionType.InvolveNovel, -40, DayStart.AddDays(-20), CoinReferenceType.Novel, "1");
            AddTransaction(context, OldUserId, CoinType.Real, CoinTransactionType.InvolveNovel, -40, DayStart.AddDays(-40), CoinReferenceType.Novel, "1");

            context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _database?.Dispose();
        }

        private static void AddUser(ApplicationDbContext context, string id, int seqNo, DateTime enrolled, DateTime lastLogin, bool prime = false, bool professional = false)
        {
            context.Users.Add(new InvolverUser { Id = id, UserName = id });
            context.Profiles.Add(new Profile
            {
                ProfileID = id,
                UserName = id,
                SeqNo = seqNo,
                EnrollmentDate = enrolled,
                LastTimeLogin = lastLogin,
                Prime = prime,
                Professional = professional,
            });
        }

        private static void AddTransaction(
            ApplicationDbContext context,
            string profileId,
            CoinType coinType,
            CoinTransactionType type,
            decimal amount,
            DateTime createdAt,
            CoinReferenceType? referenceType = null,
            string? referenceId = null)
        {
            context.CoinTransactions.Add(new CoinTransaction
            {
                ProfileId = profileId,
                CoinType = coinType,
                TransactionType = type,
                Amount = amount,
                ReferenceType = referenceType,
                ReferenceId = referenceId,
                CorrelationId = Guid.NewGuid(),
                CreatedAt = createdAt,
            });
        }

        private async Task<DailySnapshotResult> WriteSnapshotAsync()
        {
            using var context = _database.CreateContext();
            return await new DailySnapshotService(context, TimeZoneInfo.Utc).WriteSnapshotAsync(Day);
        }

        [Test]
        public async Task WriteSnapshotAsync_WithSeededDay_CalculatesSiteStat()
        {
            await WriteSnapshotAsync();

            using var context = _database.CreateContext();
            var site = await context.DailySiteStats.SingleAsync();

            Assert.Multiple(() =>
            {
                Assert.That(site.Date, Is.EqualTo(Day));
                Assert.That(site.TotalUsers, Is.EqualTo(4));
                Assert.That(site.NewUsers, Is.EqualTo(1));
                Assert.That(site.DailyActiveUsers, Is.EqualTo(2));
                Assert.That(site.MonthlyActiveUsers, Is.EqualTo(3));
                Assert.That(site.PrimeUsers, Is.EqualTo(1));
                Assert.That(site.ProfessionalUsers, Is.EqualTo(1));
                Assert.That(site.Novels, Is.EqualTo(1), "已刪除的作品不算");
                Assert.That(site.Articles, Is.EqualTo(1), "只算一般文章");
                Assert.That(site.Episodes, Is.EqualTo(1), "已刪除的章節不算");
                Assert.That(site.Comments, Is.EqualTo(3));
                Assert.That(site.NewNovels, Is.EqualTo(1));
                Assert.That(site.NewArticles, Is.EqualTo(1));
                Assert.That(site.Views, Is.EqualTo(10), "作品與文章（含意見回饋）的 DailyView 合計");
            });
        }

        [Test]
        public async Task WriteSnapshotAsync_WithSeededDay_CalculatesCoinAmountsAndCoreMetrics()
        {
            await WriteSnapshotAsync();

            using var context = _database.CreateContext();
            var site = await context.DailySiteStats.SingleAsync();

            Assert.Multiple(() =>
            {
                Assert.That(site.TopUpAmount, Is.EqualTo(100m));
                Assert.That(site.TopUpCount, Is.EqualTo(1));
                Assert.That(site.RevenueShareAmount, Is.EqualTo(18m));
                Assert.That(site.RealCoinsSpent, Is.EqualTo(50m), "當天的實體 In 幣投票與贊助");
                Assert.That(site.VirtualCoinsIssued, Is.EqualTo(5m));
                Assert.That(site.WeeklyVoters, Is.EqualTo(2), "7 天內投票的讀者，虛擬 In 幣也算");
                Assert.That(site.MonthlyPayingSupporters, Is.EqualTo(1), "30 天內以實體 In 幣支持的讀者");
                Assert.That(site.MonthlyActiveCreators, Is.EqualTo(1));
            });
        }

        [Test]
        public async Task WriteSnapshotAsync_WithSeededDay_WritesContentStatsForActiveContentOnly()
        {
            var result = await WriteSnapshotAsync();

            using var context = _database.CreateContext();
            var stats = await context.DailyContentStats.OrderBy(s => s.ContentType).ThenBy(s => s.ContentId).ToListAsync();
            var novel = stats.Single(s => s.ContentType == ContentStatType.Novel && s.ContentId == 1);
            var article = stats.Single(s => s.ContentType == ContentStatType.Article && s.ContentId == 1);
            var feedback = stats.Single(s => s.ContentType == ContentStatType.Article && s.ContentId == 2);

            Assert.Multiple(() =>
            {
                Assert.That(result.ContentCount, Is.EqualTo(3));
                Assert.That(stats, Has.Count.EqualTo(3), "已刪除、沒有活動的作品不寫入");

                Assert.That(novel.OwnerProfileId, Is.EqualTo(CreatorId));
                Assert.That(novel.Views, Is.EqualTo(5));
                Assert.That(novel.UniqueReaders, Is.EqualTo(2), "同一位用戶只算一次；未登入以 Session 區分");
                Assert.That(novel.NewFollows, Is.EqualTo(1));
                Assert.That(novel.NewComments, Is.EqualTo(2), "章節留言算在作品上");
                Assert.That(novel.CoinsReceived, Is.EqualTo(30m), "投票換算成作品");

                Assert.That(article.Views, Is.EqualTo(3));
                Assert.That(article.UniqueReaders, Is.EqualTo(1));
                Assert.That(article.NewComments, Is.EqualTo(0), "前幾天的留言不算");
                Assert.That(article.CoinsReceived, Is.EqualTo(20m));

                Assert.That(feedback.OwnerProfileId, Is.EqualTo(ReaderId));
                Assert.That(feedback.Views, Is.EqualTo(2));
            });
        }

        [Test]
        public async Task WriteSnapshotAsync_WhenRunTwice_ReplacesExistingRows()
        {
            await WriteSnapshotAsync();
            await WriteSnapshotAsync();

            using var context = _database.CreateContext();

            Assert.That(await context.DailySiteStats.CountAsync(), Is.EqualTo(1));
            Assert.That(await context.DailyContentStats.CountAsync(), Is.EqualTo(3));
        }

        [Test]
        public async Task WriteSnapshotAsync_WithSeededDay_DoesNotResetDailyViews()
        {
            await WriteSnapshotAsync();

            using var context = _database.CreateContext();

            Assert.That(await context.Novels.SumAsync(n => n.DailyView), Is.EqualTo(5), "歸零由排程在快照之後另外執行");
        }

        [Test]
        public void For_WithTaiwanTimeZone_ConvertsDayToUtcRange()
        {
            var taipei = TimeZoneInfo.CreateCustomTimeZone("Taipei", TimeSpan.FromHours(8), "Taipei", "Taipei");

            var window = SnapshotWindow.For(Day, taipei);

            Assert.Multiple(() =>
            {
                Assert.That(window.LocalStart, Is.EqualTo(DayStart));
                Assert.That(window.LocalEnd, Is.EqualTo(DayStart.AddDays(1)));
                Assert.That(window.UtcStart, Is.EqualTo(DayStart.AddHours(-8)));
                Assert.That(window.UtcEnd, Is.EqualTo(DayStart.AddDays(1).AddHours(-8)));
            });
        }

        [Test]
        public void PreviousDay_AtMidnight_ReturnsDayThatJustEnded()
        {
            var date = SnapshotWindow.PreviousDay(new DateTime(2026, 11, 19, 0, 0, 5));

            Assert.That(date, Is.EqualTo(Day));
        }

        [TestCase(120, 100, 20)]
        [TestCase(80, 100, -20)]
        [TestCase(5, 0, null)]
        [TestCase(5, null, null)]
        public void ChangePercent_WithPreviousValue_ReturnsPercentOrNull(int current, int? previous, int? expected)
        {
            var result = DashboardMetrics.ChangePercent(current, previous);

            Assert.That(result, Is.EqualTo(expected.HasValue ? (decimal?)expected.Value : null));
        }
    }
}
