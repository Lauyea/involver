using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

using DataAccess.Data;
using DataAccess.Models.Dashboard;
using DataAccess.Models.Recommendation;
using DataAccess.Services.Recommendations;

using Involver.Services.Admin;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Involver.Services.Recommendations;

/// <summary>
/// 推薦設定的一個版本（列表用）
/// </summary>
/// <param name="Version">版本號</param>
/// <param name="IsActive">是否啟用中</param>
/// <param name="CreatedBy">建立者 ProfileID</param>
/// <param name="CreatedByName">建立者用戶名</param>
/// <param name="CreatedAt">建立時間（UTC）</param>
/// <param name="Note">修改說明</param>
public sealed record RecommendationSettingVersion(
    int Version,
    bool IsActive,
    string CreatedBy,
    string CreatedByName,
    DateTime CreatedAt,
    string Note);

/// <summary>
/// 推薦設定：讀取（快取幾分鐘）、新增版本、切換版本。只有管理員後台會修改，每次修改都寫入 <see cref="AdminAuditLog"/>
/// </summary>
public interface IRecommendationSettingsService
{
    /// <summary>
    /// 目前啟用的設定（快取幾分鐘）
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>設定與版本</returns>
    Task<ActiveRecommendationSettings> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 所有版本，新的在前
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>版本列表</returns>
    Task<List<RecommendationSettingVersion>> GetVersionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 新增一個版本並設為啟用；舊版本保留
    /// </summary>
    /// <param name="settings">設定</param>
    /// <param name="note">修改說明</param>
    /// <param name="actorId">操作者 ProfileID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>新的版本號</returns>
    Task<int> SaveAsync(RecommendationSettings settings, string note, string actorId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 把指定版本設為啟用
    /// </summary>
    /// <param name="version">版本號</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>版本不存在時回傳 false</returns>
    Task<bool> ActivateAsync(int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// 回到上一版：啟用比目前版本小的最新版本
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>啟用的版本號；沒有上一版時為 null</returns>
    Task<int?> RollbackAsync(CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class RecommendationSettingsService(
    ApplicationDbContext context,
    IAdminAuditService auditService,
    IMemoryCache cache) : IRecommendationSettingsService
{
    /// <summary>
    /// 快取鍵
    /// </summary>
    public const string CacheKey = "Recommendation:ActiveSettings";

    /// <summary>
    /// 快取時間
    /// </summary>
    public static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 稽核紀錄的對象類型
    /// </summary>
    public const string AuditTargetType = "RecommendationSetting";

    /// <inheritdoc />
    public async Task<ActiveRecommendationSettings> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        if (cache.TryGetValue(CacheKey, out ActiveRecommendationSettings cached))
        {
            return cached;
        }

        var active = await RecommendationEngine.LoadActiveSettingsAsync(context, cancellationToken);
        cache.Set(CacheKey, active, CacheDuration);
        return active;
    }

    /// <inheritdoc />
    public async Task<List<RecommendationSettingVersion>> GetVersionsAsync(CancellationToken cancellationToken = default)
    {
        return await context.RecommendationSettings
            .OrderByDescending(s => s.Version)
            .Select(s => new RecommendationSettingVersion(
                s.Version,
                s.IsActive,
                s.CreatedBy,
                context.Profiles.Where(p => p.ProfileID == s.CreatedBy).Select(p => p.UserName).FirstOrDefault(),
                s.CreatedAt,
                s.Note))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int> SaveAsync(RecommendationSettings settings, string note, string actorId, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var errors = settings.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join("；", errors.Select(e => e.ErrorMessage)), nameof(settings));
        }

        // Serializable：讀最大版本號到新增之間不讓別人插入，兩位管理員同時儲存不會拿到同一個版本號
        await using var transaction = await context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);

        var previous = await RecommendationEngine.LoadActiveSettingsAsync(context, cancellationToken);
        var nextVersion = (await context.RecommendationSettings.MaxAsync(s => (int?)s.Version, cancellationToken) ?? 0) + 1;

        // 唯一索引保證同時只有一個啟用版本：先全部停用，再新增
        await context.RecommendationSettings
            .Where(s => s.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false), cancellationToken);

        context.RecommendationSettings.Add(new RecommendationSetting
        {
            Version = nextVersion,
            SettingsJson = settings.ToJson(),
            IsActive = true,
            CreatedBy = actorId,
            CreatedAt = DateTime.UtcNow,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
        });

        await auditService.AddAsync(
            AdminAuditAction.RecommendationSettingSaved,
            AuditTargetType,
            nextVersion.ToString(CultureInfo.InvariantCulture),
            before: previous.Version is { } v ? $"v{v}" : "程式預設值",
            after: $"v{nextVersion}：{Describe(previous.Settings, settings)}",
            cancellationToken: cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        cache.Remove(CacheKey);
        return nextVersion;
    }

    /// <inheritdoc />
    public async Task<bool> ActivateAsync(int version, CancellationToken cancellationToken = default)
    {
        var target = await context.RecommendationSettings
            .Where(s => s.Version == version)
            .Select(s => new { s.Version, s.IsActive })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (target is null)
        {
            return false;
        }

        if (target.IsActive)
        {
            return true;
        }

        var previous = await context.RecommendationSettings
            .Where(s => s.IsActive)
            .Select(s => (int?)s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        await context.RecommendationSettings
            .Where(s => s.IsActive)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false), cancellationToken);
        await context.RecommendationSettings
            .Where(s => s.Version == version)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, true), cancellationToken);

        await auditService.AddAsync(
            AdminAuditAction.RecommendationSettingActivated,
            AuditTargetType,
            version.ToString(CultureInfo.InvariantCulture),
            before: previous is { } p ? $"v{p}" : "程式預設值",
            after: $"v{version}",
            cancellationToken: cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        cache.Remove(CacheKey);
        return true;
    }

    /// <inheritdoc />
    public async Task<int?> RollbackAsync(CancellationToken cancellationToken = default)
    {
        var current = await context.RecommendationSettings
            .Where(s => s.IsActive)
            .Select(s => (int?)s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (current is null)
        {
            return null;
        }

        var previous = await context.RecommendationSettings
            .Where(s => s.Version < current)
            .OrderByDescending(s => s.Version)
            .Select(s => (int?)s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (previous is not { } version)
        {
            return null;
        }

        await ActivateAsync(version, cancellationToken);
        return version;
    }

    /// <summary>
    /// 列出有變動的欄位，例如「熱度 0.5→0.7」
    /// </summary>
    /// <param name="before">修改前</param>
    /// <param name="after">修改後</param>
    /// <returns>變更摘要</returns>
    public static string Describe(RecommendationSettings before, RecommendationSettings after)
    {
        var changes = new List<string>();
        foreach (var property in typeof(RecommendationSettings).GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var oldValue = Convert.ToString(property.GetValue(before), CultureInfo.InvariantCulture);
            var newValue = Convert.ToString(property.GetValue(after), CultureInfo.InvariantCulture);
            if (oldValue != newValue)
            {
                var name = property.GetCustomAttribute<DisplayAttribute>()?.Name ?? property.Name;
                changes.Add($"{name} {oldValue}→{newValue}");
            }
        }

        return changes.Count == 0 ? "數值未變更" : string.Join("、", changes);
    }
}
