﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Models.Database
{
    [Index("CommentId", Name = "IX_Dice_CommentID")]
    public partial class Die
    {
        [Key]
        [Column("DiceID")]
        public int DiceId { get; set; }
        public int Sides { get; set; }
        public int Value { get; set; }
        [Column("CommentID")]
        public int CommentId { get; set; }

        [ForeignKey("CommentId")]
        [InverseProperty("Dice")]
        public virtual Comment Comment { get; set; } = null!;
    }
}