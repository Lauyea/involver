using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models.StatisticalData
{
    /// <summary>
    /// In 幣交易紀錄（流水帳）。
    /// 只新增、不修改、不刪除；記錯時以一筆反向的沖正紀錄更正。
    /// 同一使用者、同一幣別的 <see cref="Amount"/> 加總，等於該幣別的餘額變化。
    /// </summary>
    public class CoinTransaction
    {
        /// <summary>
        /// 流水號。用 long，資料量大也不會用完
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// FK to Profile，餘額變動的使用者
        /// </summary>
        [Required]
        public required string ProfileId { get; set; }

        /// <summary>
        /// 幣別
        /// </summary>
        public CoinType CoinType { get; set; }

        /// <summary>
        /// 交易類型
        /// </summary>
        public CoinTransactionType TransactionType { get; set; }

        /// <summary>
        /// 異動金額，收入為正、支出為負
        /// </summary>
        [Column(TypeName = "money")]
        public decimal Amount { get; set; }

        /// <summary>
        /// 交易後該幣別的餘額
        /// </summary>
        [Column(TypeName = "money")]
        public decimal BalanceAfter { get; set; }

        /// <summary>
        /// 交易對象的 Profile ID（例如受贊助的創作者、付款的讀者），可為 null。
        /// 不設 FK，帳務紀錄不受對方帳號異動影響
        /// </summary>
        [StringLength(450)]
        public string? CounterpartyProfileId { get; set; }

        /// <summary>
        /// 關聯對象的種類
        /// </summary>
        public CoinReferenceType? ReferenceType { get; set; }

        /// <summary>
        /// 關聯對象的 ID。不設 FK，避免作品被刪除時連帶影響帳務。
        /// 金流付款的格式為「金流商:付款編號」，例如 <c>Stripe:pi_xxx</c>
        /// </summary>
        [StringLength(300)]
        public string? ReferenceId { get; set; }

        /// <summary>
        /// 同一個動作產生的多筆紀錄共用此 ID（例如讀者扣款與作者分潤）
        /// </summary>
        public Guid CorrelationId { get; set; }

        /// <summary>
        /// 防止重複入帳的鍵值，有值時不可重複。
        /// 金流付款的格式為「交易類型:金流商:付款編號」，例如 <c>TopUp:ECPay:xxx</c>，
        /// 不同金流商的付款編號相同也不會互相衝突；之後的退款可用 <c>Refund:...</c> 區分
        /// </summary>
        [StringLength(300)]
        public string? IdempotencyKey { get; set; }

        /// <summary>
        /// 備註
        /// </summary>
        [StringLength(200)]
        public string? Note { get; set; }

        /// <summary>
        /// 建立時間（UTC）
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
