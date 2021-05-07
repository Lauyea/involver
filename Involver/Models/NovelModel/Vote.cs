using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Involver.Models.NovelModel
{
    public class Vote
    {
        public int VoteID { get; set; }
        public string OwnerID { get; set; }

        //[DataType(DataType.Currency)]
        [Column(TypeName = "money")]
        public int Value { get; set; }

        public int? BiddingOptionID { get; set; }
        public BiddingOption BiddingOption { get; set; }
        public int? NormalOptionID { get; set; }
        public NormalOption NormalOption { get; set; }
    }
}