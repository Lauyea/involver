using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Recommendation
{
    /// <summary>
    /// Feed 卡片的曝光與點擊紀錄，用來算各區塊、各設定版本的 CTR 並調整權重。
    /// 卡片進入畫面時寫入；同一次頁面載入（<see cref="FeedRequestId"/>）同一張卡片只有一筆。
    /// 只保留 <see cref="Common.Parameters.RecommendationImpressionRetentionDays"/> 天，由每日排程刪除。
    /// </summary>
    public class RecommendationImpression
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
        /// 這次 Feed 頁面載入的識別碼，用來把點擊對回曝光
        /// </summary>
        public Guid FeedRequestId { get; set; }

        /// <summary>
        /// 內容類型
        /// </summary>
        public RecommendationContentType ContentType { get; set; }

        /// <summary>
        /// 作品或文章 ID
        /// </summary>
        public int ContentId { get; set; }

        /// <summary>
        /// 所在區塊
        /// </summary>
        public FeedSection Section { get; set; }

        /// <summary>
        /// 在區塊內的位置（從 0 開始）
        /// </summary>
        public int Position { get; set; }

        /// <summary>
        /// 產生「為你推薦」時使用的設定版本；其他區塊為 null
        /// </summary>
        public int? SettingVersion { get; set; }

        /// <summary>
        /// 曝光時間（UTC）
        /// </summary>
        public DateTime ShownAt { get; set; }

        /// <summary>
        /// 點擊時間（UTC）；沒有點擊為 null
        /// </summary>
        public DateTime? ClickedAt { get; set; }
    }
}
