using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.AchievementModel;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;

using Involver.Authorization.Novel;
using Involver.Services;

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

namespace InvolverTest.Services
{
    /// <summary>
    /// Issue #234：標籤服務（統一格式、防重複、數量上限、待審與自動轉正、建議清單）
    /// </summary>
    [TestFixture]
    public class TagServiceTests
    {
        private const string AuthorA = "author-a";
        private const string AuthorB = "author-b";
        private const string AuthorC = "author-c";
        private const string BannedId = "banned";
        private const string ProfessionalId = "professional";

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

            var seq = 1;
            foreach (var id in new[] { AuthorA, AuthorB, AuthorC, BannedId, ProfessionalId })
            {
                context.Users.Add(new InvolverUser { Id = id, UserName = id });
                context.Profiles.Add(new Profile
                {
                    ProfileID = id,
                    UserName = id,
                    SeqNo = seq++,
                    Banned = id == BannedId,
                    Professional = id == ProfessionalId,
                });
            }

            context.Tags.AddRange(
                new Tag { TagId = 1, Name = "Fate", NormalizedName = "fate", Status = TagStatus.Approved },
                new Tag { TagId = 2, Name = "讀書心得", NormalizedName = "讀書心得", Status = TagStatus.Approved },
                new Tag { TagId = 3, Name = "奇幻", NormalizedName = "奇幻", Status = TagStatus.Approved },
                new Tag { TagId = 4, Name = "待審標籤", NormalizedName = "待審標籤", Status = TagStatus.Pending, CreatorProfileID = AuthorA });

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

        private static string Tagify(params string[] names) =>
            System.Text.Json.JsonSerializer.Serialize(names.Select(n => new { value = n }));

        private static ClaimsPrincipal UserInRole(string role) =>
            new(new ClaimsIdentity([new Claim(ClaimTypes.Role, role)], "test"));

        private static int _novelId = 100;

        private static Novel NewNovel(string profileId, IEnumerable<Tag> tags, bool deleted = false) => new()
        {
            NovelID = ++_novelId,
            Title = $"小說{_novelId}",
            Introduction = "介紹",
            ProfileID = profileId,
            IsDeleted = deleted,
            Tags = tags.ToList(),
        };

        [Test]
        public void ParseTagifyValue_WithTagifyJson_ReturnsNames()
        {
            var names = TagService.ParseTagifyValue("[{\"value\":\" 奇幻 \"},{\"value\":\"Fate\"},{\"value\":\"\"}]");

            Assert.That(names, Is.EqualTo(new[] { "奇幻", "Fate" }));
        }

        [Test]
        public void ParseTagifyValue_WithCommaSeparated_ReturnsNames()
        {
            var names = TagService.ParseTagifyValue("奇幻, Fate，科幻");

            Assert.That(names, Is.EqualTo(new[] { "奇幻", "Fate", "科幻" }));
        }

        [Test]
        public async Task ResolveTagsAsync_WithFullWidthName_ReusesExistingTag()
        {
            using var context = CreateContext();
            var service = new TagService(context);

            var result = await service.ResolveTagsAsync(Tagify("ＦＡＴＥ"), TagTarget.Novel, AuthorA, TagAuthorLevel.Author);

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.Tags.Select(t => t.TagId), Is.EqualTo(new[] { 1 }));
                Assert.That(result.CreatedTags, Is.Empty);
            });
        }

        [Test]
        public async Task ResolveTagsAsync_WhenInputHasSameNormalizedName_KeepsOne()
        {
            using var context = CreateContext();
            var service = new TagService(context);

            var result = await service.ResolveTagsAsync(Tagify("奇幻", "奇 幻", "奇幻！"), TagTarget.Novel, AuthorA, TagAuthorLevel.Author);

            Assert.That(result.Tags.Select(t => t.TagId), Is.EqualTo(new[] { 3 }));
        }

        [TestCase("讀書心得1")]
        [TestCase("讀書心得 02")]
        [TestCase("讀書心得#3")]
        public async Task ResolveTagsAsync_WhenOnlyTrailingNumberDiffers_Fails(string name)
        {
            using var context = CreateContext();
            var service = new TagService(context);

            var result = await service.ResolveTagsAsync(Tagify(name), TagTarget.Article, ProfessionalId, TagAuthorLevel.Trusted);

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.False);
                Assert.That(result.ErrorMessage, Does.Contain("讀書心得"));
            });
        }

        [Test]
        public async Task ResolveTagsAsync_WhenNewTagsInSameRequestDifferByNumber_Fails()
        {
            using var context = CreateContext();
            var service = new TagService(context);

            var result = await service.ResolveTagsAsync(Tagify("遊戲心得1", "遊戲心得2"), TagTarget.Article, ProfessionalId, TagAuthorLevel.Trusted);

            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public async Task ResolveTagsAsync_WhenAuthorCreatesNewTag_CreatesPendingTag()
        {
            using (var context = CreateContext())
            {
                var service = new TagService(context);
                var result = await service.ResolveTagsAsync(Tagify("太空歌劇"), TagTarget.Novel, AuthorB, TagAuthorLevel.Author);
                Assert.That(result.Succeeded, Is.True);
                await context.SaveChangesAsync();
            }

            using (var context = CreateContext())
            {
                var tag = await context.Tags.SingleAsync(t => t.NormalizedName == "太空歌劇");
                Assert.Multiple(() =>
                {
                    Assert.That(tag.Status, Is.EqualTo(TagStatus.Pending));
                    Assert.That(tag.CreatorProfileID, Is.EqualTo(AuthorB));
                });
            }
        }

        [Test]
        public async Task ResolveTagsAsync_WhenTrustedCreatesNewTag_CreatesApprovedTag()
        {
            using var context = CreateContext();
            var service = new TagService(context);

            var result = await service.ResolveTagsAsync(Tagify("Type-Moon"), TagTarget.Novel, ProfessionalId, TagAuthorLevel.Trusted);

            Assert.Multiple(() =>
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.CreatedTags.Single().Status, Is.EqualTo(TagStatus.Approved));
                Assert.That(result.CreatedTags.Single().Name, Is.EqualTo("Type-Moon"));
                Assert.That(result.CreatedTags.Single().NormalizedName, Is.EqualTo("typemoon"));
            });
        }

        [Test]
        public async Task ResolveTagsAsync_WhenBannedUsesExistingTag_Fails()
        {
            using var context = CreateContext();
            var service = new TagService(context);

            var result = await service.ResolveTagsAsync(Tagify("奇幻"), TagTarget.Novel, BannedId, TagAuthorLevel.Banned);

            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public async Task ResolveTagsAsync_WhenBannedKeepsCurrentTags_Succeeds()
        {
            using var context = CreateContext();
            var service = new TagService(context);

            var result = await service.ResolveTagsAsync(Tagify("奇幻"), TagTarget.Novel, BannedId, TagAuthorLevel.Banned, [3]);

            Assert.That(result.Succeeded, Is.True);
        }

        [Test]
        public async Task ResolveTagsAsync_WhenBannedCreatesTag_Fails()
        {
            using var context = CreateContext();
            var service = new TagService(context);

            var result = await service.ResolveTagsAsync(Tagify("新標籤"), TagTarget.Novel, BannedId, TagAuthorLevel.Banned);

            Assert.That(result.Succeeded, Is.False);
        }

        [TestCase(TagTarget.Novel, 8, true)]
        [TestCase(TagTarget.Novel, 9, false)]
        [TestCase(TagTarget.Article, 5, true)]
        [TestCase(TagTarget.Article, 6, false)]
        public async Task ResolveTagsAsync_WithTagCount_RespectsLimitByTarget(TagTarget target, int count, bool expected)
        {
            using var context = CreateContext();
            var service = new TagService(context);
            var names = Enumerable.Range(0, count).Select(i => $"標籤{(char)('甲' + i)}").ToArray();

            var result = await service.ResolveTagsAsync(Tagify(names), target, ProfessionalId, TagAuthorLevel.Trusted);

            Assert.That(result.Succeeded, Is.EqualTo(expected), result.ErrorMessage);
        }

        [Test]
        public async Task ResolveTagsAsync_WhenNameTooLong_Fails()
        {
            using var context = CreateContext();
            var service = new TagService(context);

            var result = await service.ResolveTagsAsync(Tagify(new string('長', Parameters.TagNameMaxLength + 1)), TagTarget.Novel, ProfessionalId, TagAuthorLevel.Trusted);

            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public async Task ResolveTagsAsync_WhenDailyLimitExceeded_Fails()
        {
            using (var context = CreateContext())
            {
                for (var i = 0; i < Parameters.TagDailyCreateLimit; i++)
                {
                    context.Tags.Add(new Tag
                    {
                        Name = $"今天{(char)('甲' + i)}",
                        NormalizedName = $"今天{(char)('甲' + i)}",
                        Status = TagStatus.Pending,
                        CreatorProfileID = AuthorC,
                        CreateTime = System.DateTime.Now,
                    });
                }

                await context.SaveChangesAsync();
            }

            using (var context = CreateContext())
            {
                var service = new TagService(context);
                var result = await service.ResolveTagsAsync(Tagify("再一個"), TagTarget.Novel, AuthorC, TagAuthorLevel.Author);

                Assert.That(result.Succeeded, Is.False);
            }
        }

        [TestCase(3, TagStatus.Approved)]
        [TestCase(2, TagStatus.Pending)]
        public async Task PromotePendingTagsAsync_ByDistinctAuthorCount_ApprovesWhenReachingThreshold(int authorCount, TagStatus expected)
        {
            using (var context = CreateContext())
            {
                var pending = await context.Tags.SingleAsync(t => t.TagId == 4);
                var authors = new[] { AuthorA, AuthorB, AuthorC }.Take(authorCount);
                foreach (var author in authors)
                {
                    context.Novels.Add(NewNovel(author, [pending]));
                }

                // 同一位作者的第二部作品、軟刪除的作品都不算
                context.Novels.Add(NewNovel(AuthorA, [pending]));
                context.Novels.Add(NewNovel(ProfessionalId, [pending], deleted: true));
                await context.SaveChangesAsync();

                var service = new TagService(context);
                await service.PromotePendingTagsAsync([4]);
            }

            using (var context = CreateContext())
            {
                var tag = await context.Tags.AsNoTracking().SingleAsync(t => t.TagId == 4);
                Assert.That(tag.Status, Is.EqualTo(expected));
            }
        }

        [Test]
        public async Task PromotePendingTagsAsync_WhenAuthorsUseNovelAndArticle_CountsBoth()
        {
            using (var context = CreateContext())
            {
                var pending = await context.Tags.SingleAsync(t => t.TagId == 4);
                context.Novels.Add(NewNovel(AuthorA, [pending]));
                context.Novels.Add(NewNovel(AuthorB, [pending]));
                context.Articles.Add(new Article { Title = "文章", Content = "內容", ProfileID = AuthorC, Tags = [pending] });
                await context.SaveChangesAsync();

                await new TagService(context).PromotePendingTagsAsync([4]);
            }

            using (var context = CreateContext())
            {
                var tag = await context.Tags.AsNoTracking().SingleAsync(t => t.TagId == 4);
                Assert.That(tag.Status, Is.EqualTo(TagStatus.Approved));
            }
        }

        [Test]
        public async Task GetSuggestionsAsync_ForNovel_ExcludesPendingAndOrdersByNovelUsage()
        {
            using (var context = CreateContext())
            {
                var tags = await context.Tags.ToDictionaryAsync(t => t.TagId);
                context.Novels.Add(NewNovel(AuthorA, [tags[3]]));
                context.Novels.Add(NewNovel(AuthorB, [tags[3], tags[4]]));
                context.Novels.Add(NewNovel(AuthorC, [tags[1]]));
                // 軟刪除的作品不計
                context.Novels.Add(NewNovel(AuthorC, [tags[1]], deleted: true));
                context.Novels.Add(NewNovel(AuthorC, [tags[1]], deleted: true));
                // 讀書心得只有文章用過，排在小說用過的後面
                context.Articles.Add(new Article { Title = "文章", Content = "內容", ProfileID = AuthorA, Tags = [tags[2]] });
                context.Articles.Add(new Article { Title = "文章2", Content = "內容", ProfileID = AuthorB, Tags = [tags[2]] });
                context.Articles.Add(new Article { Title = "文章3", Content = "內容", ProfileID = AuthorC, Tags = [tags[2]] });
                await context.SaveChangesAsync();
            }

            using (var context = CreateContext())
            {
                var suggestions = await new TagService(context).GetSuggestionsAsync(TagTarget.Novel);

                Assert.That(suggestions.Select(s => s.Name), Is.EqualTo(new[] { "奇幻", "Fate", "讀書心得" }));
            }
        }

        [Test]
        public async Task FindSimilarAsync_WithTrailingNumber_ReturnsDuplicate()
        {
            using var context = CreateContext();

            var result = await new TagService(context).FindSimilarAsync("讀書心得1", TagTarget.Article);

            Assert.Multiple(() =>
            {
                Assert.That(result.Existing, Is.Null);
                Assert.That(result.Duplicate?.Name, Is.EqualTo("讀書心得"));
            });
        }

        [Test]
        public async Task FindSimilarAsync_WithSameNormalizedName_ReturnsExisting()
        {
            using var context = CreateContext();

            var result = await new TagService(context).FindSimilarAsync("ｆａｔｅ", TagTarget.Novel);

            Assert.That(result.Existing?.TagId, Is.EqualTo(1));
        }

        [Test]
        public async Task FindSimilarAsync_WithTypo_ReturnsSimilar()
        {
            using var context = CreateContext();

            var result = await new TagService(context).FindSimilarAsync("讀書心德", TagTarget.Article);

            Assert.Multiple(() =>
            {
                Assert.That(result.Existing, Is.Null);
                Assert.That(result.Duplicate, Is.Null);
                Assert.That(result.Similar.Select(s => s.Name), Does.Contain("讀書心得"));
            });
        }

        [Test]
        public async Task FindSimilarAsync_WhenOnlyPunctuation_ReturnsError()
        {
            using var context = CreateContext();

            var result = await new TagService(context).FindSimilarAsync("！！！", TagTarget.Novel);

            Assert.That(result.ErrorMessage, Is.Not.Null);
        }

        [Test]
        public async Task GetDuplicateReportAsync_WithNumberedTag_ListsDuplicatePair()
        {
            using (var context = CreateContext())
            {
                context.Tags.Add(new Tag { Name = "讀書心得2", NormalizedName = "讀書心得2" });
                // migration 用 SQL 算的值和程式算的不同時要列出來
                context.Tags.Add(new Tag { Name = "Ｘ－Ｍｅｎ", NormalizedName = "x－men" });
                await context.SaveChangesAsync();
            }

            using (var context = CreateContext())
            {
                var rows = await new TagService(context).GetDuplicateReportAsync();

                Assert.Multiple(() =>
                {
                    Assert.That(rows.Any(r => r.Kind == "Duplicate"
                        && new[] { r.TagA.Name, r.TagB!.Name }.OrderBy(n => n).SequenceEqual(new[] { "讀書心得", "讀書心得2" })), Is.True);
                    Assert.That(rows.Any(r => r.Kind == "NormalizedNameMismatch" && r.TagA.Name == "Ｘ－Ｍｅｎ" && r.Note == "xmen"), Is.True);
                });
            }
        }

        [TestCase(BannedId, null, TagAuthorLevel.Banned)]
        [TestCase(ProfessionalId, null, TagAuthorLevel.Trusted)]
        [TestCase(AuthorA, null, TagAuthorLevel.Author)]
        [TestCase("no-profile", null, TagAuthorLevel.Banned)]
        public async Task GetAuthorLevelAsync_ByProfile_ReturnsLevel(string profileId, string? role, TagAuthorLevel expected)
        {
            using var context = CreateContext();

            var level = await new TagService(context).GetAuthorLevelAsync(new ClaimsPrincipal(new ClaimsIdentity()), profileId);

            Assert.That(level, Is.EqualTo(expected));
        }

        [Test]
        public async Task GetAuthorLevelAsync_WhenTagManagerRole_ReturnsTrusted()
        {
            using var context = CreateContext();

            var level = await new TagService(context).GetAuthorLevelAsync(UserInRole(Novels.NovelAdministratorsRole), AuthorA);

            Assert.That(level, Is.EqualTo(TagAuthorLevel.Trusted));
        }

        [Test]
        public async Task UpdateStatusAsync_WhenTagMissing_ReturnsFalse()
        {
            using var context = CreateContext();

            var updated = await new TagService(context).UpdateStatusAsync(999, TagStatus.Approved);

            Assert.That(updated, Is.False);
        }
    }
}
