using DataAccess.Data;
using DataAccess.Models.NovelModel;

using Involver.Models.ViewModels;
using Involver.Services.NotificationSetterService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers
{
    [Route("api/v1/votings")]
    [ApiController]
    public class VotingsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<InvolverUser> _userManager;
        private readonly IAuthorizationService _authorizationService;
        private readonly INotificationSetter _notificationSetter;

        public VotingsApiController(
            ApplicationDbContext context,
            UserManager<InvolverUser> userManager,
            IAuthorizationService authorizationService,
            INotificationSetter notificationSetter)
        {
            _context = context;
            _userManager = userManager;
            _authorizationService = authorizationService;
            _notificationSetter = notificationSetter;
        }

        [HttpPost("cast")]
        [Authorize]
        public async Task<IActionResult> CastVote(CastVoteViewModel voteVM)
        { 
            var userId = _userManager.GetUserId(User);
            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.ProfileID == userId);
            var voting = await _context.Votings.Include(v => v.NormalOptions).ThenInclude(o => o.Votes).FirstOrDefaultAsync(v => v.VotingID == voteVM.VotingId);

            if (profile == null || voting == null)
            {
                return NotFound();
            }

            bool alreadyVoted = voting.NormalOptions.Any(o => o.Votes.Any(v => v.OwnerID == userId));
            if (alreadyVoted || voting.End)
            {
                return BadRequest("You have already voted or the voting has ended.");
            }

            var vote = new Vote
            {
                OwnerID = userId,
                NormalOptionID = voteVM.OptionId,
                Value = voting.Threshold // Simplified for now
            };

            _context.Votes.Add(vote);
            await _context.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Voting>> CreateVoting(CreateVotingViewModel votingVM)
        { 
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.ProfileID == _userManager.GetUserId(User));

            if (profile == null)
            {
                return Unauthorized();
            }

            var voting = new Voting
            {
                EpisodeID = votingVM.EpisodeId,
                OwnerID = profile.ProfileID,
                Title = votingVM.Title,
                Policy = votingVM.Policy,
                Limit = votingVM.Limit,
                Threshold = votingVM.Threshold,
                NumberLimit = votingVM.NumberLimit,
                CoinLimit = votingVM.CoinLimit,
                DeadLine = votingVM.DeadLine,
                CreateTime = DateTime.Now,
                End = false
            };

            foreach (var optionVM in votingVM.Options)
            {
                if (!string.IsNullOrWhiteSpace(optionVM.Content))
                {
                    var option = new NormalOption
                    {
                        Content = optionVM.Content,
                        Voting = voting
                    };
                    _context.NormalOptions.Add(option);
                }
            }

            _context.Votings.Add(voting);
            await _context.SaveChangesAsync();

            // Return the created voting with a 201 status code
            return CreatedAtAction(nameof(GetByEpisodeId), new { episodeId = voting.EpisodeID }, voting);
        }

        [HttpGet("ByEpisode/{episodeId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetByEpisodeId(int episodeId)
        {
            var userId = _userManager.GetUserId(User);

            var votings = await _context.Votings
                .Include(v => v.NormalOptions)
                    .ThenInclude(o => o.Votes)
                .Where(v => v.EpisodeID == episodeId)
                .OrderByDescending(v => v.VotingID)
                .ToListAsync();

            if (votings == null || !votings.Any())
            {
                return NotFound();
            }

            var result = votings.Select(v => new
            {
                v.VotingID,
                v.Title,
                v.Policy,
                v.Limit,
                v.Threshold,
                v.NumberLimit,
                v.CoinLimit,
                v.DeadLine,
                v.End,
                TotalCoins = v.NormalOptions.Sum(o => o.TotalCoins),
                TotalNumber = v.NormalOptions.Sum(o => o.Votes.Count()),
                Options = v.NormalOptions.Select(o => new
                {
                    o.NormalOptionID,
                    o.Content,
                    TotalCoins = o.TotalCoins,
                    VotesCount = o.Votes.Count()
                }),
                Voted = v.NormalOptions.Any(o => o.Votes.Any(vote => vote.OwnerID == userId))
            });

            return Ok(result);
        }
    }
}
