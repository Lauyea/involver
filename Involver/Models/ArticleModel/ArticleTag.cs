using System.ComponentModel.DataAnnotations;

namespace Involver.Models.ArticleModel
{
    public class ArticleTag
    {
        [Key]
        public int TagId { get; set; }

        [Required]
        [MaxLength(15)]
        public string Name { get; set; }

        public ICollection<Article> Articles { get; set; }
    }
}
