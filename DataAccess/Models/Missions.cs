using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models
{
    public class Missions
    {
        public int MissionsID { get; set; }
        [Display(Name = "看一篇文章")]
        public bool WatchArticle { get; set; }
        [Display(Name = "投一次票")]
        public bool Vote { get; set; }
        [Display(Name = "留一則評論")]
        public bool LeaveComment { get; set; }
        [Display(Name = "瀏覽公告")]
        public bool ViewAnnouncement { get; set; }
        [Display(Name = "分享一次創作")]
        public bool ShareCreation { get; set; }
        [Display(Name = "被認同一次")]
        public bool BeAgreed { get; set; }
        [Display(Name = "完成其他的所有每週任務")]
        public bool CompleteOtherMissions { get; set; }
        [Display(Name = "每日登入")]
        public bool DailyLogin { get; set; }

        /// <summary>
        /// FK to Profile
        /// </summary>
        [Required]
        public required string ProfileID { get; set; }
        /// <summary>
        /// Reference navigation to Profile
        /// </summary>
        [ForeignKey("ProfileID")]
        public Profile? Profile { get; set; }
    }
}