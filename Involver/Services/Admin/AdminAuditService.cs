using DataAccess.Data;
using DataAccess.Models.Dashboard;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Involver.Services.Admin;

/// <summary>
/// 寫入管理者操作紀錄（<see cref="AdminAuditLog"/>）
/// </summary>
public interface IAdminAuditService
{
    /// <summary>
    /// 把一筆操作紀錄加入 DbContext，由呼叫端和這次的變更一起 SaveChanges，兩者一起成功或失敗
    /// </summary>
    /// <param name="action">動作</param>
    /// <param name="targetType">對象類型，例如 User、Payment、Tag</param>
    /// <param name="targetId">對象 ID</param>
    /// <param name="before">變更前摘要</param>
    /// <param name="after">變更後摘要</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>加入的紀錄</returns>
    Task<AdminAuditLog> AddAsync(
        AdminAuditAction action,
        string targetType,
        string targetId,
        string before = null,
        string after = null,
        CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class AdminAuditService(
    ApplicationDbContext context,
    IHttpContextAccessor httpContextAccessor,
    UserManager<InvolverUser> userManager) : IAdminAuditService
{
    /// <summary>
    /// 摘要欄位的長度上限，與資料表欄位一致
    /// </summary>
    private const int SummaryMaxLength = 1000;

    /// <inheritdoc />
    public async Task<AdminAuditLog> AddAsync(
        AdminAuditAction action,
        string targetType,
        string targetId,
        string before = null,
        string after = null,
        CancellationToken cancellationToken = default)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("管理者操作紀錄必須在 HTTP 請求中寫入。");

        var actorId = userManager.GetUserId(httpContext.User)
            ?? throw new InvalidOperationException("找不到操作者。");

        var actorName = await context.Profiles
            .Where(p => p.ProfileID == actorId)
            .Select(p => p.UserName)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        var log = new AdminAuditLog
        {
            ActorProfileId = actorId,
            ActorUserName = actorName ?? httpContext.User.Identity?.Name,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            Before = Truncate(before),
            After = Truncate(after),
            CreatedAt = DateTime.UtcNow,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
        };

        context.AdminAuditLogs.Add(log);
        return log;
    }

    private static string Truncate(string value)
    {
        return value is { Length: > SummaryMaxLength } ? value[..SummaryMaxLength] : value;
    }
}
