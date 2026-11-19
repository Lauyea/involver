using DataAccess.Data;
using DataAccess.Models.Dashboard;

using Involver.Authorization.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Admin.Pages.People;

/// <summary>
/// A-5 管理者活動紀錄：依操作者、動作、日期篩選
/// </summary>
/// <param name="context">資料庫內容</param>
[Authorize(Policy = AdminPolicies.Administrators)]
public class AuditLogModel(ApplicationDbContext context) : AdminPageModel
{
    /// <summary>
    /// 每頁筆數
    /// </summary>
    public const int PageSize = 50;

    /// <summary>
    /// 操作者用戶名（部分比對）
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Actor { get; set; }

    /// <summary>
    /// 動作
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public AdminAuditAction? Action { get; set; }

    /// <summary>
    /// 開始日期（含）
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateOnly? From { get; set; }

    /// <summary>
    /// 結束日期（含）
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public DateOnly? To { get; set; }

    /// <summary>
    /// 頁碼
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 總頁數
    /// </summary>
    public int TotalPages { get; private set; }

    /// <summary>
    /// 符合條件的筆數
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// 本頁的紀錄
    /// </summary>
    public List<AdminAuditLog> Logs { get; private set; } = [];

    /// <summary>
    /// 載入活動紀錄
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var query = context.AdminAuditLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(Actor))
        {
            var actor = Actor.Trim();
            query = query.Where(l => l.ActorUserName.Contains(actor) || l.ActorProfileId == actor);
        }

        if (Action.HasValue)
        {
            query = query.Where(l => l.Action == Action.Value);
        }

        // 紀錄以 UTC 儲存，篩選的日期以伺服器時間解讀
        if (From.HasValue)
        {
            var from = From.Value.ToDateTime(TimeOnly.MinValue).ToUniversalTime();
            query = query.Where(l => l.CreatedAt >= from);
        }

        if (To.HasValue)
        {
            var to = To.Value.AddDays(1).ToDateTime(TimeOnly.MinValue).ToUniversalTime();
            query = query.Where(l => l.CreatedAt < to);
        }

        TotalCount = await query.CountAsync(cancellationToken);
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        PageIndex = Math.Clamp(PageIndex, 1, TotalPages);

        Logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .ThenByDescending(l => l.Id)
            .Skip((PageIndex - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync(cancellationToken);
    }
}
