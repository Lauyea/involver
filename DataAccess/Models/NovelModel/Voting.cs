using DataAccess.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.NovelModel
{
    public class Voting
    {
        public int VotingID { get; set; }

        public string? OwnerID { get; set; }

        [Required(ErrorMessage = "必須要有投票標題")]
        [Display(Name = "投票標題")]
        [StringLength(20, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 2)]
        public required string Title { get; set; }

        [Display(Name = "投票政策")]
        public PolicyType Policy { get; set; }

        public ModeType Mode { get; set; }

        [Display(Name = "上限類型")]
        public LimitType Limit { get; set; }

        /// <summary>
        /// 每票所需In幣
        /// </summary>
        [Display(Name = "票價")]
        [Range(0, 3000, ErrorMessage = "大小只能介於0到3000之間")]
        public int Threshold { get; set; }

        /// <summary>
        /// 設定競標選項的最低金額
        /// </summary>
        [Range(0, 3000, ErrorMessage = "大小只能介於0到3000之間")]
        public int? BiddingLowerLimit { get; set; }

        /// <summary>
        /// 設定投票人數上限
        /// </summary>
        [Display(Name = "人數上限")]
        [Range(10, 1048576, ErrorMessage = "大小只能介於10到1048576之間")]
        public int? NumberLimit { get; set; }

        /// <summary>
        /// 設定總InCoins上限
        /// </summary>
        [Display(Name = "總InCoins上限")]
        [Range(30, 1073741824, ErrorMessage = "數值設定要在30到1073741824之間")]
        public int? CoinLimit { get; set; }

        /// <summary>
        /// 是否已經結束
        /// </summary>
        public bool End { get; set; }

        /// <summary>
        /// 投票總人數
        /// </summary>
        [Display(Name = "總人數")]
        public int TotalNumber { get; set; }

        [Display(Name = "總InCoins")]
        public int TotalCoins { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "創建時間")]
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// 投票最後期限
        /// </summary>
        [DataType(DataType.DateTime)]
        [Display(Name = "期限")]
        public DateTime? DeadLine { get; set; }

        /// <summary>
        /// FK to Episode
        /// </summary>
        public int EpisodeID { get; set; }
        /// <summary>
        /// Reference navigation to Episode
        /// </summary>
        public Episode? Episode { get; set; }

        /// <summary>
        /// Collection navigation to NormalOption
        /// </summary>
        public ICollection<NormalOption>? NormalOptions { get; set; }

        /// <summary>
        /// Collection navigation to BiddingOption
        /// </summary>
        public ICollection<BiddingOption>? BiddingOptions { get; set; }
    }
}