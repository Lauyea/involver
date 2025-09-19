using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models.NovelModel
{
    /// <summary>
    /// Many-to-many join table for novel and viewer(Profile)
    /// </summary>
    public class NovelViewer
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int SeqNo { get; set; }

        public DateTime ViewDate { get; set; }

        public string? ProfileID { get; set; }
        public Profile? Profile { get; set; }

        public int NovelID { get; set; }
        public Novel? Novel { get; set; }
    }
}