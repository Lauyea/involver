using System.Linq;
using System.Threading.Tasks;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.AchievementModel;
using DataAccess.Models.ArticleModel;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InvolverTest.Data
{
    /// <summary>
    /// Issue #219：資料庫層的 CHECK 與 Unique 約束
    /// </summary>
    [TestFixture]
    public class ApplicationDbContextTests
    {
        private const string UserId = "user";
        private const string OtherId = "other";

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

            context.Users.Add(new InvolverUser { Id = UserId, UserName = UserId });
            context.Profiles.Add(new Profile { ProfileID = UserId, UserName = UserId, SeqNo = 1 });
            context.Users.Add(new InvolverUser { Id = OtherId, UserName = OtherId });
            context.Profiles.Add(new Profile { ProfileID = OtherId, UserName = OtherId, SeqNo = 2 });
            context.Articles.Add(new Article { ArticleID = 1, Title = "測試文章", Content = "內容", ProfileID = UserId });
            context.Comments.Add(new Comment { CommentID = 1, Content = "留言", ArticleID = 1, ProfileID = UserId });
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
                modelBuilder.Entity<Profile>().Property(p => p.SeqNo).ValueGeneratedNever();
                modelBuilder.Entity<ProfileAchievement>().Property(p => p.SeqNo).ValueGeneratedNever();
                modelBuilder.Entity<ProfileAchievement>().HasIndex(p => p.SeqNo).IsUnique(false);
                modelBuilder.Entity<Article>().Property(a => a.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            }
        }

        [Test]
        public async Task SaveChanges_WhenCommentHasNoParent_ThrowsDbUpdateException()
        {
            using var context = CreateContext();
            context.Comments.Add(new Comment { Content = "沒有父表", ProfileID = UserId });

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        [Test]
        public async Task SaveChanges_WhenAgreeHasBothCommentAndMessage_ThrowsDbUpdateException()
        {
            using var context = CreateContext();
            context.Messages.Add(new Message { MessageID = 1, Content = "回覆", CommentID = 1, ProfileID = UserId });
            context.SaveChanges();
            context.Agrees.Add(new Agree { CommentID = 1, MessageID = 1, ProfileID = OtherId });

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }

        [Test]
        public async Task SaveChanges_WhenSameUserAgreesCommentTwice_ThrowsDbUpdateException()
        {
            using (var context = CreateContext())
            {
                context.Agrees.Add(new Agree { CommentID = 1, ProfileID = OtherId });
                await context.SaveChangesAsync();
            }

            using (var context = CreateContext())
            {
                context.Agrees.Add(new Agree { CommentID = 1, ProfileID = OtherId });
                await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            }
        }

        [Test]
        public async Task SaveChanges_WhenSameUserFollowsAuthorTwice_ThrowsDbUpdateException()
        {
            using (var context = CreateContext())
            {
                context.Follows.Add(new Follow { ProfileID = UserId, FollowerID = OtherId });
                await context.SaveChangesAsync();
            }

            using (var context = CreateContext())
            {
                context.Follows.Add(new Follow { ProfileID = UserId, FollowerID = OtherId });
                await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            }
        }

        [Test]
        public async Task SaveChanges_WhenDifferentUsersFollowSameAuthor_Succeeds()
        {
            using var context = CreateContext();
            context.Follows.Add(new Follow { ProfileID = UserId, FollowerID = OtherId });
            context.Follows.Add(new Follow { ProfileID = OtherId, FollowerID = UserId });

            await context.SaveChangesAsync();

            Assert.That(await context.Follows.CountAsync(), Is.EqualTo(2));
        }

        [Test]
        public async Task SaveChanges_WhenTagNormalizedNameDuplicated_ThrowsDbUpdateException()
        {
            using (var context = CreateContext())
            {
                context.Tags.Add(new Tag { Name = "Fate", NormalizedName = "fate" });
                await context.SaveChangesAsync();
            }

            using (var context = CreateContext())
            {
                context.Tags.Add(new Tag { Name = "ＦＡＴＥ", NormalizedName = "fate" });
                await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
            }
        }

        [Test]
        public async Task SaveChanges_WhenNovelAndArticleShareTag_Succeeds()
        {
            using var context = CreateContext();
            var tag = new Tag { Name = "奇幻", NormalizedName = "奇幻" };
            context.Tags.Add(tag);
            context.Novels.Add(new DataAccess.Models.NovelModel.Novel { Title = "小說", Introduction = "介紹", ProfileID = UserId, Tags = [tag] });
            var article = await context.Articles.SingleAsync(a => a.ArticleID == 1);
            article.Tags = [tag];

            await context.SaveChangesAsync();

            var saved = await context.Tags
                .Where(t => t.NormalizedName == "奇幻")
                .Select(t => new { Novels = t.Novels!.Count, Articles = t.Articles!.Count })
                .SingleAsync();
            Assert.Multiple(() =>
            {
                Assert.That(saved.Novels, Is.EqualTo(1));
                Assert.That(saved.Articles, Is.EqualTo(1));
            });
        }

        [Test]
        public async Task SaveChanges_WhenUserNameAlreadyTaken_ThrowsDbUpdateException()
        {
            using var context = CreateContext();
            context.Users.Add(new InvolverUser { Id = "third", UserName = "third" });
            context.Profiles.Add(new Profile { ProfileID = "third", UserName = UserId, SeqNo = 3 });

            await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
    }
}
