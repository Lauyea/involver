using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Involver.Models
{
    public class Achievements
    {
        public int AchievementsID { get; set; }
        [Display(Name = "Beta Involver")]
        public bool BetaInvolver { get; set; }
        [Display(Name = "解鎖時間")]
        public DateTime TimeBetaInvolver { get; set; }
        [Display(Name = "特約作者")]
        public bool Professional { get; set; }
        [Display(Name = "解鎖時間")]
        public DateTime TimeProfessional { get; set; }

        [Required]
        public string ProfileID { get; set; }
        [ForeignKey("ProfileID")]
        public Profile Profile { get; set; }
    }
}
