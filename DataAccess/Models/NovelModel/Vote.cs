using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DataAccess.Models.NovelModel
{
    /// <summary>
    /// 投票
    /// </summary>
    public class Vote
    {
        public int VoteID { get; set; }

        /// <summary>
        /// 投票者ID
        /// </summary>
        public string? OwnerID { get; set; }

        /// <summary>
        /// 票值
        /// </summary>
        //[DataType(DataType.Currency)]
        [Column(TypeName = "money")]
        public int Value { get; set; }

        /// <summary>
        /// FK to BiddingOption
        /// </summary>
        public int? BiddingOptionID { get; set; }
        /// <summary>
        /// Reference navigation to BiddingOption
        /// </summary>
        public BiddingOption? BiddingOption { get; set; }

        /// <summary>
        /// FK to NormalOption
        /// </summary>
        public int? NormalOptionID { get; set; }
        /// <summary>
        /// Reference navigation to NormalOption
        /// </summary>
        public NormalOption? NormalOption { get; set; }
    }
}