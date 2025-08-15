using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.ArticleModel
{
    public class ArticleTag
    {
        [Key]
        public int TagId { get; set; }

        [Required]
        [MaxLength(15)]
        public required string Name { get; set; }

        public ICollection<Article>? Articles { get; set; }
    }
}
