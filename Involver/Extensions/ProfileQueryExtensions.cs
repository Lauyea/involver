using DataAccess.Data;

using Microsoft.EntityFrameworkCore;

namespace Involver.Extensions;

/// <summary>
/// 以 Profiles 為唯一來源的使用者狀態查詢
/// </summary>
public static class ProfileQueryExtensions
{
    /// <summary>
    /// 使用者是否被封鎖
    /// </summary>
    /// <param name="context">資料庫</param>
    /// <param name="profileId">使用者 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>被封鎖時為 true；找不到 Profile 時為 false</returns>
    public static Task<bool> IsBannedAsync(this ApplicationDbContext context, string profileId, CancellationToken cancellationToken = default)
    {
        return context.Profiles
            .AnyAsync(p => p.ProfileID == profileId && p.Banned, cancellationToken);
    }

    /// <summary>
    /// 使用者是否為會員（Prime）
    /// </summary>
    /// <param name="context">資料庫</param>
    /// <param name="profileId">使用者 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>是會員時為 true；未登入或找不到 Profile 時為 false</returns>
    public static Task<bool> IsPrimeAsync(this ApplicationDbContext context, string profileId, CancellationToken cancellationToken = default)
    {
        if (profileId == null)
        {
            return Task.FromResult(false);
        }

        return context.Profiles
            .AnyAsync(p => p.ProfileID == profileId && p.Prime, cancellationToken);
    }
}
