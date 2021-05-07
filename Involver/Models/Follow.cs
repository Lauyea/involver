using Involver.Models.NovelModel;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Involver.Models
{
    public class Follow
    {
        public int FollowID { get; set; }

        [Required]
        public string FollowerID { get; set; }
        [ForeignKey("FollowerID")]
        [InverseProperty("Followers")]
        public Profile Follower { get; set; }

        public bool ProfileMonthlyInvolver { get; set; }
        public bool NovelMonthlyInvolver { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Update Time")]
        public DateTime UpdateTime { get; set; }

        //這個不用Required，因為這是被Follow的對象，可以是NULL
        public string ProfileID { get; set; }
        [ForeignKey("ProfileID")]
        [InverseProperty("Follows")]
        public Profile Profile { get; set; }

        public int? NovelID { get; set; }
        public Novel Novel { get; set; }
    }
}