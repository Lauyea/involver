using System.Linq;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;

using Involver.Helpers;
using Involver.Services;

using InvolverTest.Infrastructure;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

namespace InvolverTest.Services
{
    /// <summary>
    /// Issue #237（#234 階段 3）：標籤合併、別名、駁回
    /// </summary>
    [TestFixture]
    public class TagServiceAdminTests
    {
        private const string AuthorId = "author";
        private const int FantasyId = 1;
        private const int MagicId = 2;
        private const int PendingId = 3;

        private SqliteTestDatabase _database = null!;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            using var context = _database.CreateContext();

            context.Users.Add(new InvolverUser { Id = AuthorId, UserName = AuthorId });
            context.Profiles.Add(new Profile { ProfileID = AuthorId, UserName = AuthorId, SeqNo = 1 });

            var fantasy = NewTag(FantasyId, "奇幻", TagStatus.Approved);
            var magic = NewTag(MagicId, "魔幻", TagStatus.Approved);
            var pending = NewTag(PendingId, "待審標籤", TagStatus.Pending);
            context.Tags.AddRange(fantasy, magic, pending);

            context.Novels.AddRange(
                new Novel { NovelID = 1, Title = "只有魔幻", Introduction = "介紹介紹", ProfileID = AuthorId, Tags = [magic] },
                new Novel { NovelID = 2, Title = "兩個都有", Introduction = "介紹介紹", ProfileID = AuthorId, Tags = [fantasy, magic, pending] });
            context.Articles.Add(new Article { ArticleID = 1, Title = "魔幻文章", Content = "內容", ProfileID = AuthorId, Tags = [magic] });
            context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _database?.Dispose();
        }

        private static Tag NewTag(int id, string name, TagStatus status) => new()
        {
            TagId = id,
            Name = name,
            NormalizedName = TagNameNormalizer.Normalize(name),
            Status = status,
        };

        private async Task<int[]> GetNovelTagIdsAsync(int novelId)
        {
            using var context = _database.CreateContext();
            return await context.Novels
                .Where(n => n.NovelID == novelId)
                .SelectMany(n => n.Tags!.Select(t => t.TagId))
                .OrderBy(id => id)
                .ToArrayAsync();
        }

        [Test]
        public async Task MergeAsync_WithTwoTags_MovesWorksAndRecordsAlias()
        {
            TagMergeResult result;
            using (var context = _database.CreateContext())
            {
                result = await new TagService(context).MergeAsync(MagicId, FantasyId);
            }

            using var verify = _database.CreateContext();
            var alias = await verify.TagAliases.SingleAsync();
            var articleTags = await verify.Articles.Where(a => a.ArticleID == 1).SelectMany(a => a.Tags!.Select(t => t.TagId)).ToListAsync();

            Assert.Multiple(async () =>
            {
                Assert.That(result.Succeeded, Is.True);
                Assert.That(result.MovedNovels, Is.EqualTo(1), "原本已有目標標籤的作品不重複計算");
                Assert.That(result.MovedArticles, Is.EqualTo(1));
                Assert.That(await GetNovelTagIdsAsync(1), Is.EqualTo(new[] { FantasyId }));
                Assert.That(await GetNovelTagIdsAsync(2), Is.EqualTo(new[] { FantasyId, PendingId }));
                Assert.That(articleTags, Is.EqualTo(new[] { FantasyId }));
                Assert.That(await verify.Tags.AnyAsync(t => t.TagId == MagicId), Is.False);
                Assert.That(alias.Name, Is.EqualTo("魔幻"));
                Assert.That(alias.TagId, Is.EqualTo(FantasyId));
            });
        }

        [Test]
        public async Task MergeAsync_InsideCallerTransaction_RollsBackWithCaller()
        {
            using (var context = _database.CreateContext())
            {
                // 後台頁面先開交易，合併與稽核紀錄一起提交；這裡模擬稽核寫入失敗而沒有提交
                await using var transaction = await context.Database.BeginTransactionAsync();
                var result = await new TagService(context).MergeAsync(MagicId, FantasyId);
                Assert.That(result.Succeeded, Is.True);
                await transaction.RollbackAsync();
            }

            using var verify = _database.CreateContext();

            Assert.That(await verify.Tags.AnyAsync(t => t.TagId == MagicId), Is.True, "沒有提交時合併不能生效");
            Assert.That(await verify.TagAliases.CountAsync(), Is.EqualTo(0));
            Assert.That(await GetNovelTagIdsAsync(1), Is.EqualTo(new[] { MagicId }));
        }

        [Test]
        public async Task MergeAsync_WithSameTag_ReturnsFailure()
        {
            using var context = _database.CreateContext();

            var result = await new TagService(context).MergeAsync(FantasyId, FantasyId);

            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public async Task ResolveTagsAsync_WithAliasName_ReturnsTargetTag()
        {
            using (var context = _database.CreateContext())
            {
                await new TagService(context).MergeAsync(MagicId, FantasyId);
            }

            using var resolveContext = _database.CreateContext();
            var result = await new TagService(resolveContext).ResolveTagsAsync(
                "[{\"value\":\"魔幻\"}]", TagTarget.Novel, AuthorId, TagAuthorLevel.Author);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Tags.Select(t => t.TagId), Is.EqualTo(new[] { FantasyId }));
            Assert.That(result.CreatedTags, Is.Empty, "別名不會建立新標籤");
        }

        [Test]
        public async Task FindSimilarAsync_WithAliasName_ReturnsTargetAsExisting()
        {
            using (var context = _database.CreateContext())
            {
                Assert.That(await new TagService(context).AddAliasAsync("西方奇幻", FantasyId), Is.Null);
            }

            using var findContext = _database.CreateContext();
            var result = await new TagService(findContext).FindSimilarAsync("西方奇幻", TagTarget.Novel);

            Assert.That(result.Existing?.TagId, Is.EqualTo(FantasyId));
        }

        [Test]
        public async Task AddAliasAsync_WithExistingTagName_ReturnsError()
        {
            using var context = _database.CreateContext();

            var error = await new TagService(context).AddAliasAsync("魔幻", FantasyId);

            Assert.That(error, Does.Contain("合併"));
            Assert.That(await context.TagAliases.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task AddAliasAsync_WithDuplicateAlias_ReturnsError()
        {
            using var context = _database.CreateContext();
            var service = new TagService(context);
            await service.AddAliasAsync("西方奇幻", FantasyId);

            var error = await service.AddAliasAsync("西方奇幻", MagicId);

            Assert.That(error, Does.Contain("已經存在"));
        }

        [Test]
        public async Task RemoveAliasAsync_WithExistingAlias_ReturnsName()
        {
            using var context = _database.CreateContext();
            var service = new TagService(context);
            await service.AddAliasAsync("西方奇幻", FantasyId);
            var aliasId = await context.TagAliases.Select(a => a.TagAliasId).SingleAsync();

            var name = await service.RemoveAliasAsync(aliasId);

            Assert.That(name, Is.EqualTo("西方奇幻"));
            Assert.That(await context.TagAliases.CountAsync(), Is.EqualTo(0));
        }

        [Test]
        public async Task RejectAsync_WithPendingTag_RemovesTagFromWorks()
        {
            using (var context = _database.CreateContext())
            {
                Assert.That(await new TagService(context).RejectAsync(PendingId), Is.EqualTo("待審標籤"));
            }

            Assert.That(await GetNovelTagIdsAsync(2), Is.EqualTo(new[] { FantasyId, MagicId }));
        }

        [Test]
        public async Task RejectAsync_WithApprovedTag_ReturnsNull()
        {
            using var context = _database.CreateContext();

            var name = await new TagService(context).RejectAsync(FantasyId);

            Assert.That(name, Is.Null);
            Assert.That(await context.Tags.AnyAsync(t => t.TagId == FantasyId), Is.True);
        }
    }
}
