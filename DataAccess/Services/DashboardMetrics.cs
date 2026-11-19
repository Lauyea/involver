using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.StatisticalData;

using Microsoft.EntityFrameworkCore;

namespace DataAccess.Services
{
    /// <summary>
    /// 儀表板的核心指標。每日快照與總覽的即時數字共用同一套定義，兩邊算出來的數字才對得起來。
    /// </summary>
    public static class DashboardMetrics
    {
        /// <summary>
        /// 「每週」指標的天數
        /// </summary>
        public const int WeeklyDays = 7;

        /// <summary>
        /// 「每月」指標（MAU、付費支持者、活躍創作者）的天數
        /// </summary>
        public const int MonthlyDays = 30;

        /// <summary>
        /// 讀者支持創作的交易類型（投票與各種贊助）
        /// </summary>
        public static readonly CoinTransactionType[] SupportTypes =
        [
            CoinTransactionType.Vote,
            CoinTransactionType.InvolveNovel,
            CoinTransactionType.InvolveArticle,
            CoinTransactionType.InvolveCreator,
            CoinTransactionType.MonthlyInvolveNovel,
            CoinTransactionType.MonthlyInvolveCreator,
        ];

        /// <summary>
        /// 期間內參與投票的不重複讀者（實體、虛擬 In 幣都算）
        /// </summary>
        /// <param name="context">資料庫內容</param>
        /// <param name="utcStart">起始時間（含，UTC）</param>
        /// <param name="utcEnd">結束時間（不含，UTC）</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>人數</returns>
        public static Task<int> CountVotersAsync(ApplicationDbContext context, DateTime utcStart, DateTime utcEnd, CancellationToken cancellationToken = default)
        {
            return context.CoinTransactions
                .Where(t => t.TransactionType == CoinTransactionType.Vote
                    && t.Amount < 0
                    && t.CreatedAt >= utcStart
                    && t.CreatedAt < utcEnd)
                .Select(t => t.ProfileId)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        /// <summary>
        /// 期間內以實體 In 幣付費支持（投票、贊助）的不重複人數
        /// </summary>
        /// <param name="context">資料庫內容</param>
        /// <param name="utcStart">起始時間（含，UTC）</param>
        /// <param name="utcEnd">結束時間（不含，UTC）</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>人數</returns>
        public static Task<int> CountPayingSupportersAsync(ApplicationDbContext context, DateTime utcStart, DateTime utcEnd, CancellationToken cancellationToken = default)
        {
            return context.CoinTransactions
                .Where(t => t.CoinType == CoinType.Real
                    && t.Amount < 0
                    && SupportTypes.Contains(t.TransactionType)
                    && t.CreatedAt >= utcStart
                    && t.CreatedAt < utcEnd)
                .Select(t => t.ProfileId)
                .Distinct()
                .CountAsync(cancellationToken);
        }

        /// <summary>
        /// 期間內有更新章節或發表一般文章的不重複創作者
        /// </summary>
        /// <param name="context">資料庫內容</param>
        /// <param name="localStart">起始時間（含，伺服器時間）</param>
        /// <param name="localEnd">結束時間（不含，伺服器時間）</param>
        /// <param name="cancellationToken">取消權杖</param>
        /// <returns>人數</returns>
        public static Task<int> CountActiveCreatorsAsync(ApplicationDbContext context, DateTime localStart, DateTime localEnd, CancellationToken cancellationToken = default)
        {
            var episodeAuthors = context.Episodes
                .Where(e => !e.IsDeleted && e.UpdateTime >= localStart && e.UpdateTime < localEnd)
                .Select(e => e.Novel!.ProfileID);

            var articleAuthors = context.Articles
                .Where(a => !a.IsDeleted
                    && a.Type == ArticleType.General
                    && a.CreateTime >= localStart
                    && a.CreateTime < localEnd)
                .Select(a => a.ProfileID);

            return episodeAuthors
                .Union(articleAuthors)
                .CountAsync(cancellationToken);
        }

        /// <summary>
        /// 計算兩個數字的變化百分比
        /// </summary>
        /// <param name="current">本期</param>
        /// <param name="previous">上一期</param>
        /// <returns>變化百分比；上一期為 0 時無法比較，回傳 null</returns>
        public static decimal? ChangePercent(decimal current, decimal? previous)
        {
            if (previous is null or 0)
            {
                return null;
            }

            return Math.Round((current - previous.Value) / previous.Value * 100, 1);
        }
    }
}
