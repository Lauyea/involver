using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models.Dashboard
{
    /// <summary>
    /// 手動登錄的網站支出（主機、網域、金流手續費等系統裡沒有的費用），單位為新台幣
    /// </summary>
    public class SiteExpense
    {
        /// <summary>
        /// 流水號
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 支出日期
        /// </summary>
        [Display(Name = "日期")]
        public DateOnly Date { get; set; }

        /// <summary>
        /// 類別
        /// </summary>
        [Display(Name = "類別")]
        public SiteExpenseCategory Category { get; set; }

        /// <summary>
        /// 金額（新台幣）
        /// </summary>
        [Column(TypeName = "money")]
        [Range(0.01, 100000000)]
        [Display(Name = "金額")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 說明
        /// </summary>
        [StringLength(200)]
        [Display(Name = "說明")]
        public string? Note { get; set; }

        /// <summary>
        /// 登錄者 ProfileID
        /// </summary>
        [StringLength(450)]
        public string? CreatedByProfileId { get; set; }

        /// <summary>
        /// 登錄時間（UTC）
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
