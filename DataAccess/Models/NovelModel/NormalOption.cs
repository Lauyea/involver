using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models.NovelModel
{
    public class NormalOption
    {
        public int NormalOptionID { get; set; }
        public string? OwnerID { get; set; }

        
        [Column(TypeName = "money")]
        public int TotalCoins { get; set; }
        [StringLength(20, ErrorMessage = "{0} 至少要有 {2} 到 {1} 個字元長度", MinimumLength = 2)]
        [Display(Name = "選項內容")]
        public string? Content { get; set; }

        public int VotingID { get; set; }
        public Voting? Voting { get; set; }

        public ICollection<Vote>? Votes { get; set; }
    }
}