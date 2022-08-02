using Involver.Models.ArticleModel;
using Involver.Models.NovelModel;
using System.ComponentModel.DataAnnotations;

namespace Involver.Models
{
    public class ViewIp
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public string Ip { get; set; }

        public ICollection<Novel> Novels { get; set; }

        public ICollection<Article> Articles { get; set; }
    }
}
