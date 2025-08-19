using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models
{
    /// <summary>
    /// 紀錄文章與創作的觀看者IP
    /// </summary>
    /// TODO: 之後會改成View去記錄觀看數，以及改紀錄 cookie session id
    public class ViewIp
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(20)]
        public required string Ip { get; set; }

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
