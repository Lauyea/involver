using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models.Dashboard
{
    /// <summary>
    /// 單一作品或文章的每日快照。只記錄當天有活動（瀏覽、追蹤、留言、收到 In 幣）的內容。
    /// 不設 FK：作品被刪除後，歷史數據仍保留。
    /// </summary>
    public class DailyContentStat
    {
        /// <summary>
        /// 流水號
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 日期
        /// </summary>
        public DateOnly Date { get; set; }

        /// <summary>
        /// 內容類型
        /// </summary>
        public ContentStatType ContentType { get; set; }

        /// <summary>
        /// 作品或文章 ID
        /// </summary>
        public int ContentId { get; set; }

        /// <summary>
        /// 作者 ProfileID（創作者中心依此查詢）
        /// </summary>
        [Required]
        [StringLength(450)]
        public required string OwnerProfileId { get; set; }

        /// <summary>
        /// 瀏覽數（DailyView）
        /// </summary>
        public int Views { get; set; }

        /// <summary>
        /// 不重複讀者（登入者以用戶、未登入以 Session 區分）
        /// </summary>
        public int UniqueReaders { get; set; }

        /// <summary>
        /// 新追蹤
        /// </summary>
        public int NewFollows { get; set; }

        /// <summary>
        /// 新留言（含章節留言；依留言的更新時間）
        /// </summary>
        public int NewComments { get; set; }

        /// <summary>
        /// 收到的 In 幣（投票與直接贊助，讀者付出的金額）
        /// </summary>
        [Column(TypeName = "money")]
        public decimal CoinsReceived { get; set; }

        /// <summary>
        /// 寫入時間（UTC）
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
