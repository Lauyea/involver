using System.ComponentModel.DataAnnotations.Schema;

namespace Involver.Models.ArticleModel
{
    public class ArticleViewer
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int SeqNo { get; set; }

        public DateTime ViewDate { get; set; }

        public string ProfileID { get; set; }
        public Profile Profile { get; set; }

        public int ArticleID { get; set; }
        public Article Article { get; set; }
    }
}
