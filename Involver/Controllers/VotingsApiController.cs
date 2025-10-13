using DataAccess.Common;
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
    [AllowAnonymous]
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
                return BadRequest("請登入後再投票。");
            }

            bool alreadyVoted = voting.NormalOptions.Any(o => o.Votes.Any(v => v.OwnerID == userId));
            if (alreadyVoted || voting.End)
            {
                return BadRequest("You have already voted or the voting has ended.");
            }

            var vote = new Vote
            {
                OwnerID = userId,
                NormalOptionID = voteVM.OptionId
            };

            // 如果是平等模式，票價固定
            if (voting.Policy == PolicyType.Equality)
            {
                vote.Value = voting.Threshold;
            }

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

            // Validation for LimitType
            // TODO: 要再改成回傳中文。Parameter要集中控制。
            switch ((LimitType)votingVM.Limit)
            {
                case LimitType.Time:
                    if (votingVM.DeadLine <= DateTime.Now)
                    {
                        return BadRequest("Deadline must be in the future.");
                    }
                    votingVM.NumberLimit = null;
                    votingVM.CoinLimit = null;
                    break;
                case LimitType.Number:
                    if (votingVM.NumberLimit == null || votingVM.NumberLimit < 10)
                    {
                        return BadRequest("Number limit must be 10 or greater.");
                    }
                    votingVM.DeadLine = DateTime.MaxValue; // No time limit
                    votingVM.CoinLimit = null;
                    break;
                case LimitType.Value:
                    if (votingVM.CoinLimit == null || votingVM.CoinLimit <= 0)
                    {
                        return BadRequest("Coin limit must be greater than 0.");
                    }
                    votingVM.DeadLine = DateTime.MaxValue; // No time limit
                    votingVM.NumberLimit = null;
                    break;
                default:
                    return BadRequest("Invalid limit type.");
            }

            var profile = await _context.Profiles.FirstOrDefaultAsync(p => p.ProfileID == _userManager.GetUserId(User));

            if (profile == null)
            {
                return Unauthorized();
            }

            // Server-side check to enforce policy for non-professional users
            if (!profile.Professional)
            {
                votingVM.Policy = PolicyType.Equality;
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

            var validOptions = votingVM.Options.Where(o => !string.IsNullOrWhiteSpace(o.Content)).ToList();

            if (validOptions.Count < 2)
            {
                return BadRequest("At least two options are required.");
            }

            foreach (var optionVM in validOptions)
            {
                if (optionVM.Content.Length < 2)
                {
                    return BadRequest("Option content must be at least 2 characters long.");
                }

                var option = new NormalOption
                {
                    Content = optionVM.Content,
                    Voting = voting
                };
                _context.NormalOptions.Add(option);
            }

            _context.Votings.Add(voting);
            await _context.SaveChangesAsync();

            var result = new
            {
                voting.VotingID,
                voting.Title,
                voting.Policy,
                voting.Limit,
                voting.Threshold,
                voting.NumberLimit,
                voting.CoinLimit,
                voting.DeadLine,
                voting.End,
                TotalCoins = 0,
                TotalNumber = 0,
                Options = voting.NormalOptions.Select(o => new
                {
                    o.NormalOptionID,
                    o.Content,
                    TotalCoins = 0,
                    VotesCount = 0
                }),
                Voted = false
            };

            // Return the created voting with a 201 status code
            return CreatedAtAction(nameof(GetByEpisodeId), new { episodeId = voting.EpisodeID }, result);
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
