using DataAccess.Common;

namespace Involver.Models.ViewModels
{
    public class CreateVotingViewModel
    {
        public int EpisodeId { get; set; }
        public string Title { get; set; }
        public PolicyType Policy { get; set; }
        public LimitType Limit { get; set; }
        public int Threshold { get; set; }
        public int? NumberLimit { get; set; }
        public int? CoinLimit { get; set; }
        public DateTime? DeadLine { get; set; }
        public List<CreateOptionViewModel> Options { get; set; }
    }

    public class CreateOptionViewModel
    {
        public string Content { get; set; }
    }

    public class CastVoteViewModel
    {
        public int VotingId { get; set; }
        public int OptionId { get; set; }
    }
}
