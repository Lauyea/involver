using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models.Dashboard
{
    /// <summary>
    /// 創作者分潤撥款紀錄。每位創作者每個月份一筆；
    /// 金額取自該月的 <see cref="StatisticalData.CoinMonthlyBalance"/>（CreatorIncome）收入合計。
    /// 沒有撥款紀錄的已結算月份就是「待撥」。
    /// </summary>
    public class CreatorPayout
    {
        /// <summary>
        /// 流水號
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 創作者 ProfileID
        /// </summary>
        [Required]
        [StringLength(450)]
        public required string ProfileId { get; set; }

        /// <summary>
        /// 收入月份（該月 1 日）
        /// </summary>
        public DateOnly Month { get; set; }

        /// <summary>
        /// 撥款金額
        /// </summary>
        [Column(TypeName = "money")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 標記撥款的時間（UTC）
        /// </summary>
        public DateTime PaidAt { get; set; }

        /// <summary>
        /// 標記撥款的管理者 ProfileID
        /// </summary>
        [StringLength(450)]
        public string? PaidByProfileId { get; set; }

        /// <summary>
        /// 備註（例如轉帳編號）
        /// </summary>
        [StringLength(200)]
        public string? Note { get; set; }
    }
}
