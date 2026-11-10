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
    public class ViewCountServiceTests
    {
        private const string CreatorId = "creator";
        private const int NovelId = 1;
        private const int ArticleId = 1;
        private const int EpisodeId = 1;

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

            context.Profiles.Add(new Profile { ProfileID = CreatorId, UserName = CreatorId, SeqNo = 1, Views = 10 });
            context.Novels.Add(new Novel { NovelID = NovelId, Title = "測試創作", Introduction = "介紹", ProfileID = CreatorId, TotalViews = 10, DailyView = 3 });
            context.Articles.Add(new Article { ArticleID = ArticleId, Title = "測試文章", Content = "內容", ProfileID = CreatorId, TotalViews = 10, DailyView = 3 });
            context.Episodes.Add(new Episode { EpisodeID = EpisodeId, Title = "第一章", Content = "內容", NovelID = NovelId, Views = 10 });
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

        private async Task<Novel> GetNovelAsync()
        {
            using var context = CreateContext();
            return await context.Novels.AsNoTracking().SingleAsync(n => n.NovelID == NovelId);
        }

        private async Task<Article> GetArticleAsync()
        {
            using var context = CreateContext();
            return await context.Articles.AsNoTracking().SingleAsync(a => a.ArticleID == ArticleId);
        }

        [Test]
        public async Task AddNovelViewAsync_WithTrackedNovel_IncrementsDatabaseAndMemory()
        {
            using var context = CreateContext();
            var novel = await context.Novels.SingleAsync(n => n.NovelID == NovelId);
            var service = new ViewCountService(context);

            await service.AddNovelViewAsync(novel);

            var saved = await GetNovelAsync();
            Assert.That(saved.TotalViews, Is.EqualTo(11));
            Assert.That(saved.DailyView, Is.EqualTo(4));
            Assert.That(novel.TotalViews, Is.EqualTo(11));
            Assert.That(novel.DailyView, Is.EqualTo(4));
        }

        [Test]
        public async Task AddNovelViewAsync_WithTrackedNovel_DoesNotMarkCountersModified()
        {
            using var context = CreateContext();
            var novel = await context.Novels.SingleAsync(n => n.NovelID == NovelId);
            var service = new ViewCountService(context);

            await service.AddNovelViewAsync(novel);
            context.ChangeTracker.DetectChanges();

            var entry = context.Entry(novel);
            Assert.That(entry.State, Is.EqualTo(EntityState.Unchanged));
            Assert.That(entry.Property(n => n.TotalViews).IsModified, Is.False);
            Assert.That(entry.Property(n => n.DailyView).IsModified, Is.False);
        }

        [Test]
        public async Task AddNovelViewAsync_ConcurrentRequestsWithLaterSave_CountsEveryView()
        {
            // 模擬兩個併發請求：都先讀到 TotalViews = 10，各自累加後再存檔其他欄位
            using var first = CreateContext();
            using var second = CreateContext();
            var novelInFirst = await first.Novels.SingleAsync(n => n.NovelID == NovelId);
            var novelInSecond = await second.Novels.SingleAsync(n => n.NovelID == NovelId);

            await new ViewCountService(first).AddNovelViewAsync(novelInFirst);
            await new ViewCountService(second).AddNovelViewAsync(novelInSecond);

            novelInFirst.Block = true;
            await first.SaveChangesAsync();
            await second.SaveChangesAsync();

            var saved = await GetNovelAsync();
            Assert.That(saved.TotalViews, Is.EqualTo(12));
            Assert.That(saved.DailyView, Is.EqualTo(5));
            Assert.That(saved.Block, Is.True);
        }

        [Test]
        public async Task AddNovelViewAsync_WithDetachedNovel_IncrementsDatabaseAndMemory()
        {
            using var context = CreateContext();
            var novel = await context.Novels.AsNoTracking().SingleAsync(n => n.NovelID == NovelId);
            var service = new ViewCountService(context);

            await service.AddNovelViewAsync(novel);

            var saved = await GetNovelAsync();
            Assert.That(saved.TotalViews, Is.EqualTo(11));
            Assert.That(novel.TotalViews, Is.EqualTo(11));
            Assert.That(context.Entry(novel).State, Is.EqualTo(EntityState.Detached));
        }

        [Test]
        public async Task AddArticleViewAsync_WithCountDaily_IncrementsTotalAndDailyViews()
        {
            using var context = CreateContext();
            var article = await context.Articles.SingleAsync(a => a.ArticleID == ArticleId);
            var service = new ViewCountService(context);

            await service.AddArticleViewAsync(article);

            var saved = await GetArticleAsync();
            Assert.That(saved.TotalViews, Is.EqualTo(11));
            Assert.That(saved.DailyView, Is.EqualTo(4));
            Assert.That(article.TotalViews, Is.EqualTo(11));
        }

        [Test]
        public async Task AddArticleViewAsync_WithoutCountDaily_IncrementsTotalViewsOnly()
        {
            using var context = CreateContext();
            var article = await context.Articles.SingleAsync(a => a.ArticleID == ArticleId);
            var service = new ViewCountService(context);

            await service.AddArticleViewAsync(article, countDaily: false);

            var saved = await GetArticleAsync();
            Assert.That(saved.TotalViews, Is.EqualTo(11));
            Assert.That(saved.DailyView, Is.EqualTo(3));
            Assert.That(article.DailyView, Is.EqualTo(3));
        }

        [Test]
        public async Task AddEpisodeViewAsync_WithStaleEntityLaterSaved_KeepsAtomicIncrement()
        {
            using var context = CreateContext();
            var episode = await context.Episodes.SingleAsync(e => e.EpisodeID == EpisodeId);

            // 其他請求在這之間累加了觀看數
            using (var other = CreateContext())
            {
                var otherEpisode = await other.Episodes.SingleAsync(e => e.EpisodeID == EpisodeId);
                await new ViewCountService(other).AddEpisodeViewAsync(otherEpisode);
            }

            await new ViewCountService(context).AddEpisodeViewAsync(episode);
            episode.Title = "改過的標題";
            await context.SaveChangesAsync();

            using var verify = CreateContext();
            var saved = await verify.Episodes.AsNoTracking().SingleAsync(e => e.EpisodeID == EpisodeId);
            Assert.That(saved.Views, Is.EqualTo(12));
            Assert.That(saved.Title, Is.EqualTo("改過的標題"));
        }

        [Test]
        public async Task AddProfileViewAsync_WithTrackedProfile_IncrementsViews()
        {
            using var context = CreateContext();
            var profile = await context.Profiles.SingleAsync(p => p.ProfileID == CreatorId);
            var service = new ViewCountService(context);

            await service.AddProfileViewAsync(profile);

            using var verify = CreateContext();
            var saved = await verify.Profiles.AsNoTracking().SingleAsync(p => p.ProfileID == CreatorId);
            Assert.That(saved.Views, Is.EqualTo(11));
            Assert.That(profile.Views, Is.EqualTo(11));
        }
    }
}
