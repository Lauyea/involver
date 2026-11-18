using System.ComponentModel.DataAnnotations;

using DataAccess.Common;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;

namespace DataAccess.Models
{
    /// <summary>
    /// 標籤。小說和文章共用同一張表，分別透過 NovelTaggings、ArticleTaggings 兩張關聯表連到作品。
    /// </summary>
    public class Tag
    {
        /// <summary>
        /// 標籤 ID
        /// </summary>
        [Key]
        public int TagId { get; set; }

        /// <summary>
        /// 名稱。保留作者原本輸入的寫法，用來顯示。
        /// </summary>
        [Required]
        [MaxLength(Parameters.TagNameMaxLength)]
        [Display(Name = "名稱")]
        public required string Name { get; set; }

        /// <summary>
        /// 統一格式後的名稱（Unique）。所有比對都用這個欄位。
        /// 格式規則見 Involver.Helpers.TagNameNormalizer。
        /// </summary>
        [Required]
        [MaxLength(Parameters.TagNormalizedNameMaxLength)]
        public required string NormalizedName { get; set; }

        /// <summary>
        /// 狀態（正式／待審）
        /// </summary>
        [Display(Name = "狀態")]
        public TagStatus Status { get; set; } = TagStatus.Approved;

        /// <summary>
        /// 建立者 ProfileID。資料搬移前就存在的標籤沒有建立者。
        /// </summary>
        [MaxLength(450)]
        public string? CreatorProfileID { get; set; }

        /// <summary>
        /// Reference navigation to 建立者
        /// </summary>
        public Profile? CreatorProfile { get; set; }

        /// <summary>
        /// 建立時間
        /// </summary>
        [Display(Name = "建立時間")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// Collection navigation to Novels
        /// </summary>
        public ICollection<Novel>? Novels { get; set; }

        /// <summary>
        /// Collection navigation to Articles
        /// </summary>
        public ICollection<Article>? Articles { get; set; }
    }
}
