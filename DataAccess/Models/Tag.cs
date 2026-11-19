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
        /// 是否為分類標籤。分類由管理員在後台維護，在新增、編輯作品時和一般標籤分開選，不佔一般標籤的名額。
        /// 一定和 <see cref="CategoryScope"/> 一起設定：是分類時範圍不為 <see cref="CategoryScope.None"/>
        /// </summary>
        [Display(Name = "分類")]
        public bool IsCategory { get; set; }

        /// <summary>
        /// 分類適用的作品類型（小說、文章或兩者）；不適用的作品上，這個標籤只是一般標籤
        /// </summary>
        [Display(Name = "分類適用")]
        public CategoryScope CategoryScope { get; set; }

        /// <summary>
        /// 分類的顯示順序（小的在前）；不是分類時不使用
        /// </summary>
        [Display(Name = "分類順序")]
        public int CategoryOrder { get; set; }

        /// <summary>
        /// 分類的 Font Awesome 圖示（例如 <c>fa-hat-wizard</c>）；沒有時顯示預設圖示
        /// </summary>
        [MaxLength(50)]
        [Display(Name = "分類圖示")]
        public string? CategoryIcon { get; set; }

        /// <summary>
        /// Collection navigation to Novels
        /// </summary>
        /// <summary>
        /// 是否為適用於指定作品類型的分類
        /// </summary>
        /// <param name="scope">作品類型</param>
        /// <returns>是否為適用的分類</returns>
        public bool IsCategoryFor(CategoryScope scope) => IsCategory && (CategoryScope & scope) != 0;

        public ICollection<Novel>? Novels { get; set; }

        /// <summary>
        /// Collection navigation to Articles
        /// </summary>
        public ICollection<Article>? Articles { get; set; }

        /// <summary>
        /// Collection navigation to 別名
        /// </summary>
        public ICollection<TagAlias>? Aliases { get; set; }
    }
}
