using System.Text.RegularExpressions;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;

using Involver.Helpers;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Involver.Services;

/// <inheritdoc />
/// <param name="context">資料庫內容</param>
/// <param name="cache">快取（分類幾乎不變，每頁都要用）</param>
public partial class CategoryService(ApplicationDbContext context, IMemoryCache cache) : ICategoryService
{
    /// <summary>
    /// 快取鍵
    /// </summary>
    public const string CacheKey = "Categories:All";

    /// <summary>
    /// 快取時間
    /// </summary>
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(30);

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out IReadOnlyList<CategoryOption> cached))
        {
            return cached;
        }

        var categories = await context.Tags
            .Where(t => t.IsCategory)
            .OrderBy(t => t.CategoryOrder)
            .ThenBy(t => t.TagId)
            .Select(t => new CategoryOption(t.TagId, t.Name, t.CategoryIcon ?? ICategoryService.DefaultIcon, t.CategoryOrder, t.CategoryScope))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        cache.Set(CacheKey, (IReadOnlyList<CategoryOption>)categories, CacheDuration);
        return categories;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<CategoryOption>> GetCategoriesAsync(TagTarget target, CancellationToken cancellationToken = default) =>
        (await GetCategoriesAsync(cancellationToken)).Where(c => c.AppliesTo(target)).ToList();

    /// <inheritdoc />
    public async Task<CategorySelectionResult> SelectAsync(
        IEnumerable<int> selectedIds,
        IEnumerable<Tag> freeTags,
        TagTarget target,
        bool required,
        CancellationToken cancellationToken = default)
    {
        var scope = target.ToCategoryScope();
        var ids = (selectedIds ?? []).Distinct().ToList();
        var selected = ids.Count == 0
            ? []
            : (await context.Tags
                    .Where(t => ids.Contains(t.TagId) && t.IsCategory)
                    .ToListAsync(cancellationToken))
                .Where(t => t.IsCategoryFor(scope))
                .ToList();

        if (selected.Count != ids.Count)
        {
            return CategorySelectionResult.Fail("選擇的分類已經不存在或不適用，請重新選擇");
        }

        // 一般標籤輸入框裡剛好輸入了適用的分類名稱：當成勾選了這個分類；不適用的分類只是一般標籤
        foreach (var tag in (freeTags ?? []).Where(t => t.IsCategoryFor(scope)))
        {
            if (selected.All(t => t.TagId != tag.TagId))
            {
                selected.Add(tag);
            }
        }

        if (selected.Count > Parameters.CategoryMaxPerWork)
        {
            return CategorySelectionResult.Fail($"分類最多選 {Parameters.CategoryMaxPerWork} 個");
        }

        if (required && selected.Count == 0)
        {
            return CategorySelectionResult.Fail("請至少選一個分類");
        }

        return new CategorySelectionResult
        {
            Categories = selected.OrderBy(t => t.CategoryOrder).ThenBy(t => t.TagId).ToList(),
        };
    }

    /// <inheritdoc />
    public async Task<(CategoryOption Category, string ErrorMessage)> AddAsync(string name, string icon, CategoryScope scope, CancellationToken cancellationToken = default)
    {
        scope &= CategoryScope.All;
        if (scope == CategoryScope.None)
        {
            return (null, "請至少選一種適用的作品類型");
        }

        var displayName = TagNameNormalizer.CleanDisplayName(name);
        var normalizedName = TagNameNormalizer.Normalize(displayName);
        var error = TagService.ValidateName(displayName, normalizedName);
        if (error != null)
        {
            return (null, error);
        }

        if (!TryCleanIcon(icon, out var cleanIcon))
        {
            return (null, "圖示格式不正確，請填 Font Awesome 的類別名稱，例如 fa-hat-wizard");
        }

        var tag = await context.Tags.FirstOrDefaultAsync(t => t.NormalizedName == normalizedName, cancellationToken)
            ?? await context.TagAliases
                .Where(a => a.NormalizedName == normalizedName)
                .Select(a => a.Tag)
                .FirstOrDefaultAsync(cancellationToken);

        if (tag is { IsCategory: true })
        {
            return (null, $"「{tag.Name}」已經是分類");
        }

        var nextOrder = (await context.Tags.Where(t => t.IsCategory).MaxAsync(t => (int?)t.CategoryOrder, cancellationToken) ?? 0) + 1;
        if (tag == null)
        {
            tag = new Tag
            {
                Name = displayName,
                NormalizedName = normalizedName,
                Status = TagStatus.Approved,
                CreateTime = DateTime.Now,
            };
            context.Tags.Add(tag);
        }

        // 待審標籤設成分類時一併轉正
        tag.Status = TagStatus.Approved;
        tag.IsCategory = true;
        tag.CategoryScope = scope;
        tag.CategoryOrder = nextOrder;
        tag.CategoryIcon = cleanIcon;
        await context.SaveChangesAsync(cancellationToken);
        cache.Remove(CacheKey);

        return (new CategoryOption(tag.TagId, tag.Name, cleanIcon ?? ICategoryService.DefaultIcon, tag.CategoryOrder, scope), null);
    }

    /// <inheritdoc />
    public async Task<(string Name, string Before, string After, string ErrorMessage)> UpdateAsync(
        int tagId,
        string icon,
        CategoryScope scope,
        CancellationToken cancellationToken = default)
    {
        scope &= CategoryScope.All;
        if (scope == CategoryScope.None)
        {
            return (null, null, null, "請至少選一種適用的作品類型");
        }

        if (!TryCleanIcon(icon, out var cleanIcon))
        {
            return (null, null, null, "圖示格式不正確，請填 Font Awesome 的類別名稱，例如 fa-hat-wizard");
        }

        var tag = await context.Tags.FirstOrDefaultAsync(t => t.TagId == tagId && t.IsCategory, cancellationToken);
        if (tag == null)
        {
            return (null, null, null, "找不到分類");
        }

        var before = Describe(tag.CategoryIcon, tag.CategoryScope);
        tag.CategoryIcon = cleanIcon;
        tag.CategoryScope = scope;
        await context.SaveChangesAsync(cancellationToken);
        cache.Remove(CacheKey);
        return (tag.Name, before, Describe(cleanIcon, scope), null);
    }

    /// <summary>
    /// 分類設定的摘要（稽核紀錄用），例如「fa-feather，適用文章」
    /// </summary>
    /// <param name="icon">圖示</param>
    /// <param name="scope">適用範圍</param>
    /// <returns>摘要</returns>
    public static string Describe(string icon, CategoryScope scope) =>
        $"{icon ?? ICategoryService.DefaultIcon}，適用{ScopeName(scope)}";

    /// <summary>
    /// 適用範圍的中文名稱
    /// </summary>
    /// <param name="scope">適用範圍</param>
    /// <returns>中文名稱</returns>
    public static string ScopeName(CategoryScope scope) => scope switch
    {
        CategoryScope.Novel => "小說",
        CategoryScope.Article => "文章",
        CategoryScope.All => "小說與文章",
        _ => "無",
    };

    /// <inheritdoc />
    public async Task<string> MoveAsync(int tagId, int offset, CancellationToken cancellationToken = default)
    {
        var categories = await context.Tags
            .Where(t => t.IsCategory)
            .OrderBy(t => t.CategoryOrder)
            .ThenBy(t => t.TagId)
            .ToListAsync(cancellationToken);

        var index = categories.FindIndex(t => t.TagId == tagId);
        var other = index + Math.Sign(offset);
        if (index < 0 || offset == 0 || other < 0 || other >= categories.Count)
        {
            return null;
        }

        var neighbour = categories[other].Name;
        (categories[index], categories[other]) = (categories[other], categories[index]);

        // 重新編號，順序永遠是 1、2、3……
        for (var i = 0; i < categories.Count; i++)
        {
            categories[i].CategoryOrder = i + 1;
        }

        await context.SaveChangesAsync(cancellationToken);
        cache.Remove(CacheKey);
        return neighbour;
    }

    /// <inheritdoc />
    public async Task<string> RemoveAsync(int tagId, CancellationToken cancellationToken = default)
    {
        var tag = await context.Tags.FirstOrDefaultAsync(t => t.TagId == tagId && t.IsCategory, cancellationToken);
        if (tag == null)
        {
            return null;
        }

        tag.IsCategory = false;
        tag.CategoryScope = CategoryScope.None;
        tag.CategoryOrder = 0;
        tag.CategoryIcon = null;
        await context.ProfileCategoryPreferences
            .Where(p => p.TagId == tagId)
            .ExecuteDeleteAsync(cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        cache.Remove(CacheKey);
        return tag.Name;
    }

    /// <summary>
    /// 整理圖示名稱：只接受 Font Awesome 的類別名稱（fa-xxx）。空白時為 null（使用預設圖示）
    /// </summary>
    /// <param name="icon">輸入</param>
    /// <param name="clean">整理後的圖示</param>
    /// <returns>格式是否正確</returns>
    public static bool TryCleanIcon(string icon, out string clean)
    {
        clean = string.IsNullOrWhiteSpace(icon) ? null : icon.Trim().ToLowerInvariant();
        if (clean != null && clean.StartsWith("fas ", StringComparison.Ordinal))
        {
            clean = clean[4..].Trim();
        }

        return clean == null || IconPattern().IsMatch(clean);
    }

    [GeneratedRegex("^fa-[a-z0-9-]{1,40}$")]
    private static partial Regex IconPattern();
}
