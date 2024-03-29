﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Models.Database
{
    [Table("ArticleViewer")]
    [Index("ArticleId", Name = "IX_ArticleViewer_ArticleID")]
    public partial class ArticleViewer
    {
        [Key]
        [Column("ProfileID")]
        public string ProfileId { get; set; } = null!;
        [Key]
        [Column("ArticleID")]
        public int ArticleId { get; set; }
        public int SeqNo { get; set; }
        public DateTime ViewDate { get; set; }

        [ForeignKey("ArticleId")]
        [InverseProperty("ArticleViewers")]
        public virtual Article Article { get; set; } = null!;
        [ForeignKey("ProfileId")]
        [InverseProperty("ArticleViewers")]
        public virtual Profile Profile { get; set; } = null!;
    }
}