using DataAccess.Data;
using DataAccess.Models.Recommendation;

using Microsoft.EntityFrameworkCore;

namespace Involver.Services.Recommendations;

/// <summary>
/// 章節閱讀紀錄（<see cref="EpisodeRead"/>）：進入章節時記錄開始閱讀，看到結尾時標記讀完
/// </summary>
public interface IEpisodeReadService
{
    /// <summary>
    /// 記錄會員開始（或再次）閱讀某章節
    /// </summary>
    /// <param name="profileId">會員</param>
    /// <param name="episodeId">章節 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task RecordOpenAsync(string profileId, int episodeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 標記會員讀到章節結尾
    /// </summary>
    /// <param name="profileId">會員</param>
    /// <param name="episodeId">章節 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>章節不存在、已刪除，或會員還沒開啟過這個章節時回傳 false</returns>
    Task<bool> MarkReachedEndAsync(string profileId, int episodeId, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class EpisodeReadService(ApplicationDbContext context, ILogger<EpisodeReadService> logger) : IEpisodeReadService
{
    /// <inheritdoc />
    public async Task RecordOpenAsync(string profileId, int episodeId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(profileId);
        var now = DateTime.Now;

        var updated = await context.EpisodeReads
            .Where(r => r.ProfileId == profileId && r.EpisodeId == episodeId)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.LastReadAt, now), cancellationToken);

        if (updated == 0)
        {
            await InsertAsync(new EpisodeRead
            {
                ProfileId = profileId,
                EpisodeId = episodeId,
                FirstReadAt = now,
                LastReadAt = now,
            }, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<bool> MarkReachedEndAsync(string profileId, int episodeId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(profileId);

        var exists = await context.Episodes
            .AnyAsync(e => e.EpisodeID == episodeId && !e.IsDeleted, cancellationToken);
        if (!exists)
        {
            return false;
        }

        var now = DateTime.Now;
        var updated = await context.EpisodeReads
            .Where(r => r.ProfileId == profileId && r.EpisodeId == episodeId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.ReachedEnd, true)
                .SetProperty(r => r.ReachedEndAt, r => r.ReachedEndAt ?? now)
                .SetProperty(r => r.LastReadAt, now),
                cancellationToken);

        // 只接受開過的章節（開啟章節頁時伺服器已寫入紀錄），避免直接呼叫 API 灌高讀完率
        return updated > 0;
    }

    /// <summary>
    /// 新增一筆紀錄。同時有兩個請求新增同一筆時，唯一索引會擋下第二筆；那一筆的資訊已經由第一筆記下，直接忽略
    /// </summary>
    private async Task InsertAsync(EpisodeRead read, CancellationToken cancellationToken)
    {
        context.EpisodeReads.Add(read);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogDebug(ex, "Episode read for {EpisodeId} was inserted concurrently.", read.EpisodeId);
        }
        finally
        {
            context.Entry(read).State = EntityState.Detached;
        }
    }
}
