using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Models.FeedbackModel
{
    /// <summary>
    /// 回饋
    /// </summary>
    /// TODO: 之後可能會併入文章。
    public class Feedback
    {
        public int FeedbackID { get; set; }

        [Required(ErrorMessage = "必須要有標題")]
        [StringLength(50, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 2)]
        [Display(Name = "標題")]
        public required string Title { get; set; }

        public string? OwnerID { get; set; }

        [Display(Name = "作者")]
        public string? OwnerName { get; set; }

        [Required(ErrorMessage = "必須要有內容")]
        [StringLength(65536, ErrorMessage = "{0} 最多只能有 {1} 個字元")]
        [Display(Name = "內容")]
        public required string Content { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "時間")]
        public DateTime UpdateTime { get; set; }

        public bool Block { get; set; }

        /// <summary>
        /// 是否接受。會有In幣獎勵。
        /// </summary>
        public bool Accept { get; set; }

        /// <summary>
        /// Collection navigation to Comments
        /// </summary>
        public ICollection<Comment>? Comments { get; set; }
    }
}
