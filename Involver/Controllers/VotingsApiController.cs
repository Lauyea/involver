using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;

using Involver.Extensions;
using Involver.Helpers;
using Involver.Models.ViewModels;
using Involver.Services;
using Involver.Services.NotificationSetterService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers;

public class CastVoteApiViewModel
{
    public int VotingId { get; set; }
    public int OptionId { get; set; }
    public int Value { get; set; }
    public bool IsVirtual { get; set; }
}


[Route("api/v1/votings")]
[ApiController]
public class VotingsApiController(
    ApplicationDbContext context,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService,
    ICoinService coinService) : ControllerBase
{
    [HttpPost("cast")]
    public async Task<IActionResult> CastVote(CastVoteApiViewModel voteVM)
    {
        var userId = userManager.GetUserId(User);
        if (userId == null)
        {
            return Unauthorized("請登入後再投票。");
        }

        var voter = await context.Profiles
            .Include(p => p.Missions)
            .FirstOrDefaultAsync(p => p.ProfileID == userId);

        var voting = await context.Votings
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

        var option = await context.NormalOptions.FindAsync(voteVM.OptionId);
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

        // 票價為 0 或負數時，扣款會變成加值
        if (voteValue <= 0)
        {
            return BadRequest("票價必須大於 0。");
        }

        var episode = await context.Episodes.FindAsync(voting.EpisodeID);
        if (episode == null)
        {
            return NotFound("找不到章節資料。");
        }

        var novel = await context.Novels.FindAsync(episode.NovelID);
        if (novel == null)
        {
            return NotFound("找不到小說資料。");
        }

        await using var transaction = await context.Database.BeginTransactionAsync();

        // Deduct coins
        if (!await coinService.TrySpendAsync(userId, voteValue, voteVM.IsVirtual))
        {
            return BadRequest(voteVM.IsVirtual ? "虛擬In幣餘額不足。" : "實體In幣餘額不足。");
        }

        // 扣款會鎖住投票者的資料列直到交易結束，在這之後重新檢查才擋得住重複送出的投票
        bool votedConcurrently = await context.Votes
            .AnyAsync(v => v.OwnerID == userId && v.NormalOption.VotingID == voting.VotingID);
        if (votedConcurrently)
        {
            return BadRequest("您已經投過票了。");
        }

        // Create Vote
        var newVote = new Vote
        {
            OwnerID = userId,
            NormalOptionID = voteVM.OptionId,
            Value = voteValue,
            //IsVirtual = voteVM.IsVirtual
        };
        context.Votes.Add(newVote);

        // Mission check
        var originalVirtualCoins = voter.VirtualCoins;
        var missionMessage = string.Empty;
        if (voter.Missions.Vote != true)
        {
            voter.Missions.Vote = true;
            voter.AwardCoins();
            missionMessage = "每週任務：投一次票 已完成，獲得5 虛擬In幣。";
        }

        // 檢查是否完成所有任務，若完成會自動加獎勵幣
        voter.Missions.CheckCompletion(voter);

        // 任務獎勵改用原子更新：若讓 SaveChanges 寫回 voter 的餘額，會用讀取時的舊值蓋掉剛才的扣款
        var missionAward = voter.VirtualCoins - originalVirtualCoins;
        context.Entry(voter).Property(p => p.VirtualCoins).IsModified = false;
        if (missionAward > 0)
        {
            await coinService.AddVirtualCoinsAsync(userId, missionAward);
        }

        // Update totals
        await context.Votings
            .Where(v => v.VotingID == voting.VotingID)
            .ExecuteUpdateAsync(s => s
                .SetProperty(v => v.TotalCoins, v => v.TotalCoins + voteValue)
                .SetProperty(v => v.TotalNumber, v => v.TotalNumber + 1));
        await context.NormalOptions
            .Where(o => o.NormalOptionID == option.NormalOptionID)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.TotalCoins, o => o.TotalCoins + voteValue));

        await coinService.AddNovelCoinsAsync(novel.NovelID, voteValue);

        // Update creator's earnings if using real coins
        if (!voteVM.IsVirtual)
        {
            decimal shareRatio = (voting.Policy == PolicyType.Liberty) ? Parameters.LibertyShareRatio : Parameters.EqualityShareRatio;
            await coinService.AddCreatorIncomeAsync(novel.ProfileID, voteValue * shareRatio);
        }

        // Update Involving
        var involving = await context.Involvings
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
            context.Involvings.Add(newInvolving);
        }

        await context.SaveChangesAsync();
        await transaction.CommitAsync();

        // 成就服務會用同一個 DbContext 修改 voter，必須先載入最新餘額，避免以舊值覆寫
        await context.Entry(voter).ReloadAsync();

        // Achievement check
        var toasts = await achievementService.VoteCountAsync(userId);
        var usedCoinsToasts = await achievementService.UseCoinsCountAsync(userId, voter.UsedCoins);
        toasts.AddRange(usedCoinsToasts);

        return Ok(new { Toasts = toasts, MissionMessage = missionMessage });
    }

    [HttpPost]
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

        var profile = await context.Profiles.FirstOrDefaultAsync(p => p.ProfileID == userManager.GetUserId(User));

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
            context.NormalOptions.Add(option);
        }

        context.Votings.Add(voting);
        await context.SaveChangesAsync();

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
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<object>>> GetByEpisodeId(int episodeId)
    {
        var userId = userManager.GetUserId(User);

        var votings = await context.Votings
            .Include(v => v.NormalOptions)
                .ThenInclude(o => o.Votes)
            .Where(v => v.EpisodeID == episodeId)
            .OrderByDescending(v => v.VotingID)
            .ToListAsync();

        if (votings == null || votings.Count == 0)
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
