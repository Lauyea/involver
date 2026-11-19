using System;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;
using DataAccess.Models.Recommendation;

using Involver.Helpers;
using Involver.Services;

using InvolverTest.Infrastructure;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using NUnit.Framework;

namespace InvolverTest.Services
{
    /// <summary>
    /// 分類標籤：作品選分類（依適用範圍）、後台維護分類，以及分類不佔一般標籤名額、合併時接手分類
    /// </summary>
    [TestFixture]
    public class CategoryServiceTests
    {
        private const string AuthorId = "author";
        private const int FantasyId = 1;
        private const int MysteryId = 2;
        private const int LoveId = 3;
        private const int HistoryId = 4;
        private const int MagicId = 10;
        private const int PendingId = 11;

        private SqliteTestDatabase _database = null!;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            using var context = _database.CreateContext();

            context.Users.Add(new InvolverUser { Id = AuthorId, UserName = AuthorId });
            context.Profiles.Add(new Profile { ProfileID = AuthorId, UserName = AuthorId, SeqNo = 1 });
            context.Tags.AddRange(
                NewCategory(FantasyId, "奇幻", 1),
                NewCategory(MysteryId, "推理", 2),
                NewCategory(LoveId, "愛情", 3),
                NewCategory(HistoryId, "歷史", 4),
                NewTag(MagicId, "魔法", TagStatus.Approved),
                NewTag(PendingId, "散文", TagStatus.Pending));
            context.ProfileCategoryPreferences.Add(new ProfileCategoryPreference { ProfileId = AuthorId, TagId = LoveId, CreatedAt = DateTime.UtcNow });
            context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _database?.Dispose();
        }

        [Test]
        public async Task GetCategoriesAsync_ReturnsCategoriesInOrderWithDefaultIcon()
        {
            await using var context = _database.CreateContext();

            var categories = await CreateService(context).GetCategoriesAsync();

            Assert.That(categories.Select(c => c.Name), Is.EqualTo(new[] { "奇幻", "推理", "愛情", "歷史" }));
            Assert.That(categories[0].Icon, Is.EqualTo("fa-hat-wizard"));
            Assert.That(categories[1].Icon, Is.EqualTo(ICategoryService.DefaultIcon));
        }

        [Test]
        public async Task SelectAsync_FreeTagThatIsCategory_IsMergedIntoCategories()
        {
            await using var context = _database.CreateContext();
            var freeTags = await context.Tags.Where(t => t.TagId == MysteryId || t.TagId == MagicId).ToListAsync();

            var result = await CreateService(context).SelectAsync([FantasyId], freeTags, TagTarget.Novel, required: true);

            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.Categories.Select(t => t.TagId), Is.EqualTo(new[] { FantasyId, MysteryId }));
        }

        [Test]
        public async Task SelectAsync_MoreThanMax_Fails()
        {
            await using var context = _database.CreateContext();

            var result = await CreateService(context).SelectAsync([FantasyId, MysteryId, LoveId, HistoryId], [], TagTarget.Novel, required: false);

            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain(Parameters.CategoryMaxPerWork.ToString()));
        }

        [Test]
        public async Task SelectAsync_RequiredWithoutCategory_Fails()
        {
            await using var context = _database.CreateContext();
            var service = CreateService(context);

            Assert.That((await service.SelectAsync([], [], TagTarget.Novel, required: true)).Succeeded, Is.False);
            Assert.That((await service.SelectAsync([], [], TagTarget.Novel, required: false)).Succeeded, Is.True);
        }

        [Test]
        public async Task SelectAsync_IdThatIsNotCategory_Fails()
        {
            await using var context = _database.CreateContext();

            var result = await CreateService(context).SelectAsync([MagicId], [], TagTarget.Novel, required: false);

            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public async Task AddAsync_ExistingPendingTag_BecomesApprovedCategoryAtTheEnd()
        {
            await using (var context = _database.CreateContext())
            {
                var (category, error) = await CreateService(context).AddAsync(" 散文 ", "fas fa-feather", CategoryScope.Article);

                Assert.That(error, Is.Null);
                Assert.That(category.TagId, Is.EqualTo(PendingId));
                Assert.That(category.Icon, Is.EqualTo("fa-feather"));
            }

            await using var verify = _database.CreateContext();
            var tag = await verify.Tags.SingleAsync(t => t.TagId == PendingId);
            Assert.That(tag.IsCategory, Is.True);
            Assert.That(tag.Status, Is.EqualTo(TagStatus.Approved));
            Assert.That(tag.CategoryOrder, Is.EqualTo(5));
            Assert.That(tag.CategoryScope, Is.EqualTo(CategoryScope.Article));
        }

        [Test]
        public async Task AddAsync_NewName_CreatesApprovedTag_AndRejectsDuplicatesAndBadIcons()
        {
            await using var context = _database.CreateContext();
            var service = CreateService(context);

            var (created, error) = await service.AddAsync("讀書心得", null, CategoryScope.Article);
            Assert.That(error, Is.Null);
            Assert.That(created.Icon, Is.EqualTo(ICategoryService.DefaultIcon));
            Assert.That(await context.Tags.CountAsync(t => t.Name == "讀書心得" && t.IsCategory && t.Status == TagStatus.Approved), Is.EqualTo(1));

            Assert.That((await service.AddAsync("奇幻", null, CategoryScope.Novel)).ErrorMessage, Does.Contain("已經是分類"));
            Assert.That((await service.AddAsync("評論", "<script>", CategoryScope.Article)).ErrorMessage, Is.Not.Null);
        }

        [Test]
        public async Task MoveAsync_SwapsWithNeighbour_AndRenumbers()
        {
            await using (var context = _database.CreateContext())
            {
                var swapped = await CreateService(context).MoveAsync(LoveId, -1);
                Assert.That(swapped, Is.EqualTo("推理"));
                Assert.That(await CreateService(context).MoveAsync(FantasyId, -1), Is.Null);
            }

            await using var verify = _database.CreateContext();
            var names = await verify.Tags.Where(t => t.IsCategory).OrderBy(t => t.CategoryOrder).Select(t => t.Name).ToListAsync();
            Assert.That(names, Is.EqualTo(new[] { "奇幻", "愛情", "推理", "歷史" }));
        }

        [Test]
        public async Task RemoveAsync_KeepsTagAndWorks_DeletesPreferences()
        {
            await using (var context = _database.CreateContext())
            {
                context.Novels.Add(new Novel { NovelID = 1, Title = "愛情小說", Introduction = "介紹介紹", ProfileID = AuthorId, Tags = [context.Tags.Single(t => t.TagId == LoveId)] });
                await context.SaveChangesAsync();

                Assert.That(await CreateService(context).RemoveAsync(LoveId), Is.EqualTo("愛情"));
            }

            await using var verify = _database.CreateContext();
            var tag = await verify.Tags.Include(t => t.Novels).SingleAsync(t => t.TagId == LoveId);
            Assert.That(tag.IsCategory, Is.False);
            Assert.That(tag.CategoryScope, Is.EqualTo(CategoryScope.None));
            Assert.That(tag.Novels, Has.Count.EqualTo(1));
            Assert.That(await verify.ProfileCategoryPreferences.AnyAsync(), Is.False);
        }

        [Test]
        public async Task ResolveTagsAsync_CategoryTags_DoNotCountTowardLimit()
        {
            await using var context = _database.CreateContext();
            var tagService = new TagService(context);

            // 文章一般標籤上限 5 個；「奇幻」適用文章不佔名額，「推理」只適用小說，在文章上算一般標籤
            var names = new[] { "奇幻", "推理", "魔法", "一", "二", "三" };
            var tagString = "[" + string.Join(",", names.Select(n => $"{{\"value\":\"{n}\"}}")) + "]";

            var result = await tagService.ResolveTagsAsync(tagString, TagTarget.Article, AuthorId, TagAuthorLevel.Trusted);

            Assert.That(result.Succeeded, Is.True, result.ErrorMessage);
            Assert.That(result.Tags.Count(t => !t.IsCategoryFor(CategoryScope.Article)), Is.EqualTo(Parameters.ArticleTagSize));
        }

        [Test]
        public async Task ResolveTagsAsync_CategoryNotForTarget_CountsTowardLimit()
        {
            await using var context = _database.CreateContext();
            var names = new[] { "推理", "魔法", "一", "二", "三", "四" };
            var tagString = "[" + string.Join(",", names.Select(n => $"{{\"value\":\"{n}\"}}")) + "]";

            var result = await new TagService(context).ResolveTagsAsync(tagString, TagTarget.Article, AuthorId, TagAuthorLevel.Trusted);

            Assert.That(result.Succeeded, Is.False);
        }

        [Test]
        public async Task GetSuggestionsAsync_ExcludesCategoriesForTargetOnly()
        {
            await using var context = _database.CreateContext();
            var tagService = new TagService(context);

            var novel = await tagService.GetSuggestionsAsync(TagTarget.Novel);
            var article = await tagService.GetSuggestionsAsync(TagTarget.Article);

            Assert.That(novel.Select(s => s.Name), Is.EqualTo(new[] { "魔法" }));
            Assert.That(article.Select(s => s.Name), Is.EquivalentTo(new[] { "魔法", "推理", "愛情", "歷史" }));
        }

        [Test]
        public async Task SelectAsync_ForArticle_RejectsNovelOnlyCategory_AndKeepsItAsPlainTag()
        {
            await using var context = _database.CreateContext();
            var service = CreateService(context);
            var freeTags = await context.Tags.Where(t => t.TagId == MysteryId).ToListAsync();

            var rejected = await service.SelectAsync([MysteryId], [], TagTarget.Article, required: false);
            var merged = await service.SelectAsync([FantasyId], freeTags, TagTarget.Article, required: false);

            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(merged.Categories.Select(t => t.TagId), Is.EqualTo(new[] { FantasyId }));
        }

        [Test]
        public async Task GetCategoriesAsync_ForTarget_FiltersByScope()
        {
            await using var context = _database.CreateContext();
            var service = CreateService(context);

            Assert.That((await service.GetCategoriesAsync(TagTarget.Article)).Select(c => c.Name), Is.EqualTo(new[] { "奇幻" }));
            Assert.That((await service.GetCategoriesAsync(TagTarget.Novel)).Count, Is.EqualTo(4));
        }

        [Test]
        public async Task UpdateAsync_ChangesIconAndScope_RejectsEmptyScope()
        {
            await using var context = _database.CreateContext();
            var service = CreateService(context);

            var (name, before, after, error) = await service.UpdateAsync(LoveId, "fa-heart", CategoryScope.All);
            var empty = await service.UpdateAsync(LoveId, "fa-heart", CategoryScope.None);

            Assert.That(error, Is.Null);
            Assert.That(name, Is.EqualTo("愛情"));
            Assert.That(before, Is.EqualTo($"{ICategoryService.DefaultIcon}，適用小說"));
            Assert.That(after, Is.EqualTo("fa-heart，適用小說與文章"));
            Assert.That(empty.ErrorMessage, Is.Not.Null);
            Assert.That((await context.Tags.SingleAsync(t => t.TagId == LoveId)).CategoryScope, Is.EqualTo(CategoryScope.All));
        }

        [Test]
        public async Task MergeAsync_CategoryIntoPlainTag_TargetBecomesCategoryAndKeepsPreferences()
        {
            await using (var context = _database.CreateContext())
            {
                var result = await new TagService(context).MergeAsync(LoveId, MagicId);
                Assert.That(result.Succeeded, Is.True, result.ErrorMessage);
            }

            await using var verify = _database.CreateContext();
            var target = await verify.Tags.SingleAsync(t => t.TagId == MagicId);
            Assert.That(target.IsCategory, Is.True);
            Assert.That(target.CategoryOrder, Is.EqualTo(3));
            Assert.That(target.CategoryScope, Is.EqualTo(CategoryScope.Novel));
            Assert.That(await verify.ProfileCategoryPreferences.Select(p => p.TagId).ToListAsync(), Is.EqualTo(new[] { MagicId }));
        }

        private static CategoryService CreateService(ApplicationDbContext context) =>
            new(context, new MemoryCache(new MemoryCacheOptions()));

        private static Tag NewTag(int id, string name, TagStatus status) => new()
        {
            TagId = id,
            Name = name,
            NormalizedName = TagNameNormalizer.Normalize(name),
            Status = status,
        };

        private static Tag NewCategory(int id, string name, int order)
        {
            var tag = NewTag(id, name, TagStatus.Approved);
            tag.IsCategory = true;
            tag.CategoryScope = id == FantasyId ? CategoryScope.All : CategoryScope.Novel;
            tag.CategoryOrder = order;
            tag.CategoryIcon = id == FantasyId ? "fa-hat-wizard" : null;
            return tag;
        }
    }
}
