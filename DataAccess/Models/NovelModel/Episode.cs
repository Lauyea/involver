using DataAccess.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.NovelModel
{
    public class Episode
    {
        public int EpisodeID { get; set; }

        [Required(ErrorMessage = "必須要有標題")]
        [StringLength(Parameters.SmallContentLength, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 2)]
        [Display(Name = "標題")]
        public required string Title { get; set; }

        public string? OwnerID { get; set; }

        [Required(ErrorMessage = "必須要有內容")]
        [StringLength(Parameters.ArticleLength, ErrorMessage = "{0} 最多只能有 {1} 個字元")]
        [Display(Name = "內容")]
        public required string Content { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "時間")]
        public DateTime UpdateTime { get; set; }

        [Display(Name = "觀看數")]
        public int Views { get; set; }

        /// <summary>
        /// 是否有舉行投票
        /// </summary>
        public bool HasVoting { get; set; }

        /// <summary>
        /// 是否為最新章節
        /// </summary>
        public bool IsLast { get; set; }

        /// <summary>
        /// 軟刪除Tag
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// FK to Novel
        /// </summary>
        public int NovelID { get; set; }
        /// <summary>
        /// Reference navigation to Novel
        /// </summary>
        public Novel? Novel { get; set; }

        /// <summary>
        /// Collection navigation to Comments
        /// </summary>
        public ICollection<Comment>? Comments { get; set; }
        /// <summary>
        /// Collection navigation to Votings
        /// </summary>
        public ICollection<Voting>? Votings { get; set; }
    }
}