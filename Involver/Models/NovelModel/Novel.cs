using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Involver.Models.NovelModel
{
    public enum Type
    {
        Fantasy, History, Love, Real, Modern, Science, Horror, Detective
    }
    public class Novel
    {
        public int NovelID { get; set; }
        [Required(ErrorMessage = "必須要有標題")]
        [StringLength(50, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 2)]
        [Display(Name = "標題")]
        public string Title { get; set; }
        [Required(ErrorMessage = "必須要有介紹內容")]
        [StringLength(512, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 8)]
        [Display(Name = "介紹")]
        public string Introduction { get; set; }

        [DataType(DataType.ImageUrl)]
        [Display(Name = "主題圖片網址")]
        [StringLength(128)]
        public string ImageUrl { get; set; }

        [DisplayFormat(NullDisplayText = "No type")]
        [Display(Name = "類型")]
        public Type? Type { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "創建時間")]
        public DateTime CreateTime { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "更新時間")]
        public DateTime UpdateTime { get; set; }

        
        [Column(TypeName = "money")]
        public decimal TotalCoins { get; set; }

        
        [Column(TypeName = "money")]
        public decimal MonthlyCoins { get; set; }

        [Display(Name = "付費會員限定")]
        public bool PrimeRead { get; set; }

        [Display(Name = "完結")]
        public bool End { get; set; }
        [Display(Name = "觀看數")]
        public int Views { get; set; }
        [Display(Name = "封鎖")]
        public bool Block { get; set; }

        [Required]
        public string ProfileID { get; set; }
        [ForeignKey("ProfileID")]
        public Profile Profile { get; set; }

        public ICollection<Episode> Episodes { get; set; }

        public ICollection<Comment> Comments { get; set; }

        public ICollection<Involving> Involvers { get; set; }
        public ICollection<Follow> Follows { get; set; }
    }
}
