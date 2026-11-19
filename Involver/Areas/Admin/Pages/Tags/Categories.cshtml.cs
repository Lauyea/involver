using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.Dashboard;

using Involver.Authorization.Admin;
using Involver.Services;
using Involver.Services.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Admin.Pages.Tags;

/// <summary>
/// 分類：由管理員維護的分類標籤，各自設定適用小說、文章或兩者。新增、修改、調整順序、移除，都寫進管理者活動紀錄
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="categoryService">分類服務</param>
/// <param name="auditService">管理者操作紀錄</param>
[Authorize(Policy = AdminPolicies.Tags)]
public class CategoriesModel(
    ApplicationDbContext context,
    ICategoryService categoryService,
    IAdminAuditService auditService) : AdminPageModel
{
    /// <summary>
    /// 稽核紀錄的對象類型
    /// </summary>
    private const string AuditTarget = "Category";

    /// <summary>
    /// 一列分類
    /// </summary>
    public class CategoryRow
    {
        /// <summary>分類</summary>
        public CategoryOption Category { get; init; }

        /// <summary>使用的小說數（不含已刪除）；不適用小說時為 null</summary>
        public int? NovelCount { get; init; }

        /// <summary>使用的一般文章數（不含已刪除）；不適用文章時為 null</summary>
        public int? ArticleCount { get; init; }

        /// <summary>選這個分類當作冷啟動偏好的會員數</summary>
        public int PreferenceCount { get; init; }
    }

    /// <summary>
    /// 分類，依顯示順序
    /// </summary>
    public List<CategoryRow> Rows { get; private set; } = [];

    /// <summary>
    /// 沒有分類的小說數
    /// </summary>
    public int UncategorizedNovels { get; private set; }

    /// <summary>
    /// 沒有分類的一般文章數
    /// </summary>
    public int UncategorizedArticles { get; private set; }

    /// <summary>
    /// 新增分類的名稱
    /// </summary>
    [BindProperty]
    public string NewName { get; set; }

    /// <summary>
    /// 新增分類的圖示
    /// </summary>
    [BindProperty]
    public string NewIcon { get; set; }

    /// <summary>
    /// 新增分類適用的作品類型
    /// </summary>
    [BindProperty]
    public List<CategoryScope> NewScopes { get; set; } = [];

    /// <summary>
    /// 載入分類與使用量
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var categories = await categoryService.GetCategoriesAsync(cancellationToken);
        var ids = categories.Select(c => c.TagId).ToList();
        var usages = await context.Tags
            .Where(t => ids.Contains(t.TagId))
            .Select(t => new
            {
                t.TagId,
                Novels = t.Novels!.Count(n => !n.IsDeleted),
                Articles = t.Articles!.Count(a => !a.IsDeleted && a.Type == ArticleType.General),
            })
            .AsNoTracking()
            .ToDictionaryAsync(t => t.TagId, cancellationToken);
        var preferences = await context.ProfileCategoryPreferences
            .Where(p => ids.Contains(p.TagId))
            .GroupBy(p => p.TagId)
            .Select(g => new { TagId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.TagId, g => g.Count, cancellationToken);

        Rows = categories
            .Select(c => new CategoryRow
            {
                Category = c,
                NovelCount = !c.AppliesTo(TagTarget.Novel) ? null : usages.TryGetValue(c.TagId, out var u) ? u.Novels : 0,
                ArticleCount = !c.AppliesTo(TagTarget.Article) ? null : usages.TryGetValue(c.TagId, out var v) ? v.Articles : 0,
                PreferenceCount = preferences.GetValueOrDefault(c.TagId),
            })
            .ToList();

        UncategorizedNovels = await context.Novels
            .CountAsync(n => !n.IsDeleted && !n.Tags!.Any(t => t.IsCategory && (t.CategoryScope & CategoryScope.Novel) != 0), cancellationToken);
        UncategorizedArticles = await context.Articles
            .CountAsync(a => !a.IsDeleted && a.Type == ArticleType.General && !a.Tags!.Any(t => t.IsCategory && (t.CategoryScope & CategoryScope.Article) != 0), cancellationToken);
    }

    /// <summary>
    /// 新增分類：已有同名標籤就把它設為分類
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostAddAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var (category, error) = await categoryService.AddAsync(NewName, NewIcon, Combine(NewScopes), cancellationToken);
        if (error != null)
        {
            StatusMessage = $"錯誤：{error}";
            return RedirectToPage();
        }

        await auditService.AddAsync(AdminAuditAction.CategoryAdded, AuditTarget, category.TagId.ToString(), after: $"{category.Name}（{CategoryService.Describe(category.Icon, category.Scope)}）", cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        StatusMessage = $"已新增分類「{category.Name}」。";
        return RedirectToPage();
    }

    /// <summary>
    /// 修改分類的圖示與適用範圍
    /// </summary>
    /// <param name="tagId">標籤 ID</param>
    /// <param name="icon">圖示</param>
    /// <param name="scopes">適用的作品類型</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostUpdateAsync(int tagId, string icon, List<CategoryScope> scopes, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var (name, before, after, error) = await categoryService.UpdateAsync(tagId, icon, Combine(scopes), cancellationToken);
        if (error != null)
        {
            StatusMessage = $"錯誤：{error}。";
            return RedirectToPage();
        }

        await auditService.AddAsync(AdminAuditAction.CategoryUpdated, AuditTarget, tagId.ToString(), before: $"{name}：{before}", after: after, cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        StatusMessage = $"已更新「{name}」：{after}。";
        return RedirectToPage();
    }

    /// <summary>
    /// 合併勾選的作品類型
    /// </summary>
    private static CategoryScope Combine(IEnumerable<CategoryScope> scopes) =>
        (scopes ?? []).Aggregate(CategoryScope.None, (all, scope) => all | scope) & CategoryScope.All;

    /// <summary>
    /// 調整順序
    /// </summary>
    /// <param name="tagId">標籤 ID</param>
    /// <param name="offset">-1 往前、1 往後</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostMoveAsync(int tagId, int offset, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var name = await context.Tags.Where(t => t.TagId == tagId).Select(t => t.Name).FirstOrDefaultAsync(cancellationToken);
        var swapped = await categoryService.MoveAsync(tagId, offset, cancellationToken);
        if (swapped == null)
        {
            return RedirectToPage();
        }

        await auditService.AddAsync(AdminAuditAction.CategoryUpdated, AuditTarget, tagId.ToString(), before: name, after: offset < 0 ? $"移到「{swapped}」前面" : $"移到「{swapped}」後面", cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return RedirectToPage();
    }

    /// <summary>
    /// 移除分類：標籤和作品上的關聯都保留
    /// </summary>
    /// <param name="tagId">標籤 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostRemoveAsync(int tagId, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var name = await categoryService.RemoveAsync(tagId, cancellationToken);
        if (name == null)
        {
            StatusMessage = "錯誤：找不到分類。";
            return RedirectToPage();
        }

        await auditService.AddAsync(AdminAuditAction.CategoryRemoved, AuditTarget, tagId.ToString(), before: name, after: "一般標籤", cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        StatusMessage = $"「{name}」已改回一般標籤，作品上的標籤保留。";
        return RedirectToPage();
    }
}
