using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models
{
    public class ViewIp
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public required string Ip { get; set; }

        public ICollection<Novel>? Novels { get; set; }

        public ICollection<Article>? Articles { get; set; }
    }
}
