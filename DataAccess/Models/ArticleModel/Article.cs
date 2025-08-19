using DataAccess.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Models.ArticleModel
{
    public class Article
    {
        /// <summary>
        /// PK
        /// </summary>
        public int ArticleID { get; set; }

        /// <summary>
        /// 標題
        /// </summary>
        [Required(ErrorMessage = "必須要有標題")]
        [StringLength(50, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 2)]
        [Display(Name = "標題")]
        public required string Title { get; set; }

        /// <summary>
        /// 內容
        /// </summary>
        [Required(ErrorMessage = "必須要有內容")]
        [StringLength(65536, ErrorMessage = "{0} 最多只能有 {1} 個字元")]
        [Display(Name = "內容")]
        public required string Content { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [DataType(DataType.DateTime)]
        [Display(Name = "建立時間")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 更新時間
        /// </summary>
        [DataType(DataType.DateTime)]
        [Display(Name = "更新時間")]
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 觀看數
        /// </summary>
        [Display(Name = "觀看數")]
        public int Views { get; set; }

        /// <summary>
        /// 主題圖片網址
        /// </summary>
        [DataType(DataType.ImageUrl)]
        [Display(Name = "主題圖片網址")]
        [StringLength(1024)]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// 是否封鎖
        /// </summary>
        public bool Block { get; set; }

        /// <summary>
        /// 總In幣數
        /// </summary>
        [Column(TypeName = "money")]
        public decimal TotalCoins { get; set; }

        /// <summary>
        /// 月In幣數
        /// </summary>
        [Column(TypeName = "money")]
        public decimal MonthlyCoins { get; set; }

        /// <summary>
        /// Gets or sets the JSON representation of the view record.
        /// </summary>
        /// TODO: 之後可能會刪除
        public string? ViewRecordJson { get; set; }

        /// <summary>
        /// 每日觀看數
        /// </summary>
        public int DailyView { get; set; }

        /// <summary>
        /// FK to Profile
        /// </summary>
        [Required]
        public required string ProfileID { get; set; }
        /// <summary>
        /// Reference navigation to Profile
        /// </summary>
        [ForeignKey("ProfileID")]
        [InverseProperty("Articles")]
        public Profile? Profile { get; set; }

        /// <summary>
        /// Collection navigation to Comments
        /// </summary>
        public ICollection<Comment>? Comments { get; set; }

        /// <summary>
        /// Collection navigation to Involvers
        /// </summary>
        public ICollection<Involving>? Involvers { get; set; }

        /// <summary>
        /// Collection navigation to ArticleTags
        /// </summary>
        public ICollection<ArticleTag>? ArticleTags { get; set; }

        /// <summary>
        /// Collection navigation to ViewIps
        /// </summary>
        public ICollection<ViewIp>? ViewIps { get; set; }

        /// <summary>
        /// Collection navigation to Viewers
        /// </summary>
        public ICollection<Profile>? Viewers { get; set; }

        /// <summary>
        /// Many-to-many join table for Viewers
        /// </summary>
        public List<ArticleViewer>? ArticleViewers { get; set; }
    }
}
