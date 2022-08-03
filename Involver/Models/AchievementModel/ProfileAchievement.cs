using System.ComponentModel.DataAnnotations.Schema;

namespace Involver.Models.AchievementModel
{
    public class ProfileAchievement
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int SeqNo { get; set; }
        public DateTime AchieveDate { get; set; }

        public string ProfileID { get; set; }
        public Profile Profile { get; set; }

        public int AchievementID { get; set; }
        public Achievement Achievement { get; set; }
    }
}
