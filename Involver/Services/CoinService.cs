using DataAccess.Data;

using Microsoft.EntityFrameworkCore;

namespace Involver.Services;

/// <inheritdoc cref="ICoinService"/>
public class CoinService(ApplicationDbContext context) : ICoinService
{
    public async Task<bool> TrySpendAsync(string profileId, decimal amount, bool isVirtual = false, CancellationToken cancellationToken = default)
    {
        EnsurePositive(amount);

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

        return affected == 1;
    }

    public async Task<bool> AddRealCoinsAsync(string profileId, decimal amount, CancellationToken cancellationToken = default)
    {
        EnsurePositive(amount);

        int affected = await context.Profiles
            .Where(p => p.ProfileID == profileId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.RealCoins, p => p.RealCoins + amount),
                cancellationToken);

        return affected == 1;
    }

    public async Task AddVirtualCoinsAsync(string profileId, decimal amount, CancellationToken cancellationToken = default)
    {
        EnsurePositive(amount);

        await context.Profiles
            .Where(p => p.ProfileID == profileId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.VirtualCoins, p => p.VirtualCoins + amount),
                cancellationToken);
    }

    public async Task AddCreatorIncomeAsync(string profileId, decimal amount, CancellationToken cancellationToken = default)
    {
        EnsurePositive(amount);

        await context.Profiles
            .Where(p => p.ProfileID == profileId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.MonthlyCoins, p => p.MonthlyCoins + amount),
                cancellationToken);
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
    /// 金額必須為正數，避免負數扣款變成加值
    /// </summary>
    private static void EnsurePositive(decimal amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
    }
}
