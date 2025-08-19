using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class Message
    {
        public int MessageID { get; set; }

        [Required(ErrorMessage = "必須要有內容")]
        [StringLength(1024, ErrorMessage = "{0} 最多只能有 {1} 個字元")]
        [Display(Name = "內容")]
        public required string Content { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "時間")]
        public DateTime UpdateTime { get; set; }

        public bool Block { get; set; }

        /// <summary>
        /// FK to Comment
        /// </summary>
        public int CommentID { get; set; }
        /// <summary>
        /// Reference navigation to Comment
        /// </summary>
        public Comment? Comment { get; set; }

        /// <summary>
        /// FK to Profile
        /// </summary>
        // Delete Profile then Set NULL
        // 當初可能是為了保留Message，才設定成nullable的
        public string? ProfileID { get; set; }
        /// <summary>
        /// Reference navigation to Profile
        /// </summary>
        [ForeignKey("ProfileID")]
        public Profile? Profile { get; set; }

        public ICollection<Agree>? Agrees { get; set; }
    }
}