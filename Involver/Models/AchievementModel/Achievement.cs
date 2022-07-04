using System.ComponentModel.DataAnnotations;

namespace Involver.Models.AchievementModel
{
    public class Achievement
    {
        public int AchievementID { get; set; }

        [Required]
        [StringLength(32)]
        public string Title { get; set; }

        [StringLength(128)]
        public string Content { get; set; }

        public ICollection<Profile> Profiles { get; set; }

        public List<ProfileAchievement> ProfileAchievements { get; set; }
    }
}
