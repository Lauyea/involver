using Involver.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Views.Shared.Components.Voting
{
    public class VotingViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(int episodeId)
        {
            var votingViewModel = new VotingViewModel
            {
                EpisodeId = episodeId
            };

            return View(votingViewModel);
        }
    }

    public class VotingViewModel
    {
        public int EpisodeId { get; set; }
    }
}
