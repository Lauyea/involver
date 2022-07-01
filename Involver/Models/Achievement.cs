namespace Involver.Models
{
    public class Achievement
    {
        public int AchievementId { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }

        public DateTime? AchievementDate { get; set; }

        public ICollection<Profile> Profiles { get; set; }
    }
}
