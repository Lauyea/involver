using System;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;
using DataAccess.Models.Recommendation;
using DataAccess.Services.Recommendations;

using InvolverTest.Infrastructure;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;


namespace InvolverTest.Services
{
    /// <summary>
    /// Issue #238：推薦排程讀取資料庫、寫入 UserRecommendations
    /// </summary>
    [TestFixture]
    public class RecommendationEngineTests
    {
        private static readonly DateTime Now = new(2026, 11, 19, 12, 0, 0);

        private SqliteTestDatabase _database = null!;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            using var context = _database.CreateContext();

            AddUser(context, "author", 1);
            AddUser(context, "reader", 2);
            AddUser(context, "fan", 3);
            AddUser(context, "inactive", 4, lastLogin: Now.AddYears(-1));

            AddNovel(context, 1, "已讀");
            AddNovel(context, 2, "推薦候選");
            AddNovel(context, 3, "已刪除", isDeleted: true);
            AddNovel(context, 4, "已封鎖", block: true);

            context.Set<NovelViewer>().AddRange(
                new NovelViewer { SeqNo = 1, ProfileID = "reader", NovelID = 1, ViewDate = Now.AddDays(-1) },
                new NovelViewer { SeqNo = 2, ProfileID = "fan", NovelID = 2, ViewDate = Now.AddDays(-1) },
                new NovelViewer { SeqNo = 3, ProfileID = "fan", NovelID = 3, ViewDate = Now.AddDays(-1) },
                new NovelViewer { SeqNo = 4, ProfileID = "fan", NovelID = 4, ViewDate = Now.AddDays(-1) });
            context.SaveChanges();

            context.EpisodeReads.Add(new EpisodeRead
            {
                ProfileId = "fan",
                EpisodeId = 2,
                FirstReadAt = Now.AddDays(-1),
                LastReadAt = Now.AddDays(-1),
                ReachedEnd = true,
                ReachedEndAt = Now.AddDays(-1),
            });

            // 舊的推薦會被整批取代
            context.UserRecommendations.Add(new UserRecommendation
            {
                ProfileId = "reader",
                ContentType = RecommendationContentType.Novel,
                ContentId = 999,
                Rank = 0,
                Reason = "舊資料",
                ComputedAt = Now.AddDays(-1),
            });
            context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _database.Dispose();
        }

        [Test]
        public async Task RunAsync_ExcludesDeletedBlockedAndSeen_ReplacesOldRows()
        {
            await using (var context = _database.CreateContext())
            {
                var result = await new RecommendationEngine(context).RunAsync(Now);
                Assert.That(result.SettingVersion, Is.Null);
            }

            await using var verify = _database.CreateContext();
            var rows = await verify.UserRecommendations.Where(r => r.ProfileId == "reader").OrderBy(r => r.Rank).ToListAsync();

            Assert.That(rows.Select(r => r.ContentId), Is.EqualTo(new[] { 2 }));
            Assert.That(rows[0].Reason, Is.Not.Empty);
            Assert.That(await verify.UserRecommendations.AnyAsync(r => r.ContentId == 999), Is.False);
            Assert.That(await verify.UserRecommendations.AnyAsync(r => r.ProfileId == "inactive"), Is.False);
        }

        [Test]
        public async Task RunAsync_WithActiveSetting_UsesItsVersion()
        {
            await using (var context = _database.CreateContext())
            {
                context.RecommendationSettings.Add(new RecommendationSetting
                {
                    Version = 3,
                    SettingsJson = new RecommendationSettings { ResultCount = 1 }.ToJson(),
                    IsActive = true,
                    CreatedBy = "author",
                    CreatedAt = DateTime.UtcNow,
                });
                await context.SaveChangesAsync();

                var result = await new RecommendationEngine(context).RunAsync(Now);
                Assert.That(result.SettingVersion, Is.EqualTo(3));
            }

            await using var verify = _database.CreateContext();
            Assert.That(await verify.UserRecommendations.Select(r => r.SettingVersion).Distinct().ToListAsync(), Is.EqualTo(new int?[] { 3 }));
        }

        [Test]
        public async Task LoadActiveSettingsAsync_WithInvalidJson_FallsBackToDefaults()
        {
            await using var context = _database.CreateContext();
            context.RecommendationSettings.Add(new RecommendationSetting
            {
                Version = 1,
                SettingsJson = "{broken",
                IsActive = true,
                CreatedBy = "author",
                CreatedAt = DateTime.UtcNow,
            });
            await context.SaveChangesAsync();

            var active = await RecommendationEngine.LoadActiveSettingsAsync(context);

            Assert.That(active.Version, Is.Null);
            Assert.That(active.Settings.ResultCount, Is.EqualTo(new RecommendationSettings().ResultCount));
        }

        [Test]
        public async Task LoadInputAsync_ReadsCategoriesAndPreferences()
        {
            await using (var context = _database.CreateContext())
            {
                var category = new Tag { TagId = 50, Name = "奇幻", NormalizedName = "奇幻", IsCategory = true, CategoryScope = CategoryScope.Novel, CategoryOrder = 1 };
                var tag = new Tag { TagId = 51, Name = "魔法", NormalizedName = "魔法" };
                context.Tags.AddRange(category, tag);
                var novel = await context.Novels.Include(n => n.Tags).SingleAsync(n => n.NovelID == 2);
                novel.Tags!.Add(category);
                novel.Tags.Add(tag);
                context.ProfileCategoryPreferences.Add(new ProfileCategoryPreference { ProfileId = "reader", TagId = 50, CreatedAt = DateTime.UtcNow });
                await context.SaveChangesAsync();
            }

            await using var verify = _database.CreateContext();
            var input = await new RecommendationEngine(verify).LoadInputAsync(Now);

            Assert.That(input.CategoryTagIds, Is.EquivalentTo(new[] { 50 }));
            Assert.That(input.Contents.Single(c => c.Key == ContentKey.Novel(2)).TagIds, Is.EquivalentTo(new[] { 50, 51 }));
            Assert.That(input.PreferredCategories, Is.EqualTo(new[] { ("reader", 50) }));
        }

        [Test]
        public async Task LoadInputAsync_ReadsCompletionFromEpisodeReads()
        {
            await using var context = _database.CreateContext();

            var input = await new RecommendationEngine(context).LoadInputAsync(Now);

            Assert.That(input.Contents.Select(c => c.Key), Is.EquivalentTo(new[] { ContentKey.Novel(1), ContentKey.Novel(2) }));
            Assert.That(input.Readings, Has.Exactly(1).Matches<ReadingRecord>(r => r!.NovelId == 2 && r.ReachedEnd));
        }

        private static void AddUser(ApplicationDbContext context, string id, int seqNo, DateTime? lastLogin = null)
        {
            context.Users.Add(new InvolverUser { Id = id, UserName = id });
            context.Profiles.Add(new Profile
            {
                ProfileID = id,
                UserName = id,
                SeqNo = seqNo,
                EnrollmentDate = Now.AddYears(-1),
                LastTimeLogin = lastLogin ?? Now.AddDays(-1),
            });
        }

        private static void AddNovel(ApplicationDbContext context, int id, string title, bool isDeleted = false, bool block = false)
        {
            context.Novels.Add(new Novel
            {
                NovelID = id,
                Title = title,
                Introduction = "介紹介紹介紹",
                ProfileID = "author",
                CreateTime = Now.AddDays(-60),
                UpdateTime = Now.AddDays(-1),
                IsDeleted = isDeleted,
                Block = block,
            });
            context.Episodes.Add(new Episode
            {
                EpisodeID = id,
                Title = "第一章",
                Content = "內容",
                NovelID = id,
                OwnerID = "author",
                UpdateTime = Now.AddDays(-1),
            });
        }
    }
}
