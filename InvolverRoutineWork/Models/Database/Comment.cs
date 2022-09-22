﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Models.Database
{
    [Table("Comment")]
    [Index("AnnouncementId", Name = "IX_Comment_AnnouncementID")]
    [Index("ArticleId", Name = "IX_Comment_ArticleID")]
    [Index("EpisodeId", Name = "IX_Comment_EpisodeID")]
    [Index("FeedbackId", Name = "IX_Comment_FeedbackID")]
    [Index("NovelId", Name = "IX_Comment_NovelID")]
    [Index("ProfileId", Name = "IX_Comment_ProfileID")]
    public partial class Comment
    {
        public Comment()
        {
            Agrees = new HashSet<Agree>();
            Dice = new HashSet<Die>();
            Messages = new HashSet<Message>();
        }

        [Key]
        [Column("CommentID")]
        public int CommentId { get; set; }
        public string Content { get; set; } = null!;
        public DateTime UpdateTime { get; set; }
        public bool Block { get; set; }
        [Column("ProfileID")]
        public string ProfileId { get; set; } = null!;
        [Column("NovelID")]
        public int? NovelId { get; set; }
        [Column("EpisodeID")]
        public int? EpisodeId { get; set; }
        [Column("AnnouncementID")]
        public int? AnnouncementId { get; set; }
        [Column("FeedbackID")]
        public int? FeedbackId { get; set; }
        [Column("ArticleID")]
        public int? ArticleId { get; set; }

        [ForeignKey("AnnouncementId")]
        [InverseProperty("Comments")]
        public virtual Announcement? Announcement { get; set; }
        [ForeignKey("ArticleId")]
        [InverseProperty("Comments")]
        public virtual Article? Article { get; set; }
        [ForeignKey("EpisodeId")]
        [InverseProperty("Comments")]
        public virtual Episode? Episode { get; set; }
        [ForeignKey("FeedbackId")]
        [InverseProperty("Comments")]
        public virtual Feedback? Feedback { get; set; }
        [ForeignKey("NovelId")]
        [InverseProperty("Comments")]
        public virtual Novel? Novel { get; set; }
        [ForeignKey("ProfileId")]
        [InverseProperty("Comments")]
        public virtual Profile Profile { get; set; } = null!;
        [InverseProperty("Comment")]
        public virtual ICollection<Agree> Agrees { get; set; }
        [InverseProperty("Comment")]
        public virtual ICollection<Die> Dice { get; set; }
        [InverseProperty("Comment")]
        public virtual ICollection<Message> Messages { get; set; }
    }
}