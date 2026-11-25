using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;
using DataAccess.Models.Recommendation;
using DataAccess.Services.Recommendations;

using Involver.Models.ViewModels.Profiles;
using Involver.Services;
using Involver.Services.Profiles;
using Involver.Services.Recommendations;

using InvolverTest.Infrastructure;

using Microsoft.Extensions.Caching.Memory;

using NUnit.Framework;

namespace InvolverTest.Services
{
    /// <summary>
    /// 個人檔案的書架：閱讀進度、下一集、分組排序
    /// </summary>
    [TestFixture]
    public class ProfileReadingServiceTests
    {
        private const string Reader = "reader";
        private const string Author = "author";

        private SqliteTestDatabase _database = null!;

        [SetUp]
        public void SetUp()
        {
            var now = DateTime.Now;
            _database = new SqliteTestDatabase();
            using var context = _database.CreateContext();

            AddUser(context, Author, 1);
            AddUser(context, Reader, 2);

            // 1：讀完第一集 → 下一集是第二集
            AddNovel(context, 1, now, 11, 12, 13);
            // 2：第一集讀到一半 → 回到第一集
            AddNovel(context, 2, now, 21, 22);
            // 3：還沒讀
            AddNovel(context, 3, now, 31);
            // 4：全部讀完
            AddNovel(context, 4, now, 41, 42);
            // 5：跳著讀，最後讀的是最新一集 → 回頭找漏掉的第一集
            AddNovel(context, 5, now, 51, 52, 53);
            // 6：已刪除；7：沒有追蹤；8：還沒有章節
            AddNovel(context, 6, now, 61);
            AddNovel(context, 7, now, 71);
            AddNovel(context, 8, now);
            context.SaveChanges();

            context.Novels.Find(6)!.IsDeleted = true;
            foreach (var novelId in new[] { 1, 2, 3, 4, 5, 6, 8 })
            {
                context.Follows.Add(new Follow { FollowerID = Reader, NovelID = novelId, UpdateTime = now.AddDays(-10) });
            }

            AddRead(context, 11, now.AddHours(-5), reachedEnd: true);
            AddRead(context, 21, now.AddHours(-1), reachedEnd: false);
            AddRead(context, 41, now.AddHours(-9), reachedEnd: true);
            AddRead(context, 42, now.AddHours(-8), reachedEnd: true);
            AddRead(context, 52, now.AddHours(-4), reachedEnd: true);
            AddRead(context, 53, now.AddHours(-3), reachedEnd: true);
            context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _database.Dispose();
        }

        [Test]
        public async Task GetBookshelfAsync_WithReadingProgress_PointsToNextEpisode()
        {
            await using var context = _database.CreateContext();
            var service = CreateService(context);

            var shelf = await service.GetBookshelfAsync(Reader);

            var next = shelf.ToDictionary(b => b.Card.ContentId, b => b.Card.EpisodeId);
            Assert.That(next, Is.EqualTo(new Dictionary<int, int?>
            {
                [1] = 12,
                [2] = 21,
                [3] = 31,
                [4] = null,
                [5] = 51,
                [8] = null,
            }));
        }

        [Test]
        public async Task GetBookshelfAsync_WithMixedProgress_GroupsReadingThenNotStartedThenFinished()
        {
            await using var context = _database.CreateContext();
            var service = CreateService(context);

            var shelf = await service.GetBookshelfAsync(Reader);

            Assert.Multiple(() =>
            {
                Assert.That(shelf.Select(b => b.Status), Is.Ordered);
                Assert.That(shelf.Single(b => b.Card.ContentId == 1).Status, Is.EqualTo(BookshelfStatus.Reading));
                Assert.That(shelf.Single(b => b.Card.ContentId == 3).Status, Is.EqualTo(BookshelfStatus.NotStarted));
                Assert.That(shelf.Single(b => b.Card.ContentId == 4).Status, Is.EqualTo(BookshelfStatus.Finished));
                Assert.That(shelf.Single(b => b.Card.ContentId == 1).Card.Badge, Is.EqualTo("2 集未讀"));
                Assert.That(shelf.Single(b => b.Card.ContentId == 4).Card.Badge, Is.EqualTo("已讀完"));
                Assert.That(shelf.Single(b => b.Card.ContentId == 8).Status, Is.EqualTo(BookshelfStatus.NotStarted));
                Assert.That(shelf.Single(b => b.Card.ContentId == 8).Card.Badge, Is.EqualTo("尚無章節"));
            });
        }

        [Test]
        public async Task GetBookshelfAsync_DeletedOrNotFollowed_IsExcluded()
        {
            await using var context = _database.CreateContext();
            var service = CreateService(context);

            var shelf = await service.GetBookshelfAsync(Reader);

            Assert.That(shelf.Select(b => b.Card.ContentId), Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 8 }));
        }

        [Test]
        public async Task GetBookshelfAsync_WhenNothingFollowed_ReturnsEmpty()
        {
            await using var context = _database.CreateContext();
            var service = CreateService(context);

            var shelf = await service.GetBookshelfAsync(Author);

            Assert.That(shelf, Is.Empty);
        }

        [Test]
        public async Task GetWorksAsync_WithDeletedNovel_ReturnsOnlyVisibleWorks()
        {
            await using var context = _database.CreateContext();
            var service = CreateService(context);

            var works = await service.GetWorksAsync(Author, RecommendationContentType.Novel, 1);

            Assert.That(works.Select(c => c.ContentId), Is.EquivalentTo(new[] { 1, 2, 3, 4, 5, 7, 8 }));
        }

        private static ProfileReadingService CreateService(ApplicationDbContext context)
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            var homeFeed = new HomeFeedService(context, cache, new UnusedSettingsService(), new CategoryService(context, cache));
            return new ProfileReadingService(context, homeFeed);
        }

        private static void AddUser(ApplicationDbContext context, string id, int seqNo)
        {
            context.Users.Add(new InvolverUser { Id = id, UserName = id });
            context.Profiles.Add(new Profile
            {
                ProfileID = id,
                UserName = id,
                SeqNo = seqNo,
                EnrollmentDate = DateTime.Now.AddYears(-1),
                LastTimeLogin = DateTime.Now,
            });
        }

        private static void AddNovel(ApplicationDbContext context, int id, DateTime now, params int[] episodeIds)
        {
            context.Novels.Add(new Novel
            {
                NovelID = id,
                Title = $"作品{id}",
                Introduction = "介紹介紹介紹",
                ProfileID = Author,
                CreateTime = now.AddDays(-30),
                UpdateTime = now.AddDays(-id),
            });
            foreach (var episodeId in episodeIds)
            {
                context.Episodes.Add(new Episode
                {
                    EpisodeID = episodeId,
                    Title = $"第 {episodeId} 集",
                    Content = "內容",
                    NovelID = id,
                    OwnerID = Author,
                    UpdateTime = now.AddDays(-20).AddMinutes(episodeId),
                });
            }
        }

        private static void AddRead(ApplicationDbContext context, int episodeId, DateTime readAt, bool reachedEnd)
        {
            context.EpisodeReads.Add(new EpisodeRead
            {
                ProfileId = Reader,
                EpisodeId = episodeId,
                FirstReadAt = readAt,
                LastReadAt = readAt,
                ReachedEnd = reachedEnd,
                ReachedEndAt = reachedEnd ? readAt : null,
            });
        }

        /// <summary>
        /// 書架不會用到推薦設定
        /// </summary>
        private sealed class UnusedSettingsService : IRecommendationSettingsService
        {
            public Task<ActiveRecommendationSettings> GetActiveAsync(CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();

            public Task<List<RecommendationSettingVersion>> GetVersionsAsync(CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();

            public Task<int> SaveAsync(RecommendationSettings settings, string note, string actorId, CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();

            public Task<bool> ActivateAsync(int version, CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();

            public Task<int?> RollbackAsync(CancellationToken cancellationToken = default) =>
                throw new NotSupportedException();
        }
    }
}
