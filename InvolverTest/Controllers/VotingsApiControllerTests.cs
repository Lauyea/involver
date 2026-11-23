using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;

using Involver.Common;
using Involver.Controllers;
using Involver.Models.ViewModels;
using Involver.Services;

using InvolverTest.Infrastructure;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using NUnit.Framework;

namespace InvolverTest.Controllers
{
    /// <summary>
    /// VotingsApiController 的測試：免費投票（票價 0）、負數票價、章節沒有投票時的回應
    /// </summary>
    [TestFixture]
    public class VotingsApiControllerTests
    {
        private const string ReaderId = "reader";
        private const string AuthorId = "author";
        private const int EpisodeId = 1;
        private const int EpisodeWithoutVotingId = 2;
        private const int FreeVotingId = 1;
        private const int FreeOptionId = 1;

        private SqliteTestDatabase _database = null!;
        private ICoinService _coinService = null!;
        private IMissionService _missionService = null!;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            _coinService = Substitute.For<ICoinService>();
            _missionService = Substitute.For<IMissionService>();

            using var context = _database.CreateContext();
            context.Users.Add(new InvolverUser { Id = ReaderId, UserName = ReaderId });
            context.Users.Add(new InvolverUser { Id = AuthorId, UserName = AuthorId });
            context.Profiles.Add(new Profile { ProfileID = ReaderId, UserName = ReaderId, SeqNo = 1 });
            context.Profiles.Add(new Profile { ProfileID = AuthorId, UserName = AuthorId, SeqNo = 2 });
            context.Novels.Add(new Novel { NovelID = 1, Title = "測試創作", Introduction = "介紹", ProfileID = AuthorId });
            context.Episodes.Add(new Episode { EpisodeID = EpisodeId, Title = "第一章", Content = "內容", NovelID = 1, OwnerID = AuthorId });
            context.Episodes.Add(new Episode { EpisodeID = EpisodeWithoutVotingId, Title = "第二章", Content = "內容", NovelID = 1, OwnerID = AuthorId });
            context.Votings.Add(new Voting
            {
                VotingID = FreeVotingId,
                Title = "免費投票",
                Policy = PolicyType.Equality,
                Limit = LimitType.Time,
                Threshold = 0,
                DeadLine = DateTime.Now.AddDays(1),
                EpisodeID = EpisodeId,
                OwnerID = AuthorId,
            });
            context.NormalOptions.Add(new NormalOption { NormalOptionID = FreeOptionId, Content = "選項一", VotingID = FreeVotingId });
            context.NormalOptions.Add(new NormalOption { NormalOptionID = 2, Content = "選項二", VotingID = FreeVotingId });
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
        private VotingsApiController CreateController(ApplicationDbContext context, string? userId)
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

            var achievementService = Substitute.For<IAchievementService>();
            achievementService.VoteCountAsync(Arg.Any<string>()).Returns(new List<Toast>());

            var identity = userId == null
                ? new ClaimsIdentity()
                : new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "Test");

            return new VotingsApiController(context, userManager, achievementService, _coinService, _missionService)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
                }
            };
        }

        [Test]
        public async Task CastVote_WhenThresholdIsZero_RecordsVoteWithoutSpendingCoins()
        {
            await using var context = _database.CreateContext();
            var controller = CreateController(context, ReaderId);

            var result = await controller.CastVote(new CastVoteApiViewModel { VotingId = FreeVotingId, OptionId = FreeOptionId, Value = 0, IsVirtual = true });

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            await using var check = _database.CreateContext();
            var vote = await check.Votes.SingleAsync();
            Assert.That(vote.OwnerID, Is.EqualTo(ReaderId));
            Assert.That(vote.Value, Is.EqualTo(0));
            Assert.That((await check.Votings.SingleAsync()).TotalNumber, Is.EqualTo(1));
            await _coinService.DidNotReceiveWithAnyArgs().TrySpendAsync(default!, default, default!, default, default);
            await _coinService.DidNotReceiveWithAnyArgs().AddNovelCoinsAsync(default, default, default);
            await _missionService.DidNotReceiveWithAnyArgs().CompleteAsync(default!, default, default);
        }

        /// <summary>
        /// 依序投兩次會被前面的 alreadyVoted 擋下；併發時靠鎖定投票者資料列後的重新檢查，SQLite 無法重現 SQL Server 的資料列鎖定
        /// </summary>
        [Test]
        public async Task CastVote_WhenFreeVoteCastTwiceInSequence_RejectsSecondVote()
        {
            await using var context = _database.CreateContext();
            var controller = CreateController(context, ReaderId);
            var vote = new CastVoteApiViewModel { VotingId = FreeVotingId, OptionId = FreeOptionId, Value = 0, IsVirtual = true };

            await controller.CastVote(vote);
            var second = await controller.CastVote(vote);

            Assert.That(second, Is.InstanceOf<BadRequestObjectResult>());
            await using var check = _database.CreateContext();
            Assert.That(await check.Votes.CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task CastVote_WhenValueIsNegative_ReturnsBadRequest()
        {
            await using (var setup = _database.CreateContext())
            {
                var voting = await setup.Votings.SingleAsync();
                voting.Policy = PolicyType.Liberty;
                await setup.SaveChangesAsync();
            }

            await using var context = _database.CreateContext();
            var controller = CreateController(context, ReaderId);

            var result = await controller.CastVote(new CastVoteApiViewModel { VotingId = FreeVotingId, OptionId = FreeOptionId, Value = -5, IsVirtual = true });

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
            await using var check = _database.CreateContext();
            Assert.That(await check.Votes.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task CreateVoting_WhenThresholdIsNegative_ReturnsBadRequest()
        {
            await using var context = _database.CreateContext();
            var controller = CreateController(context, AuthorId);

            var result = await controller.CreateVoting(new CreateVotingViewModel
            {
                EpisodeId = EpisodeWithoutVotingId,
                Title = "負數票價",
                Policy = PolicyType.Equality,
                Limit = LimitType.Time,
                Threshold = -1,
                DeadLine = DateTime.Now.AddDays(1),
                Options = [new CreateOptionViewModel { Content = "選項一" }, new CreateOptionViewModel { Content = "選項二" }],
            });

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task CreateVoting_WhenFreeVotingUsesValueLimit_ReturnsBadRequest()
        {
            await using var context = _database.CreateContext();
            var controller = CreateController(context, AuthorId);

            var result = await controller.CreateVoting(new CreateVotingViewModel
            {
                EpisodeId = EpisodeWithoutVotingId,
                Title = "免費限值",
                Policy = PolicyType.Equality,
                Limit = LimitType.Value,
                Threshold = 0,
                CoinLimit = 1000,
                Options = [new CreateOptionViewModel { Content = "選項一" }, new CreateOptionViewModel { Content = "選項二" }],
            });

            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
            await using var check = _database.CreateContext();
            Assert.That(await check.Votings.CountAsync(), Is.EqualTo(1));
        }

        [Test]
        public async Task GetByEpisodeId_WhenEpisodeHasNoVoting_ReturnsEmptyList()
        {
            await using var context = _database.CreateContext();
            var controller = CreateController(context, null);

            var result = await controller.GetByEpisodeId(EpisodeWithoutVotingId);

            var ok = result.Result as OkObjectResult;
            Assert.That(ok, Is.Not.Null);
            Assert.That(((IEnumerable<object>)ok!.Value!).Count(), Is.EqualTo(0));
        }
    }
}
