using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using DataAccess.Common;

namespace DataAccess.Models
{
    /// <summary>
    /// 站內通知
    /// </summary>
    public class Notification
    {
        /// <summary>
        /// 通知 ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// 通知的主要文字（例如「回覆了你在《書名》的評論」），畫面會接在觸發者名稱後面。
        /// </summary>
        /// <remarks>
        /// 舊資料含有 HTML（&lt;br/&gt;、成就徽章），顯示時一律經過 AntiXss 消毒；
        /// 新資料只存純文字，使用者輸入的部分會先 HTML 編碼。
        /// </remarks>
        public string? Title { get; set; }

        /// <summary>
        /// 通知內容摘要（純文字），例如留言的前幾個字
        /// </summary>
        [StringLength(Parameters.NotificationExcerptLength)]
        public string? Excerpt { get; set; }

        /// <summary>
        /// 通知種類
        /// </summary>
        public NotificationType Type { get; set; }

        /// <summary>
        /// 觸發通知的使用者（Profile ID）；系統通知與舊資料為 null。
        /// </summary>
        /// <remarks>
        /// 不設外鍵：觸發者的 Profile 被刪除時（例如開發工具清除測試帳號），通知仍要保留，
        /// 畫面查不到 Profile 時顯示為「有人」。
        /// </remarks>
        [StringLength(450)]
        public string? ActorID { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// 是否已讀
        /// </summary>
        public bool IsRead { get; set; }

        /// <summary>
        /// 點擊通知後前往的站內網址（相對路徑）。舊資料可能是含網域的完整網址。
        /// </summary>
        [DataType(DataType.Url)]
        [Display(Name = "通知網址")]
        [StringLength(Parameters.ImageUrlLength)]
        public string? Url { get; set; }

        /// <summary>
        /// FK to Profile
        /// </summary>
        [Required]
        public required string ProfileID { get; set; }
        /// <summary>
        /// Reference navigation to Profile
        /// </summary>
        [ForeignKey("ProfileID")]
        public Profile? Profile { get; set; }
    }
}
