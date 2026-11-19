using DataAccess.Common;
using DataAccess.Data;

using Involver.Authorization.Admin;
using Involver.Models.ViewModels.Dashboard;
using Involver.Pages.Novels;
using Involver.Services.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Admin.Pages.ContentStats;

/// <summary>
/// A-3 內容：每日新增的作品、章節、文章、留言；投票模式比例；主分類分布；被封鎖的內容
/// </summary>
/// <param name="context">資料庫內容</param>
[Authorize(Policy = AdminPolicies.Content)]
public class IndexModel(ApplicationDbContext context) : AdminPageModel
{
    /// <summary>
    /// 被封鎖內容列表每種最多幾筆
    /// </summary>
    public const int BlockedLimit = 20;

    /// <summary>
    /// 被封鎖的內容
    /// </summary>
    /// <param name="Kind">種類</param>
    /// <param name="Title">標題或留言摘要</param>
    /// <param name="Url">內容頁網址</param>
    /// <param name="UpdateTime">更新時間</param>
    public record BlockedItem(string Kind, string Title, string Url, DateTime UpdateTime);

    /// <summary>
    /// 每日新增內容
    /// </summary>
    public ChartPanelViewModel DailyChart { get; private set; }

    /// <summary>
    /// 投票模式比例（投票數）
    /// </summary>
    public ChartPanelViewModel PolicyVotesChart { get; private set; }

    /// <summary>
    /// 投票模式比例（投入 In 幣）
    /// </summary>
    public ChartPanelViewModel PolicyCoinsChart { get; private set; }

    /// <summary>
    /// 小說的分類分布
    /// </summary>
    public ChartPanelViewModel NovelCategoryChart { get; private set; }

    /// <summary>
    /// 一般文章的分類分布
    /// </summary>
    public ChartPanelViewModel ArticleCategoryChart { get; private set; }

    /// <summary>
    /// 期間內各項新增的合計
    /// </summary>
    public List<KpiCardViewModel> Cards { get; private set; } = [];

    /// <summary>
    /// 被封鎖的內容
    /// </summary>
    public List<BlockedItem> BlockedItems { get; private set; } = [];

    /// <summary>
    /// 載入內容統計
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var dates = DashboardSeries.Dates(today.AddDays(-Days), today.AddDays(-1));

        // 章節、留言沒有建立時間，由前後兩天的總數相減，所以多抓前一天
        var snapshots = await context.DailySiteStats
            .Where(s => s.Date >= dates[0].AddDays(-1))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var newNovels = DashboardSeries.Values(snapshots, dates, s => s.NewNovels);
        var newEpisodes = DashboardSeries.Deltas(snapshots, dates, s => s.Episodes);
        var newArticles = DashboardSeries.Values(snapshots, dates, s => s.NewArticles);
        var newComments = DashboardSeries.Deltas(snapshots, dates, s => s.Comments);

        Cards =
        [
            new KpiCardViewModel { Label = $"{Days} 日新作品", Value = newNovels.Sum(), Icon = "fa-book" },
            new KpiCardViewModel { Label = $"{Days} 日新章節", Value = newEpisodes.Sum(), Icon = "fa-file-lines" },
            new KpiCardViewModel { Label = $"{Days} 日新文章", Value = newArticles.Sum(), Icon = "fa-newspaper" },
            new KpiCardViewModel { Label = $"{Days} 日新留言", Value = newComments.Sum(), Icon = "fa-comments" },
        ];

        DailyChart = new ChartPanelViewModel
        {
            Title = "每日新增內容",
            Subtitle = "章節、留言由每日總數相減",
            EmptyText = "每日快照開始寫入後才會有趨勢資料",
            Config = new ChartConfigViewModel
            {
                Type = "line",
                Labels = DashboardSeries.Labels(dates),
                Datasets = snapshots.Count == 0 ? [] :
                [
                    new ChartDatasetViewModel { Label = "新作品", Data = newNovels, ColorIndex = 0 },
                    new ChartDatasetViewModel { Label = "新章節", Data = newEpisodes, ColorIndex = 1 },
                    new ChartDatasetViewModel { Label = "新文章", Data = newArticles, ColorIndex = 2 },
                    new ChartDatasetViewModel { Label = "新留言", Data = newComments, ColorIndex = 3 },
                ],
            },
        };

        await LoadVotingPolicyAsync(cancellationToken);
        await LoadCategoriesAsync(cancellationToken);
        await LoadBlockedAsync(cancellationToken);
    }

    private async Task LoadVotingPolicyAsync(CancellationToken cancellationToken)
    {
        var since = DateTime.Now.AddDays(-Days);

        // 只有一般選項記錄票；依投票建立時間篩選期間
        var policies = await context.Votes
            .Where(v => v.NormalOption != null && v.NormalOption.Voting!.CreateTime >= since)
            .GroupBy(v => v.NormalOption!.Voting!.Policy)
            .Select(g => new { Policy = g.Key, Count = g.Count(), Coins = g.Sum(v => v.Value) })
            .ToListAsync(cancellationToken);

        var labels = new List<string> { "自由模式", "平等模式" };
        var liberty = policies.FirstOrDefault(p => p.Policy == PolicyType.Liberty);
        var equality = policies.FirstOrDefault(p => p.Policy == PolicyType.Equality);
        var hasData = policies.Count > 0;

        PolicyVotesChart = Doughnut("投票模式：投票數", $"近 {Days} 天建立的投票", labels, hasData,
            [liberty?.Count ?? 0, equality?.Count ?? 0]);
        PolicyCoinsChart = Doughnut("投票模式：投入 In 幣", $"近 {Days} 天建立的投票", labels, hasData,
            [liberty?.Coins ?? 0, equality?.Coins ?? 0]);
    }

    private async Task LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        // 分類就是標籤；只算適用該作品類型的分類。一部作品可以有多個分類，所以各區塊加總會大於作品數
        var counts = await context.Tags
            .Where(t => t.IsCategory)
            .Select(t => new
            {
                t.Name,
                t.CategoryOrder,
                t.CategoryScope,
                Novels = t.Novels!.Count(n => !n.IsDeleted),
                Articles = t.Articles!.Count(a => !a.IsDeleted && a.Type == ArticleType.General),
            })
            .ToListAsync(cancellationToken);

        var uncategorizedNovels = await context.Novels
            .CountAsync(n => !n.IsDeleted && !n.Tags!.Any(t => t.IsCategory && (t.CategoryScope & CategoryScope.Novel) != 0), cancellationToken);
        var uncategorizedArticles = await context.Articles
            .CountAsync(a => !a.IsDeleted && a.Type == ArticleType.General && !a.Tags!.Any(t => t.IsCategory && (t.CategoryScope & CategoryScope.Article) != 0), cancellationToken);

        NovelCategoryChart = CategoryDoughnut("小說的分類分布", "目前的小說（不含已刪除；一部可有多個分類）",
            counts.Where(c => c.CategoryScope.HasFlag(CategoryScope.Novel)).Select(c => (c.Name, c.CategoryOrder, c.Novels)), uncategorizedNovels);
        ArticleCategoryChart = CategoryDoughnut("文章的分類分布", "目前的一般文章（不含已刪除；一篇可有多個分類）",
            counts.Where(c => c.CategoryScope.HasFlag(CategoryScope.Article)).Select(c => (c.Name, c.CategoryOrder, c.Articles)), uncategorizedArticles);
    }

    private static ChartPanelViewModel CategoryDoughnut(
        string title,
        string subtitle,
        IEnumerable<(string Name, int Order, int Count)> categories,
        int uncategorized)
    {
        var ordered = categories
            .Where(c => c.Count > 0)
            .OrderByDescending(c => c.Count)
            .ThenBy(c => c.Order)
            .Select(c => (c.Name, c.Count))
            .ToList();
        if (uncategorized > 0)
        {
            ordered.Add(("未分類", uncategorized));
        }

        return Doughnut(title, subtitle,
            ordered.Select(c => c.Name).ToList(),
            ordered.Count > 0,
            ordered.Select(c => (decimal)c.Count).ToList());
    }

    private async Task LoadBlockedAsync(CancellationToken cancellationToken)
    {
        var novels = await context.Novels
            .Where(n => n.Block && !n.IsDeleted)
            .OrderByDescending(n => n.UpdateTime)
            .Take(BlockedLimit)
            .Select(n => new { n.NovelID, n.Title, n.UpdateTime })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var articles = await context.Articles
            .Where(a => a.Block && !a.IsDeleted)
            .OrderByDescending(a => a.UpdateTime)
            .Take(BlockedLimit)
            .Select(a => new { a.ArticleID, a.Title, a.Type, a.UpdateTime })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var comments = await context.Comments
            .Where(c => c.Block)
            .OrderByDescending(c => c.UpdateTime)
            .Take(BlockedLimit)
            .Select(c => new
            {
                c.CommentID,
                c.Content,
                c.UpdateTime,
                c.NovelID,
                c.ArticleID,
                c.EpisodeID,
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        BlockedItems =
        [
            .. novels.Select(n => new BlockedItem("作品", n.Title, Url.Page("/Novels/Details", new { area = "", id = n.NovelID }), n.UpdateTime)),
            .. articles.Select(a => new BlockedItem(
                a.Type == ArticleType.Feedback ? "意見回饋" : a.Type == ArticleType.Announcement ? "公告" : "文章",
                a.Title,
                Url.Page(a.Type == ArticleType.Feedback ? "/Feedbacks/Details" : a.Type == ArticleType.Announcement ? "/Announcements/Details" : "/Articles/Details", new { area = "", id = a.ArticleID }),
                a.UpdateTime)),
            .. comments.Select(c => new BlockedItem(
                "留言",
                Summarize(c.Content),
                c.NovelID != null ? Url.Page("/Novels/Details", new { area = "", id = c.NovelID })
                    : c.EpisodeID != null ? Url.Page("/Episodes/Details", new { area = "", id = c.EpisodeID })
                    : Url.Page("/Articles/Details", new { area = "", id = c.ArticleID }),
                c.UpdateTime)),
        ];
        BlockedItems = BlockedItems.OrderByDescending(b => b.UpdateTime).ToList();
    }

    /// <summary>
    /// 留言內容是 HTML，取前幾個字當摘要
    /// </summary>
    private static string Summarize(string html)
    {
        var text = System.Text.RegularExpressions.Regex.Replace(html ?? string.Empty, "<[^>]+>", " ");
        text = System.Net.WebUtility.HtmlDecode(text).Trim();
        return text.Length > 40 ? text[..40] + "…" : text;
    }

    private static ChartPanelViewModel Doughnut(string title, string subtitle, List<string> labels, bool hasData, List<decimal> data)
    {
        return new ChartPanelViewModel
        {
            Title = title,
            Subtitle = subtitle,
            Small = true,
            Config = new ChartConfigViewModel
            {
                Type = "doughnut",
                Labels = labels,
                Datasets = hasData ? [new ChartDatasetViewModel { Label = title, Data = data }] : [],
            },
        };
    }
}
