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
/// A-4 標籤：待審標籤的核准與駁回、使用量排行（小說、文章分開）
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="tagService">標籤服務</param>
/// <param name="auditService">管理者操作紀錄</param>
[Authorize(Policy = AdminPolicies.Tags)]
public class IndexModel(
    ApplicationDbContext context,
    ITagService tagService,
    IAdminAuditService auditService) : AdminPageModel
{
    /// <summary>
    /// 排行顯示的筆數
    /// </summary>
    public const int RankingSize = 30;

    /// <summary>
    /// 待審標籤
    /// </summary>
    public class PendingTag
    {
        /// <summary>標籤</summary>
        public TagSuggestion Tag { get; init; }

        /// <summary>申請者</summary>
        public string CreatorName { get; init; }

        /// <summary>申請者 ID</summary>
        public string CreatorId { get; init; }

        /// <summary>申請時間</summary>
        public DateTime CreateTime { get; init; }
    }

    /// <summary>
    /// 待審標籤
    /// </summary>
    public List<PendingTag> PendingTags { get; private set; } = [];

    /// <summary>
    /// 小說使用量排行
    /// </summary>
    public List<TagSuggestion> NovelRanking { get; private set; } = [];

    /// <summary>
    /// 文章使用量排行
    /// </summary>
    public List<TagSuggestion> ArticleRanking { get; private set; } = [];

    /// <summary>
    /// 標籤總數
    /// </summary>
    public int TotalTags { get; private set; }

    /// <summary>
    /// 沒有任何作品使用的標籤數
    /// </summary>
    public int UnusedTags { get; private set; }

    /// <summary>
    /// 載入待審標籤與排行
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var usages = await tagService.GetUsagesAsync(cancellationToken);
        TotalTags = usages.Count;
        UnusedTags = usages.Count(t => t.NovelCount == 0 && t.ArticleCount == 0);

        NovelRanking = usages.Where(t => t.NovelCount > 0).OrderByDescending(t => t.NovelCount).ThenBy(t => t.Name).Take(RankingSize).ToList();
        ArticleRanking = usages.Where(t => t.ArticleCount > 0).OrderByDescending(t => t.ArticleCount).ThenBy(t => t.Name).Take(RankingSize).ToList();

        var pending = await context.Tags
            .Where(t => t.Status == TagStatus.Pending)
            .OrderBy(t => t.CreateTime)
            .Select(t => new { t.TagId, t.CreateTime, t.CreatorProfileID, CreatorName = t.CreatorProfile!.UserName })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var usageById = usages.ToDictionary(t => t.TagId);
        PendingTags = pending
            .Where(p => usageById.ContainsKey(p.TagId))
            .Select(p => new PendingTag
            {
                Tag = usageById[p.TagId],
                CreatorId = p.CreatorProfileID,
                CreatorName = p.CreatorName,
                CreateTime = p.CreateTime,
            })
            .ToList();
    }

    /// <summary>
    /// 核准待審標籤
    /// </summary>
    /// <param name="tagId">標籤 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostApproveAsync(int tagId, CancellationToken cancellationToken)
    {
        // 變更與稽核紀錄在同一個交易中提交
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var name = await context.Tags
            .Where(t => t.TagId == tagId && t.Status == TagStatus.Pending)
            .Select(t => t.Name)
            .FirstOrDefaultAsync(cancellationToken);
        if (name == null || !await tagService.UpdateStatusAsync(tagId, TagStatus.Approved, cancellationToken))
        {
            StatusMessage = "錯誤：找不到待審標籤。";
            return RedirectToPage();
        }

        await auditService.AddAsync(AdminAuditAction.TagApproved, "Tag", tagId.ToString(), before: $"{name}：待審", after: "正式", cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        StatusMessage = $"已核准標籤「{name}」。";
        return RedirectToPage();
    }

    /// <summary>
    /// 駁回待審標籤：從作品上移除並刪除
    /// </summary>
    /// <param name="tagId">標籤 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostRejectAsync(int tagId, CancellationToken cancellationToken)
    {
        // 變更與稽核紀錄在同一個交易中提交
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var name = await tagService.RejectAsync(tagId, cancellationToken);
        if (name == null)
        {
            StatusMessage = "錯誤：找不到待審標籤。";
            return RedirectToPage();
        }

        await auditService.AddAsync(AdminAuditAction.TagRejected, "Tag", tagId.ToString(), before: $"{name}：待審", after: "駁回並刪除", cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        StatusMessage = $"已駁回標籤「{name}」。";
        return RedirectToPage();
    }
}
