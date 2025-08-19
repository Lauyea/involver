using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.ArticleModel
{
    /// <summary>
    /// 文章標籤。
    /// </summary>
    /// TODO: 之後可能會與Novel共用
    public class ArticleTag
    {
        [Key]
        public int TagId { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Required]
        [MaxLength(15)]
        public required string Name { get; set; }

        /// <summary>
        /// Collection navigation to Articles
        /// </summary>
        public ICollection<Article>? Articles { get; set; }
    }
}
