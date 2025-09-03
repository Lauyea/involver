using DataAccess.Common;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.AnnouncementModel
{
    /// <summary>
    /// 公告
    /// </summary>
    /// TODO: 之後可能會併入文章。
    public class Announcement
    {
        /// <summary>
        /// PK
        /// </summary>
        public int AnnouncementID { get; set; }

        /// <summary>
        /// 標題
        /// </summary>
        [Required(ErrorMessage = "必須要有標題")]
        [StringLength(Parameters.ArticleTitleLength, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 2)]
        [Display(Name = "標題")]
        public required string Title { get; set; }

        /// <summary>
        /// 擁有者ID
        /// </summary>
        public string? OwnerID { get; set; }

        /// <summary>
        /// 作者名稱
        /// </summary>
        [Display(Name = "作者")]
        public string? OwnerName { get; set; }

        /// <summary>
        /// 內容
        /// </summary>
        [Required(ErrorMessage = "必須要有內容")]
        [StringLength(Parameters.ArticleLength, ErrorMessage = "{0} 最多只能有 {1} 個字元")]
        [Display(Name = "內容")]
        public required string Content { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        [DataType(DataType.DateTime)]
        [Display(Name = "時間")]
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 觀看數
        /// </summary>
        [Display(Name = "觀看數")]
        public int Views { get; set; }

        /// <summary>
        /// Collection navigation to Comments
        /// </summary>
        public ICollection<Comment>? Comments { get; set; }
    }
}
