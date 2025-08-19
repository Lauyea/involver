using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using DataAccess.Models.AchievementModel;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;

namespace DataAccess.Models
{
    public class Profile
    {
        /// <summary>
        /// 創建時系統產生的ID
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public required string ProfileID { get; set; }

        [StringLength(256, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 10)]
        [Display(Name = "介紹")]
        public string? Introduction { get; set; }

        /// <summary>
        /// 頭像圖片網址
        /// </summary>
        [DataType(DataType.ImageUrl)]
        [Display(Name = "頭像圖片網址")]
        [StringLength(128)]
        public string? ImageUrl { get; set; }

        /// <summary>
        /// 橫幅圖片網址
        /// </summary>
        [DataType(DataType.ImageUrl)]
        [Display(Name = "橫幅圖片網址")]
        [StringLength(128)]
        public string? BannerImageUrl { get; set; }


        [Column(TypeName = "money")]
        [Display(Name = "實體In幣")]
        public decimal RealCoins { get; set; }

        [Column(TypeName = "money")]
        [Display(Name = "虛擬In幣")]
        public decimal VirtualCoins { get; set; }

        [Column(TypeName = "money")]
        [Display(Name = "月收入")]
        public decimal MonthlyCoins { get; set; }

        [Required]
        [StringLength(50, ErrorMessage = "長度必須在2到50個字元之間", MinimumLength = 2)]
        [Display(Name = "用戶名")]
        public required string UserName { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "註冊時間")]
        public DateTime EnrollmentDate { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "最後登入時間")]
        public DateTime LastTimeLogin { get; set; }

        /// <summary>
        /// 特約作家
        /// </summary>
        [Display(Name = "特約作家")]
        public bool Professional { get; set; }

        /// <summary>
        /// 會員
        /// </summary>
        [Display(Name = "會員")]
        public bool Prime { get; set; }

        [Display(Name = "封鎖")]
        public bool Banned { get; set; }

        [Display(Name = "可改變用戶名")]
        public bool CanChangeUserName { get; set; }

        [Display(Name = "觀看數")]
        public int Views { get; set; }

        /// <summary>
        /// Collection navigation to Novels
        /// </summary>
        public ICollection<Novel>? Novels { get; set; }

        /// <summary>
        /// Collection navigation to Articles
        /// </summary>
        public ICollection<Article>? Articles { get; set; }

        /// <summary>
        /// Collection navigation to Follows
        /// 主動追隨的人或作品
        /// </summary>
        public ICollection<Follow>? Follows { get; set; }

        /// <summary>
        /// Collection navigation to Followers
        /// 追隨者
        /// </summary>
        public ICollection<Follow>? Followers { get; set; }

        /// <summary>
        /// Collection navigation to Comments
        /// </summary>
        public ICollection<Comment>? Comments { get; set; }

        /// <summary>
        /// Collection navigation to Messages
        /// </summary>
        public ICollection<Message>? Messages { get; set; }

        /// <summary>
        /// Collection navigation to Involvings
        /// 參與、訂閱的人或作品
        /// </summary>
        public ICollection<Involving>? Involvings { get; set; }

        /// <summary>
        /// Collection navigation to Involvers
        /// 贊助者
        /// </summary>
        public ICollection<Involving>? Involvers { get; set; }

        /// <summary>
        /// Collection navigation to Agrees
        /// </summary>
        public ICollection<Agree>? Agrees { get; set; }

        /// <summary>
        /// Collection navigation to Notifications
        /// </summary>
        public ICollection<Notification>? Notifications { get; set; }

        /// <summary>
        /// Reference navigation to Missions
        /// </summary>
        public Missions? Missions { get; set; }

        /// <summary>
        /// Collection navigation to Achievements
        /// </summary>
        public ICollection<Achievement>? Achievements { get; set; }

        /// <summary>
        /// Many-to-many join table for Profile and Achievements
        /// </summary>
        public List<ProfileAchievement>? ProfileAchievements { get; set; }

        /// <summary>
        /// 序列號
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public int SeqNo { get; set; }

        /// <summary>
        /// Collection navigation to Viewed Articles
        /// </summary>
        public ICollection<Article>? ViewedArticles { get; set; }
        /// <summary>
        /// Many-to-many join table for Article and Viewer
        /// </summary>
        public List<ArticleViewer>? ArticleViewers { get; set; }

        /// <summary>
        /// Collection navigation to Viewed Novels
        /// </summary>
        public ICollection<Novel>? ViewedNovels { get; set; }
        /// <summary>
        /// Many-to-many join table for Novel and Viewers
        /// </summary>
        public List<NovelViewer>? NovelViewers { get; set; }

        /// <summary>
        /// 使用過的In幣
        /// </summary>
        [Column(TypeName = "money")]
        [Display(Name = "使用過的In幣")]
        public decimal UsedCoins { get; set; }
    }
}
