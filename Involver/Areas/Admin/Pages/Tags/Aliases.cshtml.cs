using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.Dashboard;

using Involver.Authorization.Admin;
using Involver.Services;
using Involver.Services.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Admin.Pages.Tags;

/// <summary>
/// A-4 標籤合併與別名：相似標籤報表（一鍵合併）、手動合併、別名管理
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="tagService">標籤服務</param>
/// <param name="auditService">管理者操作紀錄</param>
[Authorize(Policy = AdminPolicies.Tags)]
public class AliasesModel(
    ApplicationDbContext context,
    ITagService tagService,
    IAdminAuditService auditService) : AdminPageModel
{
    /// <summary>
    /// 相似報表最多列出的組合數
    /// </summary>
    public const int ReportLimit = 50;

    /// <summary>
    /// 所有標籤（合併與別名的下拉選單）
    /// </summary>
    public List<TagSuggestion> AllTags { get; private set; } = [];

    /// <summary>
    /// 可能重複的標籤組合
    /// </summary>
    public List<TagDuplicateReportRow> Report { get; private set; } = [];

    /// <summary>
    /// 報表的總組合數
    /// </summary>
    public int ReportTotal { get; private set; }

    /// <summary>
    /// 所有別名
    /// </summary>
    public List<TagAlias> Aliases { get; private set; } = [];

    /// <summary>
    /// 載入報表與別名
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        AllTags = (await tagService.GetUsagesAsync(cancellationToken))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();

        // 只列出兩個標籤都還在的組合；NormalizedName 不一致的列另外處理，不在這裡合併
        var report = (await tagService.GetDuplicateReportAsync(cancellationToken))
            .Where(r => r.TagB != null)
            .ToList();
        ReportTotal = report.Count;
        Report = report.Take(ReportLimit).ToList();

        Aliases = await context.TagAliases
            .Include(a => a.Tag)
            .OrderBy(a => a.Tag!.Name)
            .ThenBy(a => a.Name)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// 合併標籤：把 source 的作品搬到 target，source 記成 target 的別名
    /// </summary>
    /// <param name="sourceTagId">被合併的標籤</param>
    /// <param name="targetTagId">保留的標籤</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostMergeAsync(int sourceTagId, int targetTagId, CancellationToken cancellationToken)
    {
        // 變更與稽核紀錄在同一個交易中提交
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var result = await tagService.MergeAsync(sourceTagId, targetTagId, cancellationToken);
        if (!result.Succeeded)
        {
            StatusMessage = $"錯誤：{result.ErrorMessage}";
            return RedirectToPage();
        }

        await auditService.AddAsync(
            AdminAuditAction.TagMerged, "Tag", targetTagId.ToString(),
            before: $"「{result.SourceName}」（ID {sourceTagId}）",
            after: $"合併到「{result.TargetName}」，搬移小說 {result.MovedNovels}、文章 {result.MovedArticles}，原名稱記為別名",
            cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        StatusMessage = $"已把「{result.SourceName}」合併到「{result.TargetName}」（新加上的小說 {result.MovedNovels} 部、文章 {result.MovedArticles} 篇）。";
        return RedirectToPage();
    }

    /// <summary>
    /// 新增別名
    /// </summary>
    /// <param name="aliasName">別名</param>
    /// <param name="tagId">對應的標籤</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostAddAliasAsync(string aliasName, int tagId, CancellationToken cancellationToken)
    {
        // 變更與稽核紀錄在同一個交易中提交
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var error = await tagService.AddAliasAsync(aliasName ?? string.Empty, tagId, cancellationToken);
        if (error != null)
        {
            StatusMessage = $"錯誤：{error}";
            return RedirectToPage();
        }

        var tagName = await context.Tags.Where(t => t.TagId == tagId).Select(t => t.Name).FirstOrDefaultAsync(cancellationToken);
        await auditService.AddAsync(AdminAuditAction.TagAliasAdded, "Tag", tagId.ToString(), after: $"別名「{aliasName}」→「{tagName}」", cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        StatusMessage = $"已新增別名「{aliasName}」→「{tagName}」。";
        return RedirectToPage();
    }

    /// <summary>
    /// 移除別名
    /// </summary>
    /// <param name="aliasId">別名 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostRemoveAliasAsync(int aliasId, CancellationToken cancellationToken)
    {
        // 變更與稽核紀錄在同一個交易中提交
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var tagId = await context.TagAliases.Where(a => a.TagAliasId == aliasId).Select(a => (int?)a.TagId).FirstOrDefaultAsync(cancellationToken);
        var name = await tagService.RemoveAliasAsync(aliasId, cancellationToken);
        if (name == null)
        {
            StatusMessage = "錯誤：找不到別名。";
            return RedirectToPage();
        }

        await auditService.AddAsync(AdminAuditAction.TagAliasRemoved, "Tag", tagId?.ToString(), before: $"別名「{name}」", after: "已移除", cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        StatusMessage = $"已移除別名「{name}」。";
        return RedirectToPage();
    }
}
