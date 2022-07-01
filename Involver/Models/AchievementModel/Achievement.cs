using System.ComponentModel.DataAnnotations;

namespace Involver.Models.AchievementModel
{
    public class Achievement
    {
        public int AchievementID { get; set; }

        [Required]
        [StringLength(8)]
        public string Title { get; set; }

        [StringLength(64)]
        public string Content { get; set; }

        public ICollection<Profile> Profiles { get; set; }

        public List<ProfileAchievement> ProfileAchievements { get; set; }
    }
}
