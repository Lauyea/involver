using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.Recommendation
{
    /// <summary>
    /// 會員喜歡的分類（冷啟動）。新會員在首頁選擇，推薦演算法把它當成內容偏好的初始值。
    /// </summary>
    public class ProfileCategoryPreference
    {
        /// <summary>
        /// 會員 ProfileID
        /// </summary>
        [Required]
        [StringLength(450)]
        public required string ProfileId { get; set; }

        /// <summary>
        /// Reference navigation to 會員
        /// </summary>
        public Profile? Profile { get; set; }

        /// <summary>
        /// 分類標籤（<see cref="Tag.IsCategory"/> 為 true 的標籤）
        /// </summary>
        public int TagId { get; set; }

        /// <summary>
        /// Reference navigation to 分類標籤
        /// </summary>
        public Tag? Tag { get; set; }

        /// <summary>
        /// 選擇時間（UTC）
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}
