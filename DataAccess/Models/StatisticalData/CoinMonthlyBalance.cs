using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models.StatisticalData
{
    /// <summary>
    /// In 幣月結。由每月排程從 <see cref="CoinTransaction"/> 彙總上個月的資料，
    /// 只記錄當月有異動的使用者與幣別。舊的交易明細歸檔後，仍可靠這張表對帳。
    /// 「月」以台灣時間（UTC+8）切分。
    /// </summary>
    public class CoinMonthlyBalance
    {
        /// <summary>
        /// 流水號
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 月份，固定為該月 1 日
        /// </summary>
        public DateOnly Month { get; set; }

        /// <summary>
        /// FK to Profile
        /// </summary>
        [Required]
        public required string ProfileId { get; set; }

        /// <summary>
        /// 幣別
        /// </summary>
        public CoinType CoinType { get; set; }

        /// <summary>
        /// 期初餘額
        /// </summary>
        [Column(TypeName = "money")]
        public decimal OpeningBalance { get; set; }

        /// <summary>
        /// 當月收入合計（正數）
        /// </summary>
        [Column(TypeName = "money")]
        public decimal Credits { get; set; }

        /// <summary>
        /// 當月支出合計（以正數表示）
        /// </summary>
        [Column(TypeName = "money")]
        public decimal Debits { get; set; }

        /// <summary>
        /// 期末餘額
        /// </summary>
        [Column(TypeName = "money")]
        public decimal ClosingBalance { get; set; }

        /// <summary>
        /// 當月交易筆數
        /// </summary>
        public int TransactionCount { get; set; }

        /// <summary>
        /// 彙總時間（UTC）
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
