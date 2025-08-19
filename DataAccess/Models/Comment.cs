using DataAccess.Models.AnnouncementModel;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.FeedbackModel;
using DataAccess.Models.NovelModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class Comment
    {
        public int CommentID { get; set; }

        [Required(ErrorMessage = "必須要有內容")]
        [StringLength(16384, ErrorMessage = "{0} 最多只能有 {1} 個字元")]
        [Display(Name = "內容")]
        public required string Content { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "時間")]
        public DateTime UpdateTime { get; set; }

        public bool Block { get; set; }

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

        /// <summary>
        /// FK to Novel
        /// </summary>
        public int? NovelID { get; set; }
        /// <summary>
        /// Reference navigation to Novel
        /// </summary>
        public Novel? Novel { get; set; }

        /// <summary>
        /// FK to Episode
        /// </summary>
        public int? EpisodeID { get; set; }
        /// <summary>
        /// Reference navigation to Episode
        /// </summary>
        public Episode? Episode { get; set; }

        /// <summary>
        /// FK to Announcement
        /// </summary>
        public int? AnnouncementID { get; set; }
        /// <summary>
        /// Reference navigation to Announcement
        /// </summary>
        public Announcement? Announcement { get; set; }

        /// <summary>
        /// FK to Feedback
        /// </summary>
        public int? FeedbackID { get; set; }
        /// <summary>
        /// Reference navigation to Feedback
        /// </summary>
        public Feedback? Feedback { get; set; }

        /// <summary>
        /// FK to Article
        /// </summary>
        public int? ArticleID { get; set; }
        /// <summary>
        /// Reference navigation to Article
        /// </summary>
        public Article? Article { get; set; }

        /// <summary>
        /// Collection navigation to Agrees
        /// </summary>
        public ICollection<Agree>? Agrees { get; set; }

        /// <summary>
        /// Collection navigation to Messages
        /// </summary>
        public ICollection<Message>? Messages { get; set; }

        /// <summary>
        /// Collection navigation to Dices
        /// </summary>
        public ICollection<Dice>? Dices { get; set; }
    }
}