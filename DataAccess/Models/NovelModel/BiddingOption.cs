using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using DataAccess.Common;

namespace DataAccess.Models.NovelModel
{
    /// <summary>
    /// 競標選項。暫時還沒有用到
    /// </summary>
    public class BiddingOption
    {
        public int BiddingOptionID { get; set; }
        public string? OwnerID { get; set; }

        /// <summary>
        /// 競標金額
        /// </summary>
        [Column(TypeName = "money")]
        public int BiddingCoins { get; set; }

        /// <summary>
        /// 總額
        /// </summary>
        [Column(TypeName = "money")]
        public int TotalCoins { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Create Time")]
        public DateTime CreateTime { get; set; }
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