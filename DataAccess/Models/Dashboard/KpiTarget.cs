using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models.Dashboard
{
    /// <summary>
    /// KPI 目標值（OKR 的 Key Result）。總覽的 KPI 卡片若有涵蓋今天的目標，會顯示進度條。
    /// 同一指標、同一期間只能有一個目標。
    /// </summary>
    public class KpiTarget
    {
        /// <summary>
        /// 流水號
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 指標
        /// </summary>
        [Display(Name = "指標")]
        public KpiMetric Metric { get; set; }

        /// <summary>
        /// 期間種類
        /// </summary>
        [Display(Name = "期間")]
        public KpiPeriod Period { get; set; }

        /// <summary>
        /// 期間的第一天（月初、季初或年初）
        /// </summary>
        [Display(Name = "期間開始")]
        public DateOnly PeriodStart { get; set; }

        /// <summary>
        /// 目標值
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "目標值")]
        public decimal TargetValue { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [StringLength(200)]
        [Display(Name = "備註")]
        public string? Note { get; set; }

        /// <summary>
        /// 設定者 ProfileID
        /// </summary>
        [StringLength(450)]
        public string? CreatedByProfileId { get; set; }

        /// <summary>
        /// 最後修改時間（UTC）
        /// </summary>
        public DateTime UpdatedAt { get; set; }
    }
}
