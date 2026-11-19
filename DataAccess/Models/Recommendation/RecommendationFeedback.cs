using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Recommendation
{
    /// <summary>
    /// 推薦卡片上的「不感興趣」。負面訊號：之後不再推薦這個內容，同作者、同標籤的作品也會降低分數。
    /// 內容是小說或文章，不設 FK。
    /// </summary>
    public class RecommendationFeedback
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
        /// 建立時間（UTC）
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
