using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;
using DataAccess.Services.Recommendations;

using Involver.Services;
using Involver.Services.Recommendations;

using InvolverTest.Infrastructure;

using Microsoft.Extensions.Caching.Memory;

using NUnit.Framework;

namespace InvolverTest.Services
{
    /// <summary>
    /// Issue #238：熱門排行的防灌（新帳號、Banned 不計）
    /// </summary>
    [TestFixture]
    public class HomeFeedServiceTests
    {
        private SqliteTestDatabase _database = null!;

        [SetUp]
        public void SetUp()
        {
            var now = DateTime.Now;
            _database = new SqliteTestDatabase();
            using var context = _database.CreateContext();

            AddUser(context, "author", 1, now.AddYears(-1));
            AddUser(context, "banned", 2, now.AddYears(-1), banned: true);
            for (var i = 1; i <= 3; i++)
            {
                AddUser(context, $"old{i}", 10 + i, now.AddYears(-1));
            }

            for (var i = 1; i <= 5; i++)
            {
                AddUser(context, $"new{i}", 20 + i, now.AddDays(-1));
            }

            AddNovel(context, 1, "真的熱門", now);
            AddNovel(context, 2, "被灌票", now);
            context.SaveChanges();

            // 《真的熱門》：3 位舊帳號；《被灌票》：1 位舊帳號 + 5 個新帳號 + 1 個 Banned
            var views = new List<View>();
            views.AddRange(Enumerable.Range(1, 3).Select(i => new View { NovelId = 1, UserId = $"old{i}", CreateTime = now.AddHours(-i) }));
            views.Add(new View { NovelId = 2, UserId = "old1", CreateTime = now.AddHours(-1) });
            views.AddRange(Enumerable.Range(1, 5).Select(i => new View { NovelId = 2, UserId = $"new{i}", CreateTime = now.AddHours(-i) }));
            views.Add(new View { NovelId = 2, UserId = "banned", CreateTime = now.AddHours(-1) });
            context.Views.AddRange(views);
            context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _database.Dispose();
        }

        [Test]
        public async Task GetTrendingAsync_NewAndBannedAccounts_AreNotCounted()
        {
            await using var context = _database.CreateContext();
            var service = CreateService(context, new RecommendationSettings());

            var trending = await service.GetTrendingAsync();

            Assert.That(trending.Select(c => c.ContentId).Take(2), Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public async Task GetTrendingAsync_MinAgeZero_CountsNewAccounts()
        {
            await using var context = _database.CreateContext();
            var service = CreateService(context, new RecommendationSettings { TrustMinAccountAgeDays = 0 });

            var trending = await service.GetTrendingAsync();

            Assert.That(trending.Select(c => c.ContentId).Take(2), Is.EqualTo(new[] { 2, 1 }));
        }

        private static HomeFeedService CreateService(ApplicationDbContext context, RecommendationSettings settings)
        {
            var cache = new MemoryCache(new MemoryCacheOptions());
            return new(context, cache, new FixedSettingsService(settings), new CategoryService(context, cache));
        }

        private static void AddUser(ApplicationDbContext context, string id, int seqNo, DateTime enrolled, bool banned = false)
        {
            context.Users.Add(new InvolverUser { Id = id, UserName = id });
            context.Profiles.Add(new Profile
            {
                ProfileID = id,
                UserName = id,
                SeqNo = seqNo,
                EnrollmentDate = enrolled,
                LastTimeLogin = enrolled,
                Banned = banned,
            });
        }

        private static void AddNovel(ApplicationDbContext context, int id, string title, DateTime now)
        {
            context.Novels.Add(new Novel
            {
                NovelID = id,
                Title = title,
                Introduction = "介紹介紹介紹",
                ProfileID = "author",
                CreateTime = now.AddDays(-30),
                UpdateTime = now.AddDays(-1),
            });
            context.Episodes.Add(new Episode
            {
                EpisodeID = id,
                Title = "第一章",
                Content = "內容",
                NovelID = id,
                OwnerID = "author",
                UpdateTime = now.AddDays(-1),
            });
        }

        /// <summary>
        /// 固定回傳指定設定的設定服務
        /// </summary>
        private sealed class FixedSettingsService(RecommendationSettings settings) : IRecommendationSettingsService
        {
            public Task<ActiveRecommendationSettings> GetActiveAsync(CancellationToken cancellationToken = default) =>
                Task.FromResult(new ActiveRecommendationSettings(settings, null));

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
