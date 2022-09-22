﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Models.Database
{
    [Table("Message")]
    [Index("CommentId", Name = "IX_Message_CommentID")]
    [Index("ProfileId", Name = "IX_Message_ProfileID")]
    public partial class Message
    {
        public Message()
        {
            Agrees = new HashSet<Agree>();
        }

        [Key]
        [Column("MessageID")]
        public int MessageId { get; set; }
        [StringLength(1024)]
        public string Content { get; set; } = null!;
        public DateTime UpdateTime { get; set; }
        public bool Block { get; set; }
        [Column("CommentID")]
        public int CommentId { get; set; }
        [Column("ProfileID")]
        public string? ProfileId { get; set; }

        [ForeignKey("CommentId")]
        [InverseProperty("Messages")]
        public virtual Comment Comment { get; set; } = null!;
        [ForeignKey("ProfileId")]
        [InverseProperty("Messages")]
        public virtual Profile? Profile { get; set; }
        [InverseProperty("Message")]
        public virtual ICollection<Agree> Agrees { get; set; }
    }
}