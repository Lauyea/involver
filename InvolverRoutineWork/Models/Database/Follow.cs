﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Data.Models.Database
{
    [Table("Follow")]
    [Index("FollowerId", Name = "IX_Follow_FollowerID")]
    [Index("NovelId", Name = "IX_Follow_NovelID")]
    [Index("ProfileId", Name = "IX_Follow_ProfileID")]
    public partial class Follow
    {
        [Key]
        [Column("FollowID")]
        public int FollowId { get; set; }
        [Column("FollowerID")]
        public string FollowerId { get; set; } = null!;
        public bool ProfileMonthlyInvolver { get; set; }
        public bool NovelMonthlyInvolver { get; set; }
        public DateTime UpdateTime { get; set; }
        [Column("ProfileID")]
        public string? ProfileId { get; set; }
        [Column("NovelID")]
        public int? NovelId { get; set; }

        [ForeignKey("FollowerId")]
        [InverseProperty("FollowFollowers")]
        public virtual Profile Follower { get; set; } = null!;
        [ForeignKey("NovelId")]
        [InverseProperty("Follows")]
        public virtual Novel? Novel { get; set; }
        [ForeignKey("ProfileId")]
        [InverseProperty("FollowProfiles")]
        public virtual Profile? Profile { get; set; }
    }
}