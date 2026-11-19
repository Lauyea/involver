using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Recommendation
{
    /// <summary>
    /// 預先算好的個人推薦。由排程寫入，首頁只讀這張表，不在請求時即時計算。
    /// 每次計算會整批取代該會員的舊結果。內容是小說或文章，不設 FK。
    /// </summary>
    public class UserRecommendation
    {
        /// <summary>
        /// 流水號
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// 會員 ProfileID
        /// </summary>
        [Required]
        [StringLength(450)]
        public required string ProfileId { get; set; }

        /// <summary>
        /// 內容類型
        /// </summary>
        public RecommendationContentType ContentType { get; set; }

        /// <summary>
        /// 作品或文章 ID
        /// </summary>
        public int ContentId { get; set; }

        /// <summary>
        /// 排名（從 0 開始）
        /// </summary>
        public int Rank { get; set; }

        /// <summary>
        /// 分數
        /// </summary>
        public double Score { get; set; }

        /// <summary>
        /// 推薦理由，例如「因為你讀過《X》」、「你常看 #奇幻」
        /// </summary>
        [Required]
        [StringLength(100)]
        public required string Reason { get; set; }

        /// <summary>
        /// 計算時使用的設定版本；使用程式預設值時為 null
        /// </summary>
        public int? SettingVersion { get; set; }

        /// <summary>
        /// 計算時間（UTC）
        /// </summary>
        public DateTime ComputedAt { get; set; }
    }
}
