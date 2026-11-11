using DataAccess.Data;
using DataAccess.Models.StatisticalData;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Involver.Services;

/// <inheritdoc cref="ICoinService"/>
public class CoinService(ApplicationDbContext context) : ICoinService
{
    private const int NoteMaxLength = 200;

    public Guid CorrelationId { get; } = Guid.NewGuid();

    public async Task<bool> TrySpendAsync(string profileId, decimal amount, CoinTransactionInfo info, bool isVirtual = false, CancellationToken cancellationToken = default)
    {
        EnsurePositive(amount);

        await using var transaction = await BeginTransactionIfNoneAsync(cancellationToken);

        // 餘額檢查放在 WHERE 裡，與扣款在同一個 UPDATE 完成，兩個併發請求不可能都通過檢查
        var query = context.Profiles.Where(p => p.ProfileID == profileId);

        int affected = isVirtual
            ? await query
                .Where(p => p.VirtualCoins >= amount)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.VirtualCoins, p => p.VirtualCoins - amount)
                    .SetProperty(p => p.UsedCoins, p => p.UsedCoins + amount),
                    cancellationToken)
            : await query
                .Where(p => p.RealCoins >= amount)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.RealCoins, p => p.RealCoins - amount)
                    .SetProperty(p => p.UsedCoins, p => p.UsedCoins + amount),
                    cancellationToken);

        if (affected != 1)
        {
            return false;
        }

        await WriteTransactionAsync(profileId, isVirtual ? CoinType.Virtual : CoinType.Real, -amount, info, cancellationToken);
        await CommitIfOwnedAsync(transaction, cancellationToken);
        return true;
    }

    public async Task<bool> AddRealCoinsAsync(string profileId, decimal amount, CoinTransactionInfo info, CancellationToken cancellationToken = default)
    {
        EnsurePositive(amount);

        await using var transaction = await BeginTransactionIfNoneAsync(cancellationToken);

        int affected = await context.Profiles
            .Where(p => p.ProfileID == profileId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.RealCoins, p => p.RealCoins + amount),
                cancellationToken);

        return await FinishCreditAsync(transaction, affected, profileId, CoinType.Real, amount, info, cancellationToken);
    }

    public async Task<bool> AddVirtualCoinsAsync(string profileId, decimal amount, CoinTransactionInfo info, CancellationToken cancellationToken = default)
    {
        EnsurePositive(amount);

        await using var transaction = await BeginTransactionIfNoneAsync(cancellationToken);

        int affected = await context.Profiles
            .Where(p => p.ProfileID == profileId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.VirtualCoins, p => p.VirtualCoins + amount),
                cancellationToken);

        return await FinishCreditAsync(transaction, affected, profileId, CoinType.Virtual, amount, info, cancellationToken);
    }

    public async Task<bool> AddCreatorIncomeAsync(string profileId, decimal amount, CoinTransactionInfo info, CancellationToken cancellationToken = default)
    {
        EnsurePositive(amount);

        await using var transaction = await BeginTransactionIfNoneAsync(cancellationToken);

        int affected = await context.Profiles
            .Where(p => p.ProfileID == profileId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.MonthlyCoins, p => p.MonthlyCoins + amount),
                cancellationToken);

        return await FinishCreditAsync(transaction, affected, profileId, CoinType.CreatorIncome, amount, info, cancellationToken);
    }

    public async Task AddNovelCoinsAsync(int novelId, decimal amount, CancellationToken cancellationToken = default)
    {
        EnsurePositive(amount);

        await context.Novels
            .Where(n => n.NovelID == novelId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.MonthlyCoins, n => n.MonthlyCoins + amount)
                .SetProperty(n => n.TotalCoins, n => n.TotalCoins + amount),
                cancellationToken);
    }

    public async Task AddArticleCoinsAsync(int articleId, decimal amount, CancellationToken cancellationToken = default)
    {
        EnsurePositive(amount);

        await context.Articles
            .Where(a => a.ArticleID == articleId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.MonthlyCoins, a => a.MonthlyCoins + amount)
                .SetProperty(a => a.TotalCoins, a => a.TotalCoins + amount),
                cancellationToken);
    }

    /// <summary>
    /// 加值後的共同收尾：有加到值才寫流水帳並提交自己開的交易
    /// </summary>
    private async Task<bool> FinishCreditAsync(
        IDbContextTransaction transaction,
        int affected,
        string profileId,
        CoinType coinType,
        decimal amount,
        CoinTransactionInfo info,
        CancellationToken cancellationToken)
    {
        if (affected != 1)
        {
            return false;
        }

        await WriteTransactionAsync(profileId, coinType, amount, info, cancellationToken);
        await CommitIfOwnedAsync(transaction, cancellationToken);
        return true;
    }

    /// <summary>
    /// 寫入一筆流水帳。必須在更新餘額的同一個交易中呼叫：
    /// UPDATE 會鎖住該列直到交易結束，此時讀到的餘額一定是這次異動後的值。
    /// 以 SQL 直接寫入，不經過 SaveChanges，才不會順便把呼叫端還沒要存的追蹤實體寫進資料庫。
    /// </summary>
    private async Task WriteTransactionAsync(string profileId, CoinType coinType, decimal amount, CoinTransactionInfo info, CancellationToken cancellationToken)
    {
        decimal balanceAfter = await GetBalanceAsync(profileId, coinType, cancellationToken);

        string note = info.Note is { Length: > NoteMaxLength } ? info.Note[..NoteMaxLength] : info.Note;
        short? referenceType = (short?)info.ReferenceType;
        DateTime createdAt = DateTime.UtcNow;

        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO [CoinTransactions]
                ([ProfileId], [CoinType], [TransactionType], [Amount], [BalanceAfter],
                 [CounterpartyProfileId], [ReferenceType], [ReferenceId], [CorrelationId],
                 [IdempotencyKey], [Note], [CreatedAt])
            VALUES
                ({profileId}, {(byte)coinType}, {(short)info.Type}, {amount}, {balanceAfter},
                 {info.CounterpartyProfileId}, {referenceType}, {info.ReferenceId}, {CorrelationId},
                 {info.IdempotencyKey}, {note}, {createdAt})
            """, cancellationToken);
    }

    /// <summary>
    /// 從資料庫讀取指定幣別的最新餘額
    /// </summary>
    private Task<decimal> GetBalanceAsync(string profileId, CoinType coinType, CancellationToken cancellationToken)
    {
        var query = context.Profiles.Where(p => p.ProfileID == profileId);

        return coinType switch
        {
            CoinType.Real => query.Select(p => p.RealCoins).SingleAsync(cancellationToken),
            CoinType.Virtual => query.Select(p => p.VirtualCoins).SingleAsync(cancellationToken),
            CoinType.CreatorIncome => query.Select(p => p.MonthlyCoins).SingleAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(coinType), coinType, null),
        };
    }

    /// <summary>
    /// 呼叫端沒有開交易時，由服務自己開一個，讓餘額與流水帳一起成功或失敗
    /// </summary>
    /// <returns>服務自己開的交易；呼叫端已有交易時回傳 null</returns>
    private async Task<IDbContextTransaction> BeginTransactionIfNoneAsync(CancellationToken cancellationToken)
    {
        return context.Database.CurrentTransaction is null
            ? await context.Database.BeginTransactionAsync(cancellationToken)
            : null;
    }

    /// <summary>
    /// 只提交服務自己開的交易；呼叫端的交易由呼叫端決定是否提交
    /// </summary>
    private static async Task CommitIfOwnedAsync(IDbContextTransaction transaction, CancellationToken cancellationToken)
    {
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }
    }

    /// <summary>
    /// 金額必須為正數，避免負數扣款變成加值
    /// </summary>
    private static void EnsurePositive(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
    }
}
