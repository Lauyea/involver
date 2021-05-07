using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Involver.Models
{
    public class Agree
    {
        public int AgreeID { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "時間")]
        public DateTime UpdateTime { get; set; }
        public int? CommentID { get; set; }
        public Comment Comment { get; set; }
        public int? MessageID { get; set; }
        public Message Message { get; set; }

        [Required]
        public string ProfileID { get; set; }
        [ForeignKey("ProfileID")]
        public Profile Profile { get; set; }
    }
}
