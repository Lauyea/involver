using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models.Dashboard
{
    /// <summary>
    /// 全站每日快照。由每日排程在歸零 DailyView 之前寫入前一天的資料，每天一筆。
    /// 「天」以伺服器時間（與 DateTime.Now 相同）切分；In 幣流水帳是 UTC，計算時換算成同一段時間。
    /// 存量欄位（總數）是快照當下的值，流量欄位（新註冊、瀏覽、金額）是那一天的合計；
    /// 每日新增的章節、留言沒有建立時間，由前後兩天的總數相減。
    /// </summary>
    public class DailySiteStat
    {
        /// <summary>
        /// 日期
        /// </summary>
        [Key]
        public DateOnly Date { get; set; }

        /// <summary>
        /// 總用戶數（快照當下）
        /// </summary>
        public int TotalUsers { get; set; }

        /// <summary>
        /// 當天新註冊
        /// </summary>
        public int NewUsers { get; set; }

        /// <summary>
        /// 當天有登入的用戶（依 LastTimeLogin）
        /// </summary>
        public int DailyActiveUsers { get; set; }

        /// <summary>
        /// 30 天內有登入的用戶（依 LastTimeLogin）
        /// </summary>
        public int MonthlyActiveUsers { get; set; }

        /// <summary>
        /// Prime 會員人數
        /// </summary>
        public int PrimeUsers { get; set; }

        /// <summary>
        /// 特約作家人數
        /// </summary>
        public int ProfessionalUsers { get; set; }

        /// <summary>
        /// 作品數（不含軟刪除）
        /// </summary>
        public int Novels { get; set; }

        /// <summary>
        /// 文章數（一般文章，不含公告、意見回饋與軟刪除）
        /// </summary>
        public int Articles { get; set; }

        /// <summary>
        /// 章節數（不含軟刪除）
        /// </summary>
        public int Episodes { get; set; }

        /// <summary>
        /// 留言數
        /// </summary>
        public int Comments { get; set; }

        /// <summary>
        /// 當天新作品
        /// </summary>
        public int NewNovels { get; set; }

        /// <summary>
        /// 當天新文章（一般文章）
        /// </summary>
        public int NewArticles { get; set; }

        /// <summary>
        /// 當天瀏覽數（作品與文章的 DailyView 合計）
        /// </summary>
        public int Views { get; set; }

        /// <summary>
        /// 截至當天的 7 天內參與投票的不重複讀者
        /// </summary>
        public int WeeklyVoters { get; set; }

        /// <summary>
        /// 截至當天的 30 天內以實體 In 幣付費支持（投票、贊助）的不重複人數
        /// </summary>
        public int MonthlyPayingSupporters { get; set; }

        /// <summary>
        /// 截至當天的 30 天內有更新章節或發表文章的創作者數
        /// </summary>
        public int MonthlyActiveCreators { get; set; }

        /// <summary>
        /// 當天儲值金額
        /// </summary>
        [Column(TypeName = "money")]
        public decimal TopUpAmount { get; set; }

        /// <summary>
        /// 當天儲值筆數
        /// </summary>
        public int TopUpCount { get; set; }

        /// <summary>
        /// 當天創作者分潤金額
        /// </summary>
        [Column(TypeName = "money")]
        public decimal RevenueShareAmount { get; set; }

        /// <summary>
        /// 當天讀者花費的實體 In 幣（投票、贊助）
        /// </summary>
        [Column(TypeName = "money")]
        public decimal RealCoinsSpent { get; set; }

        /// <summary>
        /// 當天發放的虛擬 In 幣（獎勵）
        /// </summary>
        [Column(TypeName = "money")]
        public decimal VirtualCoinsIssued { get; set; }

        /// <summary>
        /// 寫入時間（UTC）
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
