using System.Linq;
using System.Threading.Tasks;

using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;

using Involver.Services.Recommendations;

using InvolverTest.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

using NUnit.Framework;

namespace InvolverTest.Services
{
    /// <summary>
    /// Issue #238：章節閱讀紀錄（讀完率的來源）
    /// </summary>
    [TestFixture]
    public class EpisodeReadServiceTests
    {
        private const string ReaderId = "reader";

        private SqliteTestDatabase _database = null!;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            using var context = _database.CreateContext();

            context.Users.Add(new InvolverUser { Id = ReaderId, UserName = ReaderId });
            context.Profiles.Add(new Profile { ProfileID = ReaderId, UserName = ReaderId, SeqNo = 1 });
            context.Novels.Add(new Novel { NovelID = 1, Title = "小說", Introduction = "介紹介紹", ProfileID = ReaderId });
            context.Episodes.AddRange(
                new Episode { EpisodeID = 1, Title = "第一章", Content = "內容", NovelID = 1 },
                new Episode { EpisodeID = 2, Title = "第二章", Content = "內容", NovelID = 1 });
            context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _database.Dispose();
        }

        [Test]
        public async Task MarkReachedEndAsync_AfterOpening_MarksRead()
        {
            await using (var context = _database.CreateContext())
            {
                var service = CreateService(context);
                await service.RecordOpenAsync(ReaderId, 1);

                Assert.That(await service.MarkReachedEndAsync(ReaderId, 1), Is.True);
            }

            await using var verify = _database.CreateContext();
            var read = await verify.EpisodeReads.SingleAsync();
            Assert.That(read.ReachedEnd, Is.True);
            Assert.That(read.ReachedEndAt, Is.Not.Null);
        }

        [Test]
        public async Task MarkReachedEndAsync_WithoutOpening_DoesNotCreateRecord()
        {
            await using (var context = _database.CreateContext())
            {
                Assert.That(await CreateService(context).MarkReachedEndAsync(ReaderId, 2), Is.False);
                Assert.That(await CreateService(context).MarkReachedEndAsync(ReaderId, 999), Is.False);
            }

            await using var verify = _database.CreateContext();
            Assert.That(await verify.EpisodeReads.AnyAsync(), Is.False);
        }

        private static EpisodeReadService CreateService(ApplicationDbContext context) =>
            new(context, NullLogger<EpisodeReadService>.Instance);
    }
}
