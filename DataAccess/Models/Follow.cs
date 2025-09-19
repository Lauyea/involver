using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using DataAccess.Models.NovelModel;

namespace DataAccess.Models
{
    public class Follow
    {
        public int FollowID { get; set; }

        /// <summary>
        /// FK to Follower(Follow者)
        /// </summary>
        [Required]
        public required string FollowerID { get; set; }
        /// <summary>
        /// Reference navigation to Follower
        /// </summary>
        [ForeignKey("FollowerID")]
        [InverseProperty("Followers")]
        public Profile? Follower { get; set; }

        /// <summary>
        /// 是否為對個人的月贊助者
        /// </summary>
        public bool ProfileMonthlyInvolver { get; set; }

        /// <summary>
        /// 是否為對創作的月贊助者
        /// </summary>
        public bool NovelMonthlyInvolver { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Update Time")]
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// FK to being followed one(被Follow者)
        /// </summary>
        // 這個不用Required，因為這是被Follow的對象，可以是NULL
        public string? ProfileID { get; set; }

        /// <summary>
        /// Reference navigation to being followed one
        /// </summary>
        [ForeignKey("ProfileID")]
        [InverseProperty("Follows")]
        public Profile? Profile { get; set; }

        /// <summary>
        /// FK to Novel
        /// </summary>
        public int? NovelID { get; set; }
        /// <summary>
        /// Reference navigation to Novel
        /// </summary>
        public Novel? Novel { get; set; }
    }
}