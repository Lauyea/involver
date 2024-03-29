﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Models.Database
{
    [Table("Involving")]
    [Index("ArticleId", Name = "IX_Involving_ArticleID")]
    [Index("InvolverId", Name = "IX_Involving_InvolverID")]
    [Index("NovelId", Name = "IX_Involving_NovelID")]
    [Index("ProfileId", Name = "IX_Involving_ProfileID")]
    public partial class Involving
    {
        [Key]
        [Column("InvolvingID")]
        public int InvolvingId { get; set; }
        [Column(TypeName = "money")]
        public decimal Value { get; set; }
        [Column(TypeName = "money")]
        public decimal MonthlyValue { get; set; }
        [Column(TypeName = "money")]
        public decimal TotalValue { get; set; }
        public DateTime LastTime { get; set; }
        [Column("ProfileID")]
        public string? ProfileId { get; set; }
        [Column("NovelID")]
        public int? NovelId { get; set; }
        [Column("ArticleID")]
        public int? ArticleId { get; set; }
        [Column("InvolverID")]
        public string InvolverId { get; set; } = null!;

        [ForeignKey("ArticleId")]
        [InverseProperty("Involvings")]
        public virtual Article? Article { get; set; }
        [ForeignKey("InvolverId")]
        [InverseProperty("InvolvingInvolvers")]
        public virtual Profile Involver { get; set; } = null!;
        [ForeignKey("NovelId")]
        [InverseProperty("Involvings")]
        public virtual Novel? Novel { get; set; }
        [ForeignKey("ProfileId")]
        [InverseProperty("InvolvingProfiles")]
        public virtual Profile? Profile { get; set; }
    }
}