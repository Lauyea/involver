using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Involver.Models.ArticleModel
{
    public class Article
    {
        public int ArticleID { get; set; }

        [Required(ErrorMessage = "必須要有標題")]
        [StringLength(50, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 2)]
        [Display(Name = "標題")]
        public string Title { get; set; }
        [Required(ErrorMessage = "必須要有內容")]
        [StringLength(65536, ErrorMessage = "{0} 最多只能有 {1} 個字元")]
        [Display(Name = "內容")]
        public string Content { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "時間")]
        public DateTime UpdateTime { get; set; }

        [Display(Name = "觀看數")]
        public int Views { get; set; }

        public bool Block { get; set; }

        
        [Column(TypeName = "money")]
        public decimal TotalCoins { get; set; }

        
        [Column(TypeName = "money")]
        public decimal MonthlyCoins { get; set; }

        [Required]
        public string ProfileID { get; set; }
        [ForeignKey("ProfileID")]
        public Profile Profile { get; set; }

        public ICollection<Comment> Comments { get; set; }

        public ICollection<Involving> Involvers { get; set; }

        public ICollection<ArticleTag> ArticleTags { get; set; }
    }
}
