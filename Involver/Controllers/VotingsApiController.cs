using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;
using Involver.Extensions;
using Involver.Helpers;
using Involver.Models.ViewModels;
using Involver.Services.NotificationSetterService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers
{
    public class CastVoteApiViewModel
    {
        public int VotingId { get; set; }
        public int OptionId { get; set; }
        public int Value { get; set; }
        public bool IsVirtual { get; set; }
    }

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
        public async Task<IActionResult> CastVote(CastVoteApiViewModel voteVM)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return Unauthorized("請登入後再投票。");
            }

            var voter = await _context.Profiles
                .Include(p => p.Missions)
                .FirstOrDefaultAsync(p => p.ProfileID == userId);

            var voting = await _context.Votings
                .Include(v => v.NormalOptions)
                    .ThenInclude(o => o.Votes)
                .FirstOrDefaultAsync(v => v.VotingID == voteVM.VotingId);

            if (voter == null || voting == null)
            {
                return NotFound("找不到投票或使用者資料。");
            }

            if (voting.End)
            {
                return BadRequest("投票已經結束了。");
            }

            bool alreadyVoted = voting.NormalOptions.Any(o => o.Votes.Any(v => v.OwnerID == userId));
            if (alreadyVoted)
            {
                return BadRequest("您已經投過票了。");
            }

            var option = await _context.NormalOptions.FindAsync(voteVM.OptionId);
            if (option == null || option.VotingID != voteVM.VotingId)
            {
                return NotFound("找不到投票選項。");
            }

            int voteValue = voteVM.Value;

            if (voting.Policy == PolicyType.Equality)
            {
                voteValue = voting.Threshold;
            }

            if (voteValue < voting.Threshold)
            {
                return BadRequest($"票價至少需要 {voting.Threshold} In幣。");
            }

            // Check balance
            if (voteVM.IsVirtual)
            {
                if (voter.VirtualCoins < voteValue)
                {
                    return BadRequest("虛擬In幣餘額不足。");
                }
            }
            else
            {
                if (voter.RealCoins < voteValue)
                {
                    return BadRequest("實體In幣餘額不足。");
                }
            }

            // Create Vote
            var newVote = new Vote
            {
                OwnerID = userId,
                NormalOptionID = voteVM.OptionId,
                Value = voteValue,
                //IsVirtual = voteVM.IsVirtual
            };
            _context.Votes.Add(newVote);

            // Deduct coins
            if (voteVM.IsVirtual)
            {
                voter.VirtualCoins -= voteValue;
            }
            else
            {
                voter.RealCoins -= voteValue;
            }
            voter.UsedCoins += voteValue;

            // Mission check
            var missionMessage = string.Empty;
            if (voter.Missions.Vote != true)
            {
                voter.Missions.Vote = true;
                voter.AwardCoins();
                missionMessage = "每週任務：投一次票 已完成，獲得5 虛擬In幣。";
            }

            // 檢查是否完成所有任務，若完成會自動加獎勵幣
            voter.Missions.CheckCompletion(voter);

            // Update totals
            voting.TotalCoins += voteValue;
            voting.TotalNumber++;
            option.TotalCoins += voteValue;

            var episode = await _context.Episodes.FindAsync(voting.EpisodeID);
            if (episode == null)
            {
                return NotFound("找不到章節資料。");
            }

            var novel = await _context.Novels.FindAsync(episode.NovelID);
            if (novel == null)
            {
                return NotFound("找不到小說資料。");
            }

            novel.MonthlyCoins += voteValue;
            novel.TotalCoins += voteValue;

            // Update creator's earnings if using real coins
            if (!voteVM.IsVirtual)
            {
                var creator = await _context.Profiles.FindAsync(novel.ProfileID);
                if (creator != null)
                {
                    decimal shareRatio = (voting.Policy == PolicyType.Liberty) ? Parameters.LibertyShareRatio : Parameters.EqualityShareRatio;
                    creator.MonthlyCoins += (decimal)voteValue * shareRatio;
                }
            }

            // Update Involving
            var involving = await _context.Involvings
                .FirstOrDefaultAsync(i => i.NovelID == novel.NovelID && i.InvolverID == userId);
            if (involving != null)
            {
                involving.Value = voteValue;
                involving.MonthlyValue += voteValue;
                involving.TotalValue += voteValue;
                involving.LastTime = DateTime.Now;
            }
            else
            {
                var newInvolving = new Involving
                {
                    Value = voteValue,
                    MonthlyValue = voteValue,
                    TotalValue = voteValue,
                    LastTime = DateTime.Now,
                    InvolverID = userId,
                    NovelID = novel.NovelID
                };
                _context.Involvings.Add(newInvolving);
            }

            await _context.SaveChangesAsync();

            // Achievement check
            var toasts = await AchievementHelper.VoteCountAsync(_context, userId);
            var usedCoinsToasts = await AchievementHelper.UseCoinsCountAsync(_context, userId, voter.UsedCoins);
            toasts.AddRange(usedCoinsToasts);

            return Ok(new { Toasts = toasts, MissionMessage = missionMessage });
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
            switch (votingVM.Limit)
            {
                case LimitType.Time:
                    if (votingVM.DeadLine <= DateTime.Now)
                    {
                        return BadRequest("截止日期必須在未來。");
                    }
                    votingVM.NumberLimit = null;
                    votingVM.CoinLimit = null;
                    break;
                case LimitType.Number:
                    if (votingVM.NumberLimit == null || votingVM.NumberLimit < Parameters.VotingNumberLimitMin)
                    {
                        return BadRequest($"數量限制必須為 {Parameters.VotingNumberLimitMin} 或更大。");
                    }
                    votingVM.DeadLine = null; // No time limit
                    votingVM.CoinLimit = null;
                    break;
                case LimitType.Value:
                    if (votingVM.CoinLimit == null || votingVM.CoinLimit <= Parameters.VotingCoinLimitMin)
                    {
                        return BadRequest($"In幣限額必須大於 {Parameters.VotingCoinLimitMin}。");
                    }
                    votingVM.DeadLine = null; // No time limit
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
                return BadRequest("至少需要兩個選項。");
            }

            foreach (var optionVM in validOptions)
            {
                if (optionVM.Content.Length < 2)
                {
                    return BadRequest("選項內容必須至少包含 2 個字元。");
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
