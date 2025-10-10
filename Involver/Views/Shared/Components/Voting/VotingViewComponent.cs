using DataAccess.Data;
using Involver.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Involver.Views.Shared.Components.Voting
{
    public class VotingViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<InvolverUser> _userManager;

        public VotingViewComponent(ApplicationDbContext context, UserManager<InvolverUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(int episodeId)
        {
            var votingViewModel = new VotingViewModel
            {
                EpisodeId = episodeId,
                IsProfessional = false // Default to false
            };

            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User as ClaimsPrincipal);
                var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.ProfileID == userId);
                if (profile != null && profile.Professional)
                {
                    votingViewModel.IsProfessional = true;
                }
            }

            return View(votingViewModel);
        }
    }

    public class VotingViewModel
    {
        public int EpisodeId { get; set; }
        public bool IsProfessional { get; set; }
    }
}
