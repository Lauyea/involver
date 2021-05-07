using Involver.Models.AnnouncementModel;
using Involver.Models.ArticleModel;
using Involver.Models.FeedbackModel;
using Involver.Models.NovelModel;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Involver.Models
{
    public class Comment
    {
        public int CommentID { get; set; }
        [Required(ErrorMessage = "必須要有內容")]
        [StringLength(16384, ErrorMessage = "{0} 最多只能有 {1} 個字元")]
        [Display(Name = "內容")]
        public string Content { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "時間")]
        public DateTime UpdateTime { get; set; }

        public bool Block { get; set; }

        [Required]
        public string ProfileID { get; set; }
        [ForeignKey("ProfileID")]
        public Profile Profile { get; set; }

        public int? NovelID { get; set; }
        public Novel Novel { get; set; }

        public int? EpisodeID { get; set; }
        public Episode Episode { get; set; }

        public int? AnnouncementID { get; set; }
        public Announcement Announcement { get; set; }

        public int? FeedbackID { get; set; }
        public Feedback Feedback { get; set; }

        public int? ArticleID { get; set; }
        public Article Article { get; set; }

        public ICollection<Agree> Agrees { get; set; }

        public ICollection<Message> Messages { get; set; }

        public ICollection<Dice> Dices { get; set; }
    }
}