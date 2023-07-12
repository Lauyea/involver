using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Involver.Models
{
    public class Message
    {
        public int MessageID { get; set; }

        [Required(ErrorMessage = "必須要有內容")]
        [StringLength(1024, ErrorMessage = "{0} 最多只能有 {1} 個字元")]
        [Display(Name = "內容")]
        public string Content { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "時間")]
        public DateTime UpdateTime { get; set; }

        public bool Block { get; set; }

        public int CommentID { get; set; }

        public Comment Comment { get; set; }

        //Delete Profile then Set NULL
        public string ProfileID { get; set; }

        [ForeignKey("ProfileID")]
        public Profile Profile { get; set; }

        public ICollection<Agree> Agrees { get; set; }
    }
}