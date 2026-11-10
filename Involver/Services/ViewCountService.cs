using System.Linq.Expressions;

using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;

using Microsoft.EntityFrameworkCore;

namespace Involver.Services;

/// <inheritdoc cref="IViewCountService"/>
public class ViewCountService(ApplicationDbContext context) : IViewCountService
{
    public async Task AddNovelViewAsync(Novel novel, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(novel);

        await context.Novels
            .Where(n => n.NovelID == novel.NovelID)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.TotalViews, n => n.TotalViews + 1)
                .SetProperty(n => n.DailyView, n => n.DailyView + 1),
                cancellationToken);

        IncrementInMemory(novel, n => n.TotalViews);
        IncrementInMemory(novel, n => n.DailyView);
    }

    public async Task AddArticleViewAsync(Article article, bool countDaily = true, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(article);

        var query = context.Articles.Where(a => a.ArticleID == article.ArticleID);

        if (countDaily)
        {
            await query.ExecuteUpdateAsync(s => s
                .SetProperty(a => a.TotalViews, a => a.TotalViews + 1)
                .SetProperty(a => a.DailyView, a => a.DailyView + 1),
                cancellationToken);
        }
        else
        {
            await query.ExecuteUpdateAsync(s => s
                .SetProperty(a => a.TotalViews, a => a.TotalViews + 1),
                cancellationToken);
        }

        IncrementInMemory(article, a => a.TotalViews);
        if (countDaily)
        {
            IncrementInMemory(article, a => a.DailyView);
        }
    }

    public async Task AddEpisodeViewAsync(Episode episode, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(episode);

        await context.Episodes
            .Where(e => e.EpisodeID == episode.EpisodeID)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Views, e => e.Views + 1),
                cancellationToken);

        IncrementInMemory(episode, e => e.Views);
    }

    public async Task AddProfileViewAsync(Profile profile, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(profile);

        await context.Profiles
            .Where(p => p.ProfileID == profile.ProfileID)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Views, p => p.Views + 1),
                cancellationToken);

        IncrementInMemory(profile, p => p.Views);
    }

    /// <summary>
    /// 把記憶體中的計數 +1 供頁面顯示。
    /// 實體若被追蹤，會同步更新原始值並取消修改標記，避免之後的 SaveChanges 用這個值整欄寫回資料庫。
    /// </summary>
    /// <typeparam name="TEntity">實體型別</typeparam>
    /// <param name="entity">實體</param>
    /// <param name="property">計數欄位</param>
    private void IncrementInMemory<TEntity>(TEntity entity, Expression<Func<TEntity, int>> property)
        where TEntity : class
    {
        var entry = context.Entry(entity);
        var propertyEntry = entry.Property(property);
        var newValue = propertyEntry.CurrentValue + 1;

        propertyEntry.CurrentValue = newValue;

        if (entry.State != EntityState.Detached)
        {
            propertyEntry.OriginalValue = newValue;
            propertyEntry.IsModified = false;
        }
    }
}
