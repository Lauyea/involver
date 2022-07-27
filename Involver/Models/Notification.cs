using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Involver.Models
{
    public class Notification
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool IsRead { get; set; }

        [DataType(DataType.Url)]
        [Display(Name = "通知網址")]
        [StringLength(128)]
        public string Url { get; set; }

        [Required]
        public string ProfileID { get; set; }

        [ForeignKey("ProfileID")]
        public Profile Profile { get; set; }
    }
}
