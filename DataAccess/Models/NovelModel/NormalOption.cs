using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using DataAccess.Common;

namespace DataAccess.Models.NovelModel
{
    /// <summary>
    /// 普通投票選項
    /// </summary>
    public class NormalOption
    {
        public int NormalOptionID { get; set; }

        public string? OwnerID { get; set; }


        [Column(TypeName = "money")]
        public int TotalCoins { get; set; }

        [StringLength(Parameters.SmallContentLength, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 2)]
        [Display(Name = "選項內容")]
        public string? Content { get; set; }

        /// <summary>
        /// FK to Voting
        /// </summary>
        public int VotingID { get; set; }
        /// <summary>
        /// Reference navigation to Voting
        /// </summary>
        public Voting? Voting { get; set; }

        /// <summary>
        /// Collection navigation to Votes
        /// </summary>
        public ICollection<Vote>? Votes { get; set; }
    }
}