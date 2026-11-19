using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.Dashboard;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Creator;

/// <summary>
/// 創作者中心 B-2 作品表現：每部作品、每篇文章一列，可排序；期間數字讀每日快照
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="userManager">帳號管理</param>
public class WorksModel(ApplicationDbContext context, UserManager<InvolverUser> userManager) : CreatorPageModel(userManager)
{
    /// <summary>
    /// 可排序的欄位
    /// </summary>
    public static readonly string[] SortKeys = ["views", "periodViews", "readers", "follows", "comments", "coins", "periodCoins", "updated"];

    /// <summary>
    /// 作品表現的一列
    /// </summary>
    public class WorkRow
    {
        /// <summary>內容類型</summary>
        public ContentStatType Type { get; init; }

        /// <summary>作品或文章 ID</summary>
        public int Id { get; init; }

        /// <summary>標題</summary>
        public string Title { get; init; }

        /// <summary>更新時間</summary>
        public DateTime UpdateTime { get; init; }

        /// <summary>總瀏覽</summary>
        public int TotalViews { get; init; }

        /// <summary>期間瀏覽（快照）</summary>
        public int PeriodViews { get; set; }

        /// <summary>不重複讀者（登入的讀者）</summary>
        public int Readers { get; init; }

        /// <summary>追蹤數（只有作品）</summary>
        public int? Follows { get; init; }

        /// <summary>留言數（作品含章節留言）</summary>
        public int Comments { get; init; }

        /// <summary>累計收到的 In 幣</summary>
        public decimal Coins { get; init; }

        /// <summary>期間收到的 In 幣（快照）</summary>
        public decimal PeriodCoins { get; set; }
    }

    /// <summary>
    /// 排序欄位
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Sort { get; set; } = "views";

    /// <summary>
    /// 作品與文章
    /// </summary>
    public List<WorkRow> Works { get; private set; } = [];

    /// <summary>
    /// 載入作品表現
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var me = UserId;
        if (!SortKeys.Contains(Sort))
        {
            Sort = "views";
        }

        var novels = await context.Novels
            .Where(n => n.ProfileID == me && !n.IsDeleted)
            .Select(n => new WorkRow
            {
                Type = ContentStatType.Novel,
                Id = n.NovelID,
                Title = n.Title,
                UpdateTime = n.UpdateTime,
                TotalViews = n.TotalViews,
                Readers = n.NovelViewers!.Count,
                Follows = n.Follows!.Count,
                Comments = n.Comments!.Count + n.Episodes!.SelectMany(e => e.Comments!).Count(),
                Coins = n.TotalCoins,
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var articles = await context.Articles
            .Where(a => a.ProfileID == me && !a.IsDeleted && a.Type == ArticleType.General)
            .Select(a => new WorkRow
            {
                Type = ContentStatType.Article,
                Id = a.ArticleID,
                Title = a.Title,
                UpdateTime = a.UpdateTime,
                TotalViews = a.TotalViews,
                Readers = a.ArticleViewers!.Count,
                Follows = null,
                Comments = a.Comments!.Count,
                Coins = a.TotalCoins,
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Works = [.. novels, .. articles];

        // 期間數字：快照只記錄有活動的日子，在記憶體加總（decimal）
        var since = DateOnly.FromDateTime(DateTime.Now).AddDays(-Days);
        var stats = await context.DailyContentStats
            .Where(s => s.OwnerProfileId == me && s.Date >= since)
            .Select(s => new { s.ContentType, s.ContentId, s.Views, s.CoinsReceived })
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var byContent = stats
            .GroupBy(s => (s.ContentType, s.ContentId))
            .ToDictionary(g => g.Key, g => (Views: g.Sum(s => s.Views), Coins: g.Sum(s => s.CoinsReceived)));

        foreach (var work in Works)
        {
            if (byContent.TryGetValue((work.Type, work.Id), out var period))
            {
                work.PeriodViews = period.Views;
                work.PeriodCoins = period.Coins;
            }
        }

        Works = Sort switch
        {
            "periodViews" => Works.OrderByDescending(w => w.PeriodViews).ToList(),
            "readers" => Works.OrderByDescending(w => w.Readers).ToList(),
            "follows" => Works.OrderByDescending(w => w.Follows ?? -1).ToList(),
            "comments" => Works.OrderByDescending(w => w.Comments).ToList(),
            "coins" => Works.OrderByDescending(w => w.Coins).ToList(),
            "periodCoins" => Works.OrderByDescending(w => w.PeriodCoins).ToList(),
            "updated" => Works.OrderByDescending(w => w.UpdateTime).ToList(),
            _ => Works.OrderByDescending(w => w.TotalViews).ToList(),
        };
    }
}
