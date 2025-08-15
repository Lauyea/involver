using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DataAccess.Models.NovelModel
{
    public class Voting
    {
        public enum PolicyType
        {
            Equality, Liberty
        }

        public enum ModeType
        {
            Normal, Bidding
        }

        public enum LimitType
        {
            Time, Number, Value
        }

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

        [Display(Name = "票價")]
        [Range(0, 3000, ErrorMessage = "大小只能介於0到3000之間")]
        public int Threshold { get; set; }

        [Range(0, 3000, ErrorMessage = "大小只能介於0到3000之間")]
        public int? BiddingLowerLimit { get; set; }

        [Display(Name = "人數上限")]
        [Range(10, 1048576, ErrorMessage = "大小只能介於10到1048576之間")]
        public int? NumberLimit { get; set; }

        [Display(Name = "總InCoins上限")]
        [Range(30, 1073741824, ErrorMessage = "數值設定要在30到1073741824之間")]
        public int? CoinLimit { get; set; }

        public bool End { get; set; }

        [Display(Name = "總人數")]
        public int TotalNumber { get; set; }

        [Display(Name = "總InCoins")]
        public int TotalCoins { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "創建時間")]
        public DateTime CreateTime { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "期限")]
        public DateTime? DeadLine { get; set; }

        public int EpisodeID { get; set; }
        public Episode? Episode { get; set; }

        public ICollection<NormalOption>? NormalOptions { get; set; }

        public ICollection<BiddingOption>? BiddingOptions { get; set; }
    }
}