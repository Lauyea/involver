using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Involver.Models.NovelModel
{
    public class BiddingOption
    {
        public int BiddingOptionID { get; set; }
        public string OwnerID { get; set; }

        
        [Column(TypeName = "money")]
        public int BiddingCoins { get; set; }

        
        [Column(TypeName = "money")]
        public int TotalCoins { get; set; }

        [DataType(DataType.DateTime)]
        [Display(Name = "Create Time")]
        public DateTime CreateTime { get; set; }
        [StringLength(10)]
        [Display(Name = "內容")]
        public string Content { get; set; }

        public int VotingID { get; set; }
        public Voting Voting { get; set; }

        public ICollection<Vote> Votes { get; set; }
    }
}