using DataAccess.Data;
using DataAccess.Models.Dashboard;

using Involver.Models.ViewModels.Dashboard;
using Involver.Services.Admin;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Creator;

/// <summary>
/// 創作者中心 B-2 單一作品的趨勢圖（讀每日快照 <see cref="DailyContentStat"/>）。只能看自己的作品
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="userManager">帳號管理</param>
public class WorkModel(ApplicationDbContext context, UserManager<InvolverUser> userManager) : CreatorPageModel(userManager)
{
    /// <summary>
    /// 內容類型
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public ContentStatType Type { get; set; } = ContentStatType.Novel;

    /// <summary>
    /// 作品或文章 ID
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int Id { get; set; }

    /// <summary>
    /// 標題
    /// </summary>
    public string Title { get; private set; }

    /// <summary>
    /// 期間合計
    /// </summary>
    public List<KpiCardViewModel> Cards { get; private set; } = [];

    /// <summary>
    /// 瀏覽與讀者趨勢
    /// </summary>
    public ChartPanelViewModel ViewsChart { get; private set; }

    /// <summary>
    /// 互動與收入趨勢
    /// </summary>
    public ChartPanelViewModel EngagementChart { get; private set; }

    /// <summary>
    /// 載入作品趨勢
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>頁面；不是自己的作品時 404</returns>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var me = UserId;
        Title = Type == ContentStatType.Novel
            ? await context.Novels.Where(n => n.NovelID == Id && n.ProfileID == me).Select(n => n.Title).FirstOrDefaultAsync(cancellationToken)
            : await context.Articles.Where(a => a.ArticleID == Id && a.ProfileID == me).Select(a => a.Title).FirstOrDefaultAsync(cancellationToken);

        // 不是自己的作品一律當作不存在，不透露別人的作品是否存在
        if (Title == null)
        {
            return NotFound();
        }

        var today = DateOnly.FromDateTime(DateTime.Now);
        var dates = DashboardSeries.Dates(today.AddDays(-Days), today.AddDays(-1));
        var stats = await context.DailyContentStats
            .Where(s => s.ContentType == Type && s.ContentId == Id && s.Date >= dates[0])
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Date, cancellationToken);

        List<decimal> Series(Func<DailyContentStat, decimal> selector) =>
            dates.Select(d => stats.TryGetValue(d, out var s) ? selector(s) : 0).ToList();

        var views = Series(s => s.Views);
        var readers = Series(s => s.UniqueReaders);
        var follows = Series(s => s.NewFollows);
        var comments = Series(s => s.NewComments);
        var coins = Series(s => s.CoinsReceived);
        var hasData = stats.Count > 0;

        Cards =
        [
            new KpiCardViewModel { Label = $"{Days} 天瀏覽", Value = views.Sum(), Icon = "fa-eye" },
            new KpiCardViewModel { Label = "每日不重複讀者合計", Value = readers.Sum(), Icon = "fa-user-check" },
            new KpiCardViewModel { Label = "新追蹤", Value = follows.Sum(), Icon = "fa-bookmark" },
            new KpiCardViewModel { Label = "新留言", Value = comments.Sum(), Icon = "fa-comments" },
            new KpiCardViewModel { Label = "收到 In 幣", Value = coins.Sum(), Suffix = "In", Icon = "fa-coins" },
        ];

        var labels = DashboardSeries.Labels(dates);
        const string emptyText = "這段期間沒有快照資料（快照每天午夜寫入前一天）";

        ViewsChart = new ChartPanelViewModel
        {
            Title = "瀏覽與讀者",
            EmptyText = emptyText,
            Config = new ChartConfigViewModel
            {
                Type = "line",
                Labels = labels,
                Datasets = hasData ?
                [
                    new ChartDatasetViewModel { Label = "瀏覽", Data = views, ColorIndex = 0, Fill = true },
                    new ChartDatasetViewModel { Label = "不重複讀者", Data = readers, ColorIndex = 1 },
                ] : [],
            },
        };

        EngagementChart = new ChartPanelViewModel
        {
            Title = "互動與收到的 In 幣",
            EmptyText = emptyText,
            Config = new ChartConfigViewModel
            {
                Type = "bar",
                Labels = labels,
                YTitle = "次數",
                Y1Title = "In 幣",
                Datasets = hasData ?
                [
                    new ChartDatasetViewModel { Label = "新追蹤", Data = follows, ColorIndex = 2 },
                    new ChartDatasetViewModel { Label = "新留言", Data = comments, ColorIndex = 3 },
                    new ChartDatasetViewModel { Label = "收到 In 幣", Data = coins, ColorIndex = 0, Type = "line", YAxisId = "y1" },
                ] : [],
            },
        };

        return Page();
    }
}
