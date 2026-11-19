using DataAccess.Common;
using DataAccess.Data;

using Involver.Models.ViewModels.Dashboard;
using Involver.Services.Admin;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Creator;

/// <summary>
/// 創作者中心 B-3 讀者與互動：追蹤人數成長、每次投票的參與人數、投入 In 幣與勝出選項
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="userManager">帳號管理</param>
public class ReadersModel(ApplicationDbContext context, UserManager<InvolverUser> userManager) : CreatorPageModel(userManager)
{
    /// <summary>
    /// 投票列表的筆數
    /// </summary>
    public const int VotingCount = 20;

    /// <summary>
    /// 一次投票的結果
    /// </summary>
    public class VotingRow
    {
        /// <summary>投票 ID</summary>
        public int VotingId { get; init; }

        /// <summary>投票標題</summary>
        public string Title { get; init; }

        /// <summary>作品標題</summary>
        public string NovelTitle { get; init; }

        /// <summary>章節 ID</summary>
        public int EpisodeId { get; init; }

        /// <summary>章節標題</summary>
        public string EpisodeTitle { get; init; }

        /// <summary>投票模式</summary>
        public PolicyType Policy { get; init; }

        /// <summary>是否已結束</summary>
        public bool End { get; init; }

        /// <summary>建立時間</summary>
        public DateTime CreateTime { get; init; }

        /// <summary>參與人數</summary>
        public int Participants { get; init; }

        /// <summary>投入 In 幣</summary>
        public decimal Coins { get; init; }

        /// <summary>勝出（目前領先）的選項</summary>
        public string Winner { get; init; }
    }

    /// <summary>
    /// 追蹤 KPI
    /// </summary>
    public List<KpiCardViewModel> Cards { get; private set; } = [];

    /// <summary>
    /// 追蹤人數成長圖
    /// </summary>
    public ChartPanelViewModel FollowChart { get; private set; }

    /// <summary>
    /// 投票參與圖
    /// </summary>
    public ChartPanelViewModel VotingChart { get; private set; }

    /// <summary>
    /// 最近的投票
    /// </summary>
    public List<VotingRow> Votings { get; private set; } = [];

    /// <summary>
    /// 載入讀者與互動
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var me = UserId;
        await LoadFollowsAsync(me, cancellationToken);
        await LoadVotingsAsync(me, cancellationToken);
    }

    private async Task LoadFollowsAsync(string me, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.Now);
        var dates = DashboardSeries.Dates(today.AddDays(-Days + 1), today);
        var start = dates[0].ToDateTime(TimeOnly.MinValue);

        // 追蹤創作者本人或任一部作品
        var follows = context.Follows.Where(f => f.ProfileID == me || f.Novel!.ProfileID == me);

        var creatorFollowers = await context.Follows.CountAsync(f => f.ProfileID == me, cancellationToken);
        var novelFollows = await context.Follows.CountAsync(f => f.Novel!.ProfileID == me, cancellationToken);
        var before = await follows.CountAsync(f => f.UpdateTime < start, cancellationToken);
        var recent = await follows
            .Where(f => f.UpdateTime >= start)
            .Select(f => f.UpdateTime)
            .ToListAsync(cancellationToken);

        var daily = dates.Select(d => (decimal)recent.Count(t => DateOnly.FromDateTime(t) == d)).ToList();
        var cumulative = new List<decimal>();
        var running = (decimal)before;
        foreach (var count in daily)
        {
            running += count;
            cumulative.Add(running);
        }

        Cards =
        [
            new KpiCardViewModel { Label = "追蹤我的讀者", Value = creatorFollowers, Icon = "fa-user-group" },
            new KpiCardViewModel { Label = "作品被追蹤", Value = novelFollows, Icon = "fa-bookmark" },
            new KpiCardViewModel
            {
                Label = $"{Days} 天新追蹤",
                Value = daily.Sum(),
                Icon = "fa-arrow-trend-up",
                ChangePercent = DataAccess.Services.DashboardMetrics.ChangePercent(running, before),
                ComparisonLabel = $"總追蹤數與 {Days} 天前相比",
            },
        ];

        FollowChart = new ChartPanelViewModel
        {
            Title = "追蹤人數成長",
            Subtitle = "追蹤創作者與作品的合計",
            Config = new ChartConfigViewModel
            {
                Type = "bar",
                Labels = DashboardSeries.Labels(dates),
                YTitle = "每日新追蹤",
                Y1Title = "累計",
                Datasets = running == 0 ? [] :
                [
                    new ChartDatasetViewModel { Label = "每日新追蹤", Data = daily, ColorIndex = 1 },
                    new ChartDatasetViewModel { Label = "累計追蹤", Data = cumulative, ColorIndex = 0, Type = "line", YAxisId = "y1" },
                ],
            },
        };
    }

    private async Task LoadVotingsAsync(string me, CancellationToken cancellationToken)
    {
        var votings = await context.Votings
            .Where(v => v.Episode!.Novel!.ProfileID == me)
            .OrderByDescending(v => v.CreateTime)
            .Take(VotingCount)
            .Select(v => new
            {
                v.VotingID,
                v.Title,
                v.Policy,
                v.End,
                v.CreateTime,
                v.TotalNumber,
                v.TotalCoins,
                v.EpisodeID,
                EpisodeTitle = v.Episode!.Title,
                NovelTitle = v.Episode!.Novel!.Title,
                Options = v.NormalOptions!.Select(o => new { o.Content, o.TotalCoins, Votes = o.Votes!.Count }).ToList(),
            })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Votings = votings
            .Select(v =>
            {
                // 平等模式每票價值相同，看票數；自由模式看投入的 In 幣
                var leader = v.Policy == PolicyType.Equality
                    ? v.Options.OrderByDescending(o => o.Votes).ThenByDescending(o => o.TotalCoins).FirstOrDefault()
                    : v.Options.OrderByDescending(o => o.TotalCoins).ThenByDescending(o => o.Votes).FirstOrDefault();

                return new VotingRow
                {
                    VotingId = v.VotingID,
                    Title = v.Title,
                    NovelTitle = v.NovelTitle,
                    EpisodeId = v.EpisodeID,
                    EpisodeTitle = v.EpisodeTitle,
                    Policy = v.Policy,
                    End = v.End,
                    CreateTime = v.CreateTime,
                    Participants = v.TotalNumber,
                    Coins = v.TotalCoins,
                    Winner = leader == null || (leader.Votes == 0 && leader.TotalCoins == 0) ? null : leader.Content,
                };
            })
            .ToList();

        var chronological = Votings.AsEnumerable().Reverse().ToList();
        VotingChart = new ChartPanelViewModel
        {
            Title = "投票參與",
            Subtitle = $"最近 {VotingCount} 次投票",
            Config = new ChartConfigViewModel
            {
                Type = "bar",
                Labels = chronological.Select(v => $"{v.CreateTime:MM/dd} {v.Title}").ToList(),
                YTitle = "參與人數",
                Y1Title = "投入 In 幣",
                Datasets = chronological.Count == 0 ? [] :
                [
                    new ChartDatasetViewModel { Label = "參與人數", Data = chronological.Select(v => (decimal)v.Participants).ToList(), ColorIndex = 1 },
                    new ChartDatasetViewModel { Label = "投入 In 幣", Data = chronological.Select(v => v.Coins).ToList(), ColorIndex = 0, Type = "line", YAxisId = "y1" },
                ],
            },
        };
    }
}
