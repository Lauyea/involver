using DataAccess.Data;
using DataAccess.Models.Dashboard;
using DataAccess.Models.StatisticalData;

using Microsoft.EntityFrameworkCore;

namespace Involver.Services.Admin;

/// <summary>
/// 一位創作者的分潤撥款狀態
/// </summary>
public class CreatorPayoutSummary
{
    /// <summary>
    /// 創作者 ProfileID
    /// </summary>
    public string ProfileId { get; init; }

    /// <summary>
    /// 用戶名
    /// </summary>
    public string UserName { get; init; }

    /// <summary>
    /// 待撥的月份（已月結、還沒有撥款紀錄），由舊到新
    /// </summary>
    public List<DateOnly> PendingMonths { get; init; } = [];

    /// <summary>
    /// 待撥金額
    /// </summary>
    public decimal PendingAmount { get; init; }

    /// <summary>
    /// 已撥金額
    /// </summary>
    public decimal PaidAmount { get; init; }

    /// <summary>
    /// 最後一次撥款時間（UTC）
    /// </summary>
    public DateTime? LastPaidAt { get; init; }

    /// <summary>
    /// 本月累計中、尚未月結的收入
    /// </summary>
    public decimal CurrentMonthIncome { get; init; }
}

/// <summary>
/// 標記撥款的結果
/// </summary>
/// <param name="Months">這次標記的月份數</param>
/// <param name="Amount">這次標記的金額</param>
public sealed record PayoutMarkResult(int Months, decimal Amount);

/// <summary>
/// 創作者分潤撥款。每月排程把創作者收入彙總到 <see cref="CoinMonthlyBalance"/>（CreatorIncome），
/// 該月的收入合計（Credits）就是應撥金額；標記撥款時每個月份寫入一筆 <see cref="CreatorPayout"/>。
/// </summary>
public interface IPayoutService
{
    /// <summary>
    /// 列出所有有收入的創作者的待撥與已撥金額，待撥多的排前面
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>撥款狀態</returns>
    Task<List<CreatorPayoutSummary>> GetSummariesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 把創作者所有待撥月份標記為已撥款，並寫入管理者操作紀錄
    /// </summary>
    /// <param name="profileId">創作者 ProfileID</param>
    /// <param name="note">備註（例如轉帳編號）</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>標記結果；沒有待撥月份時為 0 個月</returns>
    Task<PayoutMarkResult> MarkPaidAsync(string profileId, string note, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class PayoutService(ApplicationDbContext context, IAdminAuditService auditService) : IPayoutService
{
    /// <summary>
    /// 撥款備註的長度上限，與資料表欄位一致
    /// </summary>
    private const int NoteMaxLength = 200;

    /// <inheritdoc />
    public async Task<List<CreatorPayoutSummary>> GetSummariesAsync(CancellationToken cancellationToken = default)
    {
        // decimal 在記憶體加總（SQLite 無法加總 decimal）；資料量是創作者數 × 月數
        var incomes = await context.CoinMonthlyBalances
            .Where(b => b.CoinType == CoinType.CreatorIncome && b.Credits > 0)
            .Select(b => new { b.ProfileId, b.Month, b.Credits })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var payouts = await context.CreatorPayouts
            .Select(p => new { p.ProfileId, p.Month, p.Amount, p.PaidAt })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var current = await context.Profiles
            .Where(p => p.MonthlyCoins > 0)
            .Select(p => new { p.ProfileID, p.MonthlyCoins })
            .AsNoTracking()
            .ToDictionaryAsync(p => p.ProfileID, p => p.MonthlyCoins, cancellationToken);

        var paidMonths = payouts.Select(p => (p.ProfileId, p.Month)).ToHashSet();

        var profileIds = incomes.Select(i => i.ProfileId)
            .Concat(payouts.Select(p => p.ProfileId))
            .Concat(current.Keys)
            .Distinct()
            .ToList();

        var names = await context.Profiles
            .Where(p => profileIds.Contains(p.ProfileID))
            .Select(p => new { p.ProfileID, p.UserName })
            .AsNoTracking()
            .ToDictionaryAsync(p => p.ProfileID, p => p.UserName, cancellationToken);

        return profileIds
            .Select(id =>
            {
                var pending = incomes
                    .Where(i => i.ProfileId == id && !paidMonths.Contains((id, i.Month)))
                    .OrderBy(i => i.Month)
                    .ToList();
                var paid = payouts.Where(p => p.ProfileId == id).ToList();

                return new CreatorPayoutSummary
                {
                    ProfileId = id,
                    UserName = names.GetValueOrDefault(id, id),
                    PendingMonths = pending.Select(i => i.Month).ToList(),
                    PendingAmount = pending.Sum(i => i.Credits),
                    PaidAmount = paid.Sum(p => p.Amount),
                    LastPaidAt = paid.Count > 0 ? paid.Max(p => p.PaidAt) : null,
                    CurrentMonthIncome = current.GetValueOrDefault(id),
                };
            })
            .OrderByDescending(s => s.PendingAmount)
            .ThenByDescending(s => s.CurrentMonthIncome)
            .ThenBy(s => s.UserName, StringComparer.Ordinal)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<PayoutMarkResult> MarkPaidAsync(string profileId, string note, CancellationToken cancellationToken = default)
    {
        // 備註欄位最長 200 字，不能只靠瀏覽器的 maxlength
        note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (note is { Length: > NoteMaxLength })
        {
            note = note[..NoteMaxLength];
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var paidMonths = await context.CreatorPayouts
            .Where(p => p.ProfileId == profileId)
            .Select(p => p.Month)
            .ToListAsync(cancellationToken);

        var pending = (await context.CoinMonthlyBalances
            .Where(b => b.ProfileId == profileId && b.CoinType == CoinType.CreatorIncome && b.Credits > 0)
            .Select(b => new { b.Month, b.Credits })
            .ToListAsync(cancellationToken))
            .Where(b => !paidMonths.Contains(b.Month))
            .OrderBy(b => b.Month)
            .ToList();

        if (pending.Count == 0)
        {
            return new PayoutMarkResult(0, 0);
        }

        var paidAt = DateTime.UtcNow;
        var actorId = (await auditService.AddAsync(
            AdminAuditAction.PayoutMarkedPaid,
            "User",
            profileId,
            before: $"待撥 {pending.Count} 個月：{string.Join("、", pending.Select(p => $"{p.Month:yyyy-MM} {p.Credits:N0}"))}",
            after: $"已撥 {pending.Sum(p => p.Credits):N0}" + (note == null ? string.Empty : $"（{note}）"),
            cancellationToken: cancellationToken)).ActorProfileId;

        context.CreatorPayouts.AddRange(pending.Select(p => new CreatorPayout
        {
            ProfileId = profileId,
            Month = p.Month,
            Amount = p.Credits,
            PaidAt = paidAt,
            PaidByProfileId = actorId,
            Note = note,
        }));

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new PayoutMarkResult(pending.Count, pending.Sum(p => p.Credits));
    }
}
