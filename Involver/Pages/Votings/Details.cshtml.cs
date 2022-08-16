using Involver.Common;
using Involver.Data;
using Involver.Models;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Involver.Pages.Votings
{
    public class DetailsModel : DI_BasePageModel
    {

        public DetailsModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public Voting Voting { get; set; }

        [BindProperty, Required]
        public NormalOption NormalOption { get; set; }
        public NormalOption[] NormalOptions { get; set; }

        [BindProperty]
        public Vote Vote { get; set; }

        [BindProperty]
        [Display(Name = "使用虛擬In幣")]
        public bool VirtualVote { get; set; }

        public string ErrorMessage { get; set; }

        public bool Voted { get; set; } = false;

        public string UserId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Voting = await _context.Votings
                .Include(v => v.Episode)
                .Include(v => v.NormalOptions)
                    .ThenInclude(o => o.Votes)
                .FirstOrDefaultAsync(m => m.VotingID == id);

            NormalOptions = Voting.NormalOptions.ToArray();

            if (Voting.End)
            {
                ErrorMessage = "投票已經結束了";
                return Page();
            }

            UserId = _userManager.GetUserId(User);

            foreach(var option in Voting.NormalOptions)
            {
                Voted = option.Votes.Any(v =>v.OwnerID == UserId); 
                if(Voted == true)
                {
                    break;
                }
            }

            if (Voted)
            {
                ErrorMessage = "已經投過票了";
                return Page();
            }

            if (Voting == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Voting = await _context.Votings
                .Include(v => v.Episode)
                .Include(v => v.NormalOptions)
                    .ThenInclude(o => o.Votes)
                .FirstOrDefaultAsync(m => m.VotingID == id);

            NormalOptions = Voting.NormalOptions.ToArray();

            if (Voting.Policy == Voting.PolicyType.Equality)
            {
                Vote.Value = Voting.Threshold;
            }

            if (Vote.Value < Voting.Threshold)
            {
                ErrorMessage = "票價不足於投票設定的最小值";
                return Page();
            }
            UserId = _userManager.GetUserId(User);
            Vote ExistingVote = null;
            foreach (var option in NormalOptions)
            {
                ExistingVote = option
                    .Votes
                    .Where(v => v.OwnerID == UserId)
                    .FirstOrDefault();
                if(ExistingVote != null)
                {
                    break;
                }
            }

            NormalOption = await _context
                .NormalOptions
                .Where(n => n.NormalOptionID == NormalOption.NormalOptionID)
                .FirstOrDefaultAsync();

            if (ExistingVote == null)
            {
                Vote NewVote = new Vote();
                //Protect from overposting attacks
                if (await TryUpdateModelAsync<Vote>(
                    NewVote,
                    "Vote",   // Prefix for form value.
                    v => v.Value))
                {
                    NewVote.OwnerID = UserId;
                    NewVote.NormalOptionID = NormalOption.NormalOptionID;
                    _context.Votes.Add(NewVote);
                }
            }
            else
            {
                Voted = true;
                ErrorMessage = "已經投過票了";
                return Page();
            }

            if (Vote.Value > 0)
            {
                if (VirtualVote)
                {
                    await VirtualVoteOptionAsync(_context, NormalOption, Vote.Value);
                }
                else
                {
                    await VoteOptionAsync(_context, NormalOption, Vote.Value);
                }

                if(ErrorMessage == "餘額不足")
                {
                    return Page();
                }

                return RedirectToPage("/Episodes/Details", "OnGet", new { id = Voting.EpisodeID }, "Voting");
            }
            else
            {
                await _context.SaveChangesAsync();

                return RedirectToPage("/Episodes/Details", "OnGet", new { id = Voting.EpisodeID }, "Voting");
            }
        }

        async Task VoteOptionAsync(ApplicationDbContext Context, NormalOption option, int value)
        {
            Profile Voter = await Context
                .Profiles
                .Include(p => p.Missions)
                .Where(p => p.ProfileID == UserId)
                .FirstOrDefaultAsync();
            Profile Creator = await Context
                .Profiles
                .Where(p => p.ProfileID == option.OwnerID)
                .FirstOrDefaultAsync();
            if (Voter.RealCoins < value)
            {
                ErrorMessage = "餘額不足";
                return;
            }

            var Voting = await Context
                .Votings
                .Where(v => v.VotingID == option.VotingID)
                .FirstOrDefaultAsync();
            Voting.TotalCoins += value;
            Context.Attach(Voting).State = EntityState.Modified;

            //主要用來判斷月收入的來源
            if (Voting.Policy == Voting.PolicyType.Liberty)
            {
                Creator.MonthlyCoins += (decimal)(value * 0.6);//自由模式，作者得60%分潤
            }
            else
            {
                Creator.MonthlyCoins += (decimal)(value * 0.7);//平等模式，作者得70%分潤
            }
            Context.Attach(Creator).State = EntityState.Modified;
            //消耗實體貨幣
            Voter.RealCoins -= value;
            Voter.UsedCoins += value;
            CheckMissionVote(Voter);
            Context.Attach(Voter).State = EntityState.Modified;

            var episode = await Context
                .Episodes.Where(e => e.EpisodeID == Voting.EpisodeID)
                .FirstOrDefaultAsync();

            var novel = await Context
                .Novels
                .Where(n => n.NovelID == episode.NovelID)
                .FirstOrDefaultAsync();
            novel.MonthlyCoins += value;
            novel.TotalCoins += value;
            Context.Attach(novel).State = EntityState.Modified;

            var Involving = await Context.Involvings
                .Where(i => i.NovelID == novel.NovelID)
                .Where(i => i.InvolverID == Voter.ProfileID)
                .FirstOrDefaultAsync();
            if (Involving != null)
            {
                Involving.Value = value;
                Involving.MonthlyValue += value;
                Involving.TotalValue += value;
                Involving.LastTime = DateTime.Now;
                Context.Attach(Involving).State = EntityState.Modified;
            }
            else
            {
                Involving newInvolving = new Involving()
                {
                    Value = value,
                    MonthlyValue = value,
                    TotalValue = value,
                    LastTime = DateTime.Now,
                    InvolverID = UserId,
                    NovelID = novel.NovelID
                };
                Context.Involvings.Add(newInvolving);
            }
            option.TotalCoins += value;
            Context.Attach(option).State = EntityState.Modified;

            await Context.SaveChangesAsync();

            await SetAchievements(Voter);
        }

        private async Task SetAchievements(Profile Voter)
        {
            var toasts = await Helpers.AchievementHelper.VoteCountAsync(_context, Voter.ProfileID);

            Toasts.AddRange(toasts);

            toasts = await Helpers.AchievementHelper.UseCoinsCountAsync(_context, Voter.ProfileID, Voter.UsedCoins);

            Toasts.AddRange(toasts);

            ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);
        }

        async Task VirtualVoteOptionAsync(ApplicationDbContext Context, NormalOption option, int value)
        {
            Profile Voter = await Context
                .Profiles
                .Include(p => p.Missions)
                .Where(p => p.ProfileID == UserId)
                .FirstOrDefaultAsync();

            if (Voter.VirtualCoins < value)
            {
                ErrorMessage = "餘額不足";
                return;
            }

            //消耗虛擬貨幣
            Voter.VirtualCoins -= value;
            Voter.UsedCoins += value;
            CheckMissionVote(Voter);
            Context.Attach(Voter).State = EntityState.Modified;

            var Voting = await Context
                .Votings
                .Where(v => v.VotingID == option.VotingID)
                .FirstOrDefaultAsync();
            Voting.TotalCoins += value;
            Context.Attach(Voting).State = EntityState.Modified;

            var episode = await Context
                .Episodes.Where(e => e.EpisodeID == Voting.EpisodeID)
                .FirstOrDefaultAsync();

            var novel = await Context
                .Novels
                .Where(n => n.NovelID == episode.NovelID)
                .FirstOrDefaultAsync();
            novel.MonthlyCoins += value;
            novel.TotalCoins += value;
            Context.Attach(novel).State = EntityState.Modified;

            var Involving = await Context.Involvings
                .Where(i => i.NovelID == novel.NovelID)
                .Where(i => i.InvolverID == Voter.ProfileID)
                .FirstOrDefaultAsync();
            if (Involving != null)
            {
                Involving.Value = value;
                Involving.MonthlyValue += value;
                Involving.TotalValue += value;
                Involving.LastTime = DateTime.Now;
                Context.Attach(Involving).State = EntityState.Modified;
            }
            else
            {
                Involving newInvolving = new Involving()
                {
                    Value = value,
                    MonthlyValue = value,
                    TotalValue = value,
                    LastTime = DateTime.Now,
                    InvolverID = UserId,
                    NovelID = novel.NovelID
                };
                Context.Involvings.Add(newInvolving);
            }
            option.TotalCoins += value;
            Context.Attach(option).State = EntityState.Modified;
            await Context.SaveChangesAsync();

            await SetAchievements(Voter);
        }

        private void CheckMissionVote(Profile Voter)
        {
            //Check mission:Vote
            if (Voter.Missions.Vote != true)
            {
                Voter.Missions.Vote = true;
                Voter.VirtualCoins += 5;
                StatusMessage = "每週任務：投一次票 已完成，獲得5 虛擬In幣。";
            }
            //Check other missions
            Missions missions = Voter.Missions;
            if (missions.WatchArticle
                && missions.Vote
                && missions.LeaveComment
                && missions.ViewAnnouncement
                && missions.ShareCreation
                && missions.BeAgreed)
            {
                Voter.Missions.CompleteOtherMissions = true;
            }
        }
    }
}
