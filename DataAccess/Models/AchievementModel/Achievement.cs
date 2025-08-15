using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.AchievementModel
{
    public class Achievement
    {
        public int AchievementID { get; set; }

        [Required]
        [StringLength(32)]
        public required string Code { get; set; }

        [Required]
        [StringLength(32)]
        public required string Title { get; set; }

        [StringLength(128)]
        public string? Content { get; set; }

        public ICollection<Profile>? Profiles { get; set; }

        public List<ProfileAchievement>? ProfileAchievements { get; set; }

        public int Rank { get; set; }
    }
}
