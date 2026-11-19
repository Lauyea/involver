using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.Dashboard;
using DataAccess.Models.StatisticalData;

using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    /// <summary>
    /// 每日快照的寫入結果
    /// </summary>
    /// <param name="Site">全站快照</param>
    /// <param name="ContentCount">寫入的內容快照筆數</param>
    public sealed record DailySnapshotResult(DailySiteStat Site, int ContentCount);

    /// <summary>
    /// 計算並寫入每日快照（<see cref="DailySiteStat"/>、<see cref="DailyContentStat"/>）。
    /// 由每日排程在歸零 DailyView 之前呼叫；瀏覽數取自當下的 DailyView，所以順序不能顛倒。
    /// 重跑同一天會先刪除該天既有的快照再寫入。
    /// </summary>
    /// <param name="context">資料庫內容</param>
    /// <param name="timeZone">伺服器時區，預設為 <see cref="TimeZoneInfo.Local"/>（測試時可指定）</param>
    public class DailySnapshotService(ApplicationDbContext context, TimeZoneInfo? timeZone = null)
    {
        private readonly TimeZoneInfo _timeZone = timeZone ?? TimeZoneInfo.Local;

        /// <summary>
        /// 計算並寫入指定日期的快照
        /// </summary>
        /// <param name="date">日期</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>寫入結果</returns>
        public async Task<DailySnapshotResult> WriteSnapshotAsync(DateOnly date, CancellationToken cancellationToken = default)
        {
            var window = SnapshotWindow.For(date, _timeZone);
            var transactions = await LoadSupportAndIncomeTransactionsAsync(window, cancellationToken);

            var site = await CalculateSiteStatAsync(window, transactions, cancellationToken);
            var contents = await CalculateContentStatsAsync(window, transactions, cancellationToken);

            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            await context.DailySiteStats
                .Where(s => s.Date == date)
                .ExecuteDeleteAsync(cancellationToken);
            await context.DailyContentStats
                .Where(s => s.Date == date)
                .ExecuteDeleteAsync(cancellationToken);

            context.DailySiteStats.Add(site);
            context.DailyContentStats.AddRange(contents);
            await context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return new DailySnapshotResult(site, contents.Count);
        }

        /// <summary>
        /// 計算全站快照（不寫入資料庫）
        /// </summary>
        /// <param name="window">時間範圍</param>
        /// <param name="transactions">當天的 In 幣流水帳</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>全站快照</returns>
        internal async Task<DailySiteStat> CalculateSiteStatAsync(SnapshotWindow window, IReadOnlyList<TransactionRow> transactions, CancellationToken cancellationToken)
        {
            var monthStartLocal = window.LocalEnd.AddDays(-DashboardMetrics.MonthlyDays);
            var monthStartUtc = window.UtcEnd.AddDays(-DashboardMetrics.MonthlyDays);
            var weekStartUtc = window.UtcEnd.AddDays(-DashboardMetrics.WeeklyDays);

            var profiles = context.Profiles.AsNoTracking();
            var novels = context.Novels.AsNoTracking().Where(n => !n.IsDeleted);
            var articles = context.Articles.AsNoTracking().Where(a => !a.IsDeleted && a.Type == ArticleType.General);

            var novelViews = await context.Novels.SumAsync(n => n.DailyView, cancellationToken);
            var articleViews = await context.Articles.SumAsync(a => a.DailyView, cancellationToken);

            var topUps = transactions.Where(t => t.TransactionType == CoinTransactionType.TopUp && t.Amount > 0).ToList();

            return new DailySiteStat
            {
                Date = window.Date,
                TotalUsers = await profiles.CountAsync(cancellationToken),
                NewUsers = await profiles.CountAsync(p => p.EnrollmentDate >= window.LocalStart && p.EnrollmentDate < window.LocalEnd, cancellationToken),
                DailyActiveUsers = await profiles.CountAsync(p => p.LastTimeLogin >= window.LocalStart && p.LastTimeLogin < window.LocalEnd, cancellationToken),
                MonthlyActiveUsers = await profiles.CountAsync(p => p.LastTimeLogin >= monthStartLocal && p.LastTimeLogin < window.LocalEnd, cancellationToken),
                PrimeUsers = await profiles.CountAsync(p => p.Prime, cancellationToken),
                ProfessionalUsers = await profiles.CountAsync(p => p.Professional, cancellationToken),
                Novels = await novels.CountAsync(cancellationToken),
                Articles = await articles.CountAsync(cancellationToken),
                Episodes = await context.Episodes.CountAsync(e => !e.IsDeleted, cancellationToken),
                Comments = await context.Comments.CountAsync(cancellationToken),
                NewNovels = await novels.CountAsync(n => n.CreateTime >= window.LocalStart && n.CreateTime < window.LocalEnd, cancellationToken),
                NewArticles = await articles.CountAsync(a => a.CreateTime >= window.LocalStart && a.CreateTime < window.LocalEnd, cancellationToken),
                Views = novelViews + articleViews,
                WeeklyVoters = await DashboardMetrics.CountVotersAsync(context, weekStartUtc, window.UtcEnd, cancellationToken),
                MonthlyPayingSupporters = await DashboardMetrics.CountPayingSupportersAsync(context, monthStartUtc, window.UtcEnd, cancellationToken),
                MonthlyActiveCreators = await DashboardMetrics.CountActiveCreatorsAsync(context, monthStartLocal, window.LocalEnd, cancellationToken),
                TopUpAmount = topUps.Sum(t => t.Amount),
                TopUpCount = topUps.Count,
                RevenueShareAmount = transactions
                    .Where(t => t.TransactionType == CoinTransactionType.RevenueShare && t.CoinType == CoinType.CreatorIncome)
                    .Sum(t => t.Amount),
                RealCoinsSpent = transactions
                    .Where(t => t.CoinType == CoinType.Real && t.Amount < 0 && DashboardMetrics.SupportTypes.Contains(t.TransactionType))
                    .Sum(t => -t.Amount),
                VirtualCoinsIssued = transactions
                    .Where(t => t.CoinType == CoinType.Virtual && t.Amount > 0 && t.TransactionType != CoinTransactionType.AdminAdjust)
                    .Sum(t => t.Amount),
                CreatedAt = DateTime.UtcNow,
            };
        }

        /// <summary>
        /// 計算當天有活動的作品、文章快照（不寫入資料庫）
        /// </summary>
        /// <param name="window">時間範圍</param>
        /// <param name="transactions">當天的 In 幣流水帳</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>內容快照</returns>
        internal async Task<List<DailyContentStat>> CalculateContentStatsAsync(SnapshotWindow window, IReadOnlyList<TransactionRow> transactions, CancellationToken cancellationToken)
        {
            var stats = new Dictionary<(ContentStatType Type, int Id), DailyContentStat>();
            var createdAt = DateTime.UtcNow;

            DailyContentStat GetOrAdd(ContentStatType type, int id)
            {
                if (!stats.TryGetValue((type, id), out var stat))
                {
                    // 作者稍後統一補上
                    stat = new DailyContentStat { Date = window.Date, ContentType = type, ContentId = id, OwnerProfileId = string.Empty, CreatedAt = createdAt };
                    stats.Add((type, id), stat);
                }

                return stat;
            }

            // 瀏覽數：歸零前的 DailyView
            var novelViews = await context.Novels
                .Where(n => n.DailyView > 0)
                .Select(n => new { n.NovelID, n.DailyView })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            foreach (var n in novelViews)
            {
                GetOrAdd(ContentStatType.Novel, n.NovelID).Views = n.DailyView;
            }

            var articleViews = await context.Articles
                .Where(a => a.DailyView > 0)
                .Select(a => new { a.ArticleID, a.DailyView })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            foreach (var a in articleViews)
            {
                GetOrAdd(ContentStatType.Article, a.ArticleID).Views = a.DailyView;
            }

            // 不重複讀者：觀看紀錄只保留 30 天，當天的資料一定還在
            var views = await context.Views
                .Where(v => v.CreateTime >= window.LocalStart && v.CreateTime < window.LocalEnd)
                .Select(v => new { v.NovelId, v.ArticleId, Reader = v.UserId ?? v.SessionId })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            foreach (var group in views.Where(v => v.NovelId.HasValue).GroupBy(v => v.NovelId!.Value))
            {
                GetOrAdd(ContentStatType.Novel, group.Key).UniqueReaders = CountDistinctReaders(group.Select(v => v.Reader));
            }

            foreach (var group in views.Where(v => v.ArticleId.HasValue).GroupBy(v => v.ArticleId!.Value))
            {
                GetOrAdd(ContentStatType.Article, group.Key).UniqueReaders = CountDistinctReaders(group.Select(v => v.Reader));
            }

            // 新追蹤（只有作品可以被追蹤）
            var follows = await context.Follows
                .Where(f => f.NovelID != null && f.UpdateTime >= window.LocalStart && f.UpdateTime < window.LocalEnd)
                .GroupBy(f => f.NovelID!.Value)
                .Select(g => new { NovelId = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);
            foreach (var f in follows)
            {
                GetOrAdd(ContentStatType.Novel, f.NovelId).NewFollows = f.Count;
            }

            // 新留言：章節的留言算在作品上
            var comments = await context.Comments
                .Where(c => c.UpdateTime >= window.LocalStart && c.UpdateTime < window.LocalEnd)
                .Select(c => new { NovelId = c.NovelID ?? (int?)c.Episode!.NovelID, c.ArticleID })
                .AsNoTracking()
                .ToListAsync(cancellationToken);
            foreach (var group in comments.Where(c => c.NovelId.HasValue).GroupBy(c => c.NovelId!.Value))
            {
                GetOrAdd(ContentStatType.Novel, group.Key).NewComments = group.Count();
            }

            foreach (var group in comments.Where(c => c.NovelId == null && c.ArticleID.HasValue).GroupBy(c => c.ArticleID!.Value))
            {
                GetOrAdd(ContentStatType.Article, group.Key).NewComments = group.Count();
            }

            // 收到的 In 幣：讀者投票、贊助作品或文章時付出的金額
            await AddCoinsReceivedAsync(transactions, GetOrAdd, cancellationToken);

            await FillOwnersAsync(stats, cancellationToken);

            // 找不到作者的內容（已被實體刪除）不寫入
            return stats.Values
                .Where(s => s.OwnerProfileId.Length > 0)
                .OrderBy(s => s.ContentType)
                .ThenBy(s => s.ContentId)
                .ToList();
        }

        /// <summary>
        /// 讀取當天與快照有關的 In 幣流水帳。只取需要的欄位，在記憶體加總（SQLite 無法加總 decimal）
        /// </summary>
        private Task<List<TransactionRow>> LoadSupportAndIncomeTransactionsAsync(SnapshotWindow window, CancellationToken cancellationToken)
        {
            return context.CoinTransactions
                .Where(t => t.CreatedAt >= window.UtcStart && t.CreatedAt < window.UtcEnd)
                .Select(t => new TransactionRow(t.CoinType, t.TransactionType, t.Amount, t.ReferenceType, t.ReferenceId))
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 依流水帳的關聯對象累加作品、文章收到的 In 幣；投票的關聯對象是投票，要換算成作品
        /// </summary>
        private async Task AddCoinsReceivedAsync(
            IReadOnlyList<TransactionRow> transactions,
            Func<ContentStatType, int, DailyContentStat> getOrAdd,
            CancellationToken cancellationToken)
        {
            var spending = transactions
                .Where(t => t.Amount < 0
                    && t.CoinType != CoinType.CreatorIncome
                    && DashboardMetrics.SupportTypes.Contains(t.TransactionType)
                    && t.ReferenceType != null
                    && int.TryParse(t.ReferenceId, out _))
                .Select(t => (t.ReferenceType!.Value, Id: int.Parse(t.ReferenceId!), Amount: -t.Amount))
                .ToList();

            var votingIds = spending
                .Where(s => s.Value == CoinReferenceType.Voting)
                .Select(s => s.Id)
                .Distinct()
                .ToList();

            var votingNovels = votingIds.Count == 0
                ? new Dictionary<int, int>()
                : await context.Votings
                    .Where(v => votingIds.Contains(v.VotingID))
                    .Select(v => new { v.VotingID, v.Episode!.NovelID })
                    .AsNoTracking()
                    .ToDictionaryAsync(v => v.VotingID, v => v.NovelID, cancellationToken);

            foreach (var (referenceType, id, amount) in spending)
            {
                switch (referenceType)
                {
                    case CoinReferenceType.Novel:
                        getOrAdd(ContentStatType.Novel, id).CoinsReceived += amount;
                        break;
                    case CoinReferenceType.Article:
                        getOrAdd(ContentStatType.Article, id).CoinsReceived += amount;
                        break;
                    case CoinReferenceType.Voting when votingNovels.TryGetValue(id, out var novelId):
                        getOrAdd(ContentStatType.Novel, novelId).CoinsReceived += amount;
                        break;
                }
            }
        }

        /// <summary>
        /// 補上每筆內容快照的作者
        /// </summary>
        private async Task FillOwnersAsync(Dictionary<(ContentStatType Type, int Id), DailyContentStat> stats, CancellationToken cancellationToken)
        {
            var novelIds = stats.Keys.Where(k => k.Type == ContentStatType.Novel).Select(k => k.Id).ToList();
            var articleIds = stats.Keys.Where(k => k.Type == ContentStatType.Article).Select(k => k.Id).ToList();

            if (novelIds.Count > 0)
            {
                var owners = await context.Novels
                    .Where(n => novelIds.Contains(n.NovelID))
                    .Select(n => new { n.NovelID, n.ProfileID })
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                foreach (var owner in owners)
                {
                    stats[(ContentStatType.Novel, owner.NovelID)].OwnerProfileId = owner.ProfileID;
                }
            }

            if (articleIds.Count > 0)
            {
                var owners = await context.Articles
                    .Where(a => articleIds.Contains(a.ArticleID))
                    .Select(a => new { a.ArticleID, a.ProfileID })
                    .AsNoTracking()
                    .ToListAsync(cancellationToken);
                foreach (var owner in owners)
                {
                    stats[(ContentStatType.Article, owner.ArticleID)].OwnerProfileId = owner.ProfileID;
                }
            }
        }

        /// <summary>
        /// 不重複讀者數；沒有用戶也沒有 Session 的紀錄各算一位
        /// </summary>
        private static int CountDistinctReaders(IEnumerable<string?> readers)
        {
            var list = readers.ToList();
            return list.Where(r => r != null).Distinct().Count() + list.Count(r => r == null);
        }

        /// <summary>
        /// 快照需要的流水帳欄位
        /// </summary>
        /// <param name="CoinType">幣別</param>
        /// <param name="TransactionType">交易類型</param>
        /// <param name="Amount">異動金額</param>
        /// <param name="ReferenceType">關聯對象的種類</param>
        /// <param name="ReferenceId">關聯對象的 ID</param>
        internal sealed record TransactionRow(
            CoinType CoinType,
            CoinTransactionType TransactionType,
            decimal Amount,
            CoinReferenceType? ReferenceType,
            string? ReferenceId);
    }
}
