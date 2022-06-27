using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using Involver.Models.ArticleModel;
using Involver.Models.NovelModel;

namespace Involver.Models
{
    public class Profile
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string ProfileID { get; set; }

        [StringLength(256, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 10)]
        [Display(Name = "介紹")]
        public string Introduction { get; set; }

        [DataType(DataType.ImageUrl)]
        [Display(Name = "頭像圖片網址")]
        [StringLength(128)]
        public string ImageUrl { get; set; }

        [DataType(DataType.ImageUrl)]
        [Display(Name = "橫幅圖片網址")]
        [StringLength(128)]
        public string BannerImageUrl { get; set; }


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
        public string UserName { get; set; }

        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        [Display(Name = "註冊時間")]
        public DateTime EnrollmentDate { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "最後登入時間")]
        public DateTime LastTimeLogin { get; set; }

        [Display(Name = "特約作家")]
        public bool Professioal { get; set; }

        [Display(Name = "會員")]
        public bool Prime { get; set; }

        [Display(Name = "封鎖")]
        public bool Banned { get; set; }

        [Display(Name = "可改變用戶名")]
        public bool CanChangeUserName { get; set; }

        [Display(Name = "觀看數")]
        public int Views { get; set; }

        public ICollection<Novel> Novels { get; set; }
        public ICollection<Article> Articles { get; set; }

        public ICollection<Follow> Follows { get; set; }

        public ICollection<Follow> Followers { get; set; }

        public ICollection<Comment> Comments { get; set; }
        public ICollection<Message> Messages { get; set; }

        public ICollection<Involving> Involvings { get; set; }
        public ICollection<Involving> Involvers { get; set; }
        public ICollection<Agree> Agrees { get; set; }

        public Missions Missions { get; set; }
        public Achievements Achievements { get; set; }
    }
}
