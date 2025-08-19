using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models.AchievementModel
{
    /// <summary>
    /// Many-to-many join table for Profile and Achievement
    /// </summary>
    public class ProfileAchievement
    {
        /// <summary>
        /// Sequence number
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int SeqNo { get; set; }

        /// <summary>
        /// 成就日期
        /// </summary>
        public DateTime AchieveDate { get; set; }

        /// <summary>
        /// FK to Profile
        /// </summary>
        public string? ProfileID { get; set; }
        /// <summary>
        /// Navigation to Profile
        /// </summary>
        public Profile? Profile { get; set; }

        /// <summary>
        /// FK to Achievement
        /// </summary>
        public int AchievementID { get; set; }
        /// <summary>
        /// Navigation to Achievement
        /// </summary>
        public Achievement? Achievement { get; set; }
    }
}
