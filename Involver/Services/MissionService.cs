using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.StatisticalData;

using Microsoft.EntityFrameworkCore;

namespace Involver.Services;

/// <inheritdoc cref="IMissionService"/>
public class MissionService(ApplicationDbContext context, ICoinService coinService) : IMissionService
{
    public async Task<bool> CompleteAsync(string profileId, MissionType mission, CancellationToken cancellationToken = default)
    {
        string property = mission.ToString();

        // 任務旗標與獎勵要一起成功或失敗
        await using var transaction = context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(cancellationToken)
            : null;

        // 只有把旗標從 false 改成 true 的那個請求能領獎勵
        int affected = await context.Missions
            .Where(m => m.ProfileID == profileId && !EF.Property<bool>(m, property))
            .ExecuteUpdateAsync(s => s.SetProperty(m => EF.Property<bool>(m, property), true), cancellationToken);

        if (affected == 0)
        {
            return false;
        }

        bool isDailyLogin = mission == MissionType.DailyLogin;
        await coinService.AddVirtualCoinsAsync(
            profileId,
            Parameters.MissionAwardCoins,
            new CoinTransactionInfo(
                isDailyLogin ? CoinTransactionType.LoginReward : CoinTransactionType.MissionReward,
                Note: isDailyLogin ? "每日登入" : $"每週任務：{mission}"),
            cancellationToken);

        if (!isDailyLogin)
        {
            await CompleteAllMissionsAsync(profileId, cancellationToken);
        }

        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return true;
    }

    /// <summary>
    /// 所有每週任務都完成時，發放一次全勤獎勵
    /// </summary>
    private async Task CompleteAllMissionsAsync(string profileId, CancellationToken cancellationToken)
    {
        int affected = await context.Missions
            .Where(m => m.ProfileID == profileId
                && m.WatchArticle
                && m.Vote
                && m.LeaveComment
                && m.ViewAnnouncement
                && m.ShareCreation
                && m.BeAgreed
                && !m.CompleteOtherMissions)
            .ExecuteUpdateAsync(s => s.SetProperty(m => m.CompleteOtherMissions, true), cancellationToken);

        if (affected == 1)
        {
            await coinService.AddVirtualCoinsAsync(
                profileId,
                Parameters.MissionAwardCoins,
                new CoinTransactionInfo(CoinTransactionType.MissionReward, Note: "每週任務：完成其他所有每週任務"),
                cancellationToken);
        }
    }
}
