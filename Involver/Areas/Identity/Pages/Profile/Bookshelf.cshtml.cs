using DataAccess.Data;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;
using DataAccess.Models.StatisticalData;
using DataAccess.Services;

using Involver.Common;
using Involver.Models.ViewModels.Dashboard;
using Involver.Models.ViewModels.Profiles;
using Involver.Services;
using Involver.Services.Profiles;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile;

/// <summary>
/// 我的書房：追蹤作品的閱讀進度（書架）、讀者數據、支持過的作者。只有本人看得到
/// </summary>
[Authorize]
public class BookshelfModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService,
    IProfileReadingService readingService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    /// <summary>
    /// 支持過的作者最多顯示幾位
    /// </summary>
    public const int SupportedCreatorCount = 10;

    /// <summary>
    /// 支持過的作者
    /// </summary>
    /// <param name="ProfileId">作者</param>
    /// <param name="UserName">作者用戶名</param>
    /// <param name="Total">累計支持的 In 幣</param>
    /// <param name="Monthly">本月支持的 In 幣</param>
    public record SupportedCreator(string ProfileId, string UserName, decimal Total, decimal Monthly);

    /// <summary>
    /// 個人檔案
    /// </summary>
    public DataAccess.Models.Profile Profile { get; set; }

    /// <summary>
    /// 書架，依閱讀狀態分組（閱讀中、未開始、已讀完）
    /// </summary>
    public List<IGrouping<BookshelfStatus, BookshelfItemViewModel>> Shelves { get; set; } = [];

    /// <summary>
    /// 讀者數據：本月花費、近 30 天看過的作品與文章
    /// </summary>
    public List<KpiCardViewModel> ReaderCards { get; set; } = [];

    /// <summary>
    /// 支持過的作者，依累計金額由多到少
    /// </summary>
    public List<SupportedCreator> SupportedCreators { get; set; } = [];

    /// <summary>
    /// 顯示書房
    /// </summary>
    /// <param name="id">ProfileID；省略時為登入者本人</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>頁面；不是本人時 403</returns>
    public async Task<IActionResult> OnGetAsync(string id, CancellationToken cancellationToken)
    {
        var me = UserManager.GetUserId(User);
        id ??= me;
        if (id != me)
        {
            return Forbid();
        }

        Profile = await Context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id, cancellationToken);
        if (Profile == null)
        {
            return NotFound();
        }

        var bookshelf = await readingService.GetBookshelfAsync(id, cancellationToken);
        Shelves = bookshelf.GroupBy(b => b.Status).OrderBy(g => g.Key).ToList();

        await LoadReaderStatsAsync(id, cancellationToken);
        return Page();
    }

    /// <summary>
    /// 讀者數據（本月花費、近 30 天看過的作品與文章）與支持過的作者（原本在創作者中心的「讀者視角」）
    /// </summary>
    /// <param name="me">登入者 ProfileID</param>
    /// <param name="cancellationToken">取消權杖</param>
    private async Task LoadReaderStatsAsync(string me, CancellationToken cancellationToken)
    {
        var now = DateTime.Now;
        var thisMonthUtc = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Local).ToUniversalTime();
        var spending = await Context.CoinTransactions
            .Where(t => t.ProfileId == me
                && t.Amount < 0
                && DashboardMetrics.SupportTypes.Contains(t.TransactionType)
                && t.CreatedAt >= thisMonthUtc)
            .Select(t => new { t.CoinType, t.Amount })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var since = now.AddDays(-DashboardMetrics.MonthlyDays);
        var novelsRead = await Context.Set<NovelViewer>()
            .CountAsync(v => v.ProfileID == me && v.ViewDate >= since, cancellationToken);
        var articlesRead = await Context.Set<ArticleViewer>()
            .CountAsync(v => v.ProfileID == me && v.ViewDate >= since, cancellationToken);

        ReaderCards =
        [
            new KpiCardViewModel { Label = $"近 {DashboardMetrics.MonthlyDays} 天看過的作品", Value = novelsRead, Icon = "fa-book-open-reader" },
            new KpiCardViewModel { Label = $"近 {DashboardMetrics.MonthlyDays} 天看過的文章", Value = articlesRead, Icon = "fa-newspaper" },
            new KpiCardViewModel { Label = "本月花費（虛擬 In 幣）", Value = -spending.Where(s => s.CoinType == CoinType.Virtual).Sum(s => s.Amount), Suffix = "In", Icon = "fa-gift" },
            new KpiCardViewModel { Label = "本月花費（實體 In 幣）", Value = -spending.Where(s => s.CoinType == CoinType.Real).Sum(s => s.Amount), Suffix = "In", Icon = "fa-wallet" },
        ];

        var supported = await Context.CoinTransactions
            .Where(t => t.ProfileId == me
                && t.Amount < 0
                && t.CounterpartyProfileId != null
                && t.CounterpartyProfileId != me
                && DashboardMetrics.SupportTypes.Contains(t.TransactionType))
            .GroupBy(t => t.CounterpartyProfileId)
            .Select(g => new
            {
                CreatorId = g.Key,
                Total = -g.Sum(t => t.Amount),
                Monthly = -g.Where(t => t.CreatedAt >= thisMonthUtc).Sum(t => t.Amount),
            })
            .OrderByDescending(g => g.Total)
            .Take(SupportedCreatorCount)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var ids = supported.Select(s => s.CreatorId).ToList();
        var names = await Context.Profiles
            .Where(p => ids.Contains(p.ProfileID))
            .Select(p => new { p.ProfileID, p.UserName })
            .ToDictionaryAsync(p => p.ProfileID, p => p.UserName, cancellationToken);
        SupportedCreators = supported
            .Select(s => new SupportedCreator(s.CreatorId, names.GetValueOrDefault(s.CreatorId, "（已刪除）"), s.Total, s.Monthly))
            .ToList();
    }
}
