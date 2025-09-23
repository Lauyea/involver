using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using DataAccess.Common;

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
        [StringLength(Parameters.ArticleTitleLength, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 2)]
        [Display(Name = "標題")]
        public required string Title { get; set; }

        /// <summary>
        /// 內容
        /// </summary>
        [Required(ErrorMessage = "必須要有內容")]
        [StringLength(Parameters.ArticleLength, ErrorMessage = "{0} 最多只能有 {1} 個字元")]
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
        [Display(Name = "總觀看數")]
        public int TotalViews { get; set; }

        /// <summary>
        /// 主題圖片網址
        /// </summary>
        [DataType(DataType.ImageUrl)]
        [Display(Name = "主題圖片網址")]
        [StringLength(Parameters.ImageUrlLength)]
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
        /// 每日觀看數
        /// </summary>
        public int DailyView { get; set; }

        /// <summary>
        /// 軟刪除Tag
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// 評論順序是否固定
        /// </summary>
        public bool IsCommentOrderFixed { get; set; } = false;

        /// <summary>
        /// 文章類型
        /// </summary>
        [Required]
        public ArticleType Type { get; set; }

        /// <summary>
        /// 是否接受。會有In幣獎勵。
        /// </summary>
        /// <remarks>
        /// 如果是 Feedback 才有的設定。
        /// </remarks>
        public bool? Accept { get; set; } // 新增的採納狀態，設為 nullable

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
        /// Collection navigation to Views
        /// </summary>
        public ICollection<View>? Views { get; set; }

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