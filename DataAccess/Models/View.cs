using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using DataAccess.Data;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;

namespace DataAccess.Models
{
    public class View
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// FK to Article
        /// </summary>
        public int? ArticleId { get; set; }
        /// <summary>
        /// Refer to Article
        /// </summary>
        [ForeignKey("ArticleId")]
        public virtual Article? Article { get; set; }

        /// <summary>
        /// FK to Novel
        /// </summary>
        public int? NovelId { get; set; }
        /// <summary>
        /// Refer to Novel
        /// </summary>
        [ForeignKey("NovelId")]
        public virtual Novel? Novel { get; set; }

        public string? UserId { get; set; }

        public string? SessionId { get; set; }

        public DateTime CreateTime { get; set; }
    }
}