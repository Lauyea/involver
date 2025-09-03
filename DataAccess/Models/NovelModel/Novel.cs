using DataAccess.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models.NovelModel
{
    public class Novel
    {
        public int NovelID { get; set; }

        [Required(ErrorMessage = "必須要有標題")]
        [StringLength(Parameters.ArticleTitleLength, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 2)]
        [Display(Name = "標題")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "必須要有介紹內容")]
        [StringLength(Parameters.NovelIntroLength, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 8)]
        [Display(Name = "介紹")]
        public required string Introduction { get; set; }

        [DataType(DataType.ImageUrl)]
        [Display(Name = "主題圖片網址")]
        [StringLength(Parameters.ImageUrlLength)]
        public string? ImageUrl { get; set; }

        [DisplayFormat(NullDisplayText = "No type")]
        [Display(Name = "類型")]
        public Common.Type? Type { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "創建時間")]
        public DateTime CreateTime { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "更新時間")]
        public DateTime UpdateTime { get; set; }

        
        [Column(TypeName = "money")]
        public decimal TotalCoins { get; set; }

        
        [Column(TypeName = "money")]
        public decimal MonthlyCoins { get; set; }

        [Display(Name = "付費限定")]
        public bool PrimeRead { get; set; }

        [Display(Name = "完結")]
        public bool End { get; set; }

        [Display(Name = "總觀看數")]
        public int TotalViews { get; set; }

        [Display(Name = "封鎖")]
        public bool Block { get; set; }

        public int DailyView { get; set; }

        /// <summary>
        /// 軟刪除Tag
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// FK to ProfileID
        /// </summary>
        [Required]
        public required string ProfileID { get; set; }
        /// <summary>
        /// Reference navigation to Profile
        /// </summary>
        [ForeignKey("ProfileID")]
        [InverseProperty("Novels")]
        public Profile? Profile { get; set; }

        /// <summary>
        /// Collection navigation to Episodes
        /// </summary>
        public ICollection<Episode>? Episodes { get; set; }

        /// <summary>
        /// Collection navigation to Comments
        /// </summary>
        public ICollection<Comment>? Comments { get; set; }

        /// <summary>
        /// Collection navigation to Involvers
        /// </summary>
        public ICollection<Involving>? Involvers { get; set; }

        /// <summary>
        /// Collection navigation to Follows
        /// </summary>
        public ICollection<Follow>? Follows { get; set; }

        /// <summary>
        /// Collection navigation to NovelTags
        /// </summary>
        public ICollection<NovelTag>? NovelTags { get; set; }

        /// <summary>
        /// Collection navigation to Views
        /// </summary>
        public ICollection<View>? Views { get; set; }

        /// <summary>
        /// Collection navigation to Viewers
        /// </summary>
        public ICollection<Profile>? Viewers { get; set; }
        /// <summary>
        /// Many-to-many join table for novel and viewers
        /// </summary>
        public List<NovelViewer>? NovelViewers { get; set; }
    }
}
