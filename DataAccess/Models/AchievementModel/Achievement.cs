using DataAccess.Common;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.AchievementModel
{
    /// <summary>
    /// 成就表
    /// </summary>
    public class Achievement
    {
        /// <summary>
        /// PK
        /// </summary>
        public int AchievementID { get; set; }

        /// <summary>
        /// 變數名稱。參考 AchievementNames
        /// </summary>
        [Required]
        [StringLength(Parameters.AchievementCodeLength)]
        public required string Code { get; set; }

        /// <summary>
        /// 名稱
        /// </summary>
        [Required]
        [StringLength(Parameters.AchievementCodeLength)]
        public required string Title { get; set; }

        /// <summary>
        /// 說明
        /// </summary>
        [StringLength(Parameters.AchievementContentLength)]
        public string? Content { get; set; }

        /// <summary>
        /// Collection navigation to Profiles
        /// </summary>
        public ICollection<Profile>? Profiles { get; set; }

        /// <summary>
        /// Many-to-many join table for Profiles
        /// </summary>
        public List<ProfileAchievement>? ProfileAchievements { get; set; }

        /// <summary>
        /// 等級
        /// </summary>
        public int Rank { get; set; }
    }
}
