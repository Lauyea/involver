using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;

using Involver.Controllers;

using InvolverTest.Infrastructure;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NUnit.Framework;

namespace InvolverTest.Controllers
{
    /// <summary>
    /// FollowsApiController 的測試：PUT 追蹤、DELETE 取消，都可以重複呼叫
    /// </summary>
    [TestFixture]
    public class FollowsApiControllerTests
    {
        private const string ReaderId = "reader";
        private const string AuthorId = "author";
        private const int NovelId = 1;

        private SqliteTestDatabase _database = null!;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();

            using var context = _database.CreateContext();
            context.Users.Add(new InvolverUser { Id = ReaderId, UserName = ReaderId });
            context.Users.Add(new InvolverUser { Id = AuthorId, UserName = AuthorId });
            context.Profiles.Add(new Profile { ProfileID = ReaderId, UserName = ReaderId, SeqNo = 1 });
            context.Profiles.Add(new Profile { ProfileID = AuthorId, UserName = AuthorId, SeqNo = 2 });
            context.Novels.Add(new Novel { NovelID = NovelId, Title = "測試創作", Introduction = "介紹", ProfileID = AuthorId });
            context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _database.Dispose();
        }

        /// <summary>
        /// 建立以指定使用者登入的 controller；userId 為 null 時是未登入
        /// </summary>
        private static FollowsApiController CreateController(ApplicationDbContext context, string? userId)
        {
            var userManager = new UserManager<InvolverUser>(
                new UserStore<InvolverUser>(context),
                Options.Create(new IdentityOptions()),
                new PasswordHasher<InvolverUser>(),
                [],
                [],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null!,
                NullLogger<UserManager<InvolverUser>>.Instance);

            var identity = userId == null
                ? new ClaimsIdentity()
                : new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "Test");

            return new FollowsApiController(context, userManager)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        /// <summary>
        /// 取出回應中的 followed
        /// </summary>
        private static bool GetFollowed(IActionResult result)
        {
            var ok = result as OkObjectResult;
            Assert.That(ok, Is.Not.Null, "應回傳 200");
            return (bool)ok!.Value!.GetType().GetProperty("followed")!.GetValue(ok.Value)!;
        }

        private async Task<int> CountFollowsAsync(Func<IQueryable<Follow>, IQueryable<Follow>> filter)
        {
            await using var context = _database.CreateContext();
            return await filter(context.Follows.Where(f => f.FollowerID == ReaderId)).CountAsync();
        }

        [Test]
        public async Task FollowAuthorAsync_WhenNotFollowed_CreatesFollow()
        {
            await using (var context = _database.CreateContext())
            {
                var result = await CreateController(context, ReaderId).FollowAuthorAsync(AuthorId, CancellationToken.None);
                Assert.That(GetFollowed(result), Is.True);
            }

            Assert.That(await CountFollowsAsync(q => q.Where(f => f.ProfileID == AuthorId)), Is.EqualTo(1));
        }

        [Test]
        public async Task FollowAuthorAsync_WhenAlreadyFollowed_KeepsSingleFollow()
        {
            await using (var context = _database.CreateContext())
            {
                await CreateController(context, ReaderId).FollowAuthorAsync(AuthorId, CancellationToken.None);
            }

            await using (var context = _database.CreateContext())
            {
                var result = await CreateController(context, ReaderId).FollowAuthorAsync(AuthorId, CancellationToken.None);
                Assert.That(GetFollowed(result), Is.True);
            }

            Assert.That(await CountFollowsAsync(q => q.Where(f => f.ProfileID == AuthorId)), Is.EqualTo(1));
        }

        [Test]
        public async Task FollowAuthorAsync_WhenAuthorMissing_ReturnsNotFound()
        {
            await using var context = _database.CreateContext();

            var result = await CreateController(context, ReaderId).FollowAuthorAsync("nobody", CancellationToken.None);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task FollowAuthorAsync_WhenSignedOut_ReturnsUnauthorized()
        {
            await using var context = _database.CreateContext();

            var result = await CreateController(context, null).FollowAuthorAsync(AuthorId, CancellationToken.None);

            Assert.That(result, Is.InstanceOf<UnauthorizedResult>());
        }

        [Test]
        public async Task UnfollowAuthorAsync_WhenFollowed_RemovesFollow()
        {
            await using (var context = _database.CreateContext())
            {
                await CreateController(context, ReaderId).FollowAuthorAsync(AuthorId, CancellationToken.None);
            }

            await using (var context = _database.CreateContext())
            {
                var result = await CreateController(context, ReaderId).UnfollowAuthorAsync(AuthorId, CancellationToken.None);
                Assert.That(GetFollowed(result), Is.False);
            }

            Assert.That(await CountFollowsAsync(q => q.Where(f => f.ProfileID == AuthorId)), Is.Zero);
        }

        [Test]
        public async Task UnfollowAuthorAsync_WhenNotFollowed_ReturnsNotFollowed()
        {
            await using var context = _database.CreateContext();

            var result = await CreateController(context, ReaderId).UnfollowAuthorAsync(AuthorId, CancellationToken.None);

            Assert.That(GetFollowed(result), Is.False);
        }

        [Test]
        public async Task FollowNovelAsync_WhenNotFollowed_CreatesFollow()
        {
            await using (var context = _database.CreateContext())
            {
                var result = await CreateController(context, ReaderId).FollowNovelAsync(NovelId, CancellationToken.None);
                Assert.That(GetFollowed(result), Is.True);
            }

            Assert.That(await CountFollowsAsync(q => q.Where(f => f.NovelID == NovelId)), Is.EqualTo(1));
        }

        [Test]
        public async Task FollowNovelAsync_WhenNovelMissing_ReturnsNotFound()
        {
            await using var context = _database.CreateContext();

            var result = await CreateController(context, ReaderId).FollowNovelAsync(999, CancellationToken.None);

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task UnfollowNovelAsync_WhenFollowed_RemovesFollow()
        {
            await using (var context = _database.CreateContext())
            {
                await CreateController(context, ReaderId).FollowNovelAsync(NovelId, CancellationToken.None);
            }

            await using (var context = _database.CreateContext())
            {
                var result = await CreateController(context, ReaderId).UnfollowNovelAsync(NovelId, CancellationToken.None);
                Assert.That(GetFollowed(result), Is.False);
            }

            Assert.That(await CountFollowsAsync(q => q.Where(f => f.NovelID == NovelId)), Is.Zero);
        }
    }
}
