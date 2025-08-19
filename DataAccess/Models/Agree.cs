using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Models
{
    /// <summary>
    /// 同意、按讚
    /// </summary>
    public class Agree
    {
        public int AgreeID { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "時間")]
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// FK to Comment
        /// </summary>
        public int? CommentID { get; set; }
        /// <summary>
        /// Reference navigation to Comment
        /// </summary>
        public Comment? Comment { get; set; }

        /// <summary>
        /// FK to Message
        /// </summary>
        public int? MessageID { get; set; }
        /// <summary>
        /// Reference navigation to Message
        /// </summary>
        public Message? Message { get; set; }

        /// <summary>
        /// FK to Profile
        /// </summary>
        [Required]
        public required string ProfileID { get; set; }
        /// <summary>
        /// Reference navigation to Profile
        /// </summary>
        [ForeignKey("ProfileID")]
        public Profile? Profile { get; set; }
    }
}
