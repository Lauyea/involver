using System.ComponentModel.DataAnnotations.Schema;

namespace Involver.Models.NovelModel
{
    public class NovelViewer
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int SeqNo { get; set; }

        public DateTime ViewDate { get; set; }

        public string ProfileID { get; set; }
        public Profile Profile { get; set; }

        public int NovelID { get; set; }
        public Novel Novel { get; set; }
    }
}
