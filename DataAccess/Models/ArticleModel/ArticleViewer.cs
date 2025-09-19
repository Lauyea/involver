using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models.ArticleModel
{
    public class ArticleViewer
    {
        /// <summary>
        /// Sequence number
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int SeqNo { get; set; }

        /// <summary>
        /// 觀看日期
        /// </summary>
        public DateTime ViewDate { get; set; }

        /// <summary>
        /// FK to Profile
        /// </summary>
        public string? ProfileID { get; set; }
        /// <summary>
        /// Reference navigation to Profile
        /// </summary>
        public Profile? Profile { get; set; }

        /// <summary>
        /// FK to Article
        /// </summary>
        public int ArticleID { get; set; }
        /// <summary>
        /// Reference navigation to Article
        /// </summary>
        public Article? Article { get; set; }
    }
}