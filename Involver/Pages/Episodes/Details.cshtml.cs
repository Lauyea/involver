using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;

using Involver.Authorization.Comment;
using Involver.Authorization.Voting;
using Involver.Common;
using Involver.Extensions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Episodes
{
    [AllowAnonymous]
    public class DetailsModel : DI_BasePageModel
    {
        public DetailsModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public Episode Episode { get; set; }

        public Episode PreviousEpisode { get; set; }

        public Episode NextEpisode { get; set; }

        public Novel Novel { get; set; }

        public List<Voting> Votings { get; set; }

        public string UserID { get; set; }

        public bool CanCreateVoting { get; set; } = false;

        public async Task<IActionResult> OnGetAsync(int? id, int? pageIndex)
        {
            UserID = _userManager.GetUserId(User);

            if (id == null)
            {
                return NotFound();
            }

            Episode = await _context.Episodes
                .Include(e => e.Novel).FirstOrDefaultAsync(m => m.EpisodeID == id);

            Novel = Episode.Novel;

            if (Episode == null)
            {
                return NotFound();
            }

            PreviousEpisode = await _context.Episodes
                .Where(e => e.NovelID == Novel.NovelID)
                .Where(e => e.EpisodeID < id)
                .OrderByDescending(e => e.EpisodeID)
                .FirstOrDefaultAsync();

            NextEpisode = await _context.Episodes
                .Where(e => e.NovelID == Novel.NovelID)
                .Where(e => e.EpisodeID > id)
                .OrderBy(e => e.EpisodeID)
                .FirstOrDefaultAsync();

            Votings = await _context.Votings
                .Where(v => v.EpisodeID == id)
                .Include(v => v.NormalOptions)
                    .ThenInclude(n => n.Votes)
                .ToListAsync();

            foreach (Voting voting in Votings)
            {
                if (voting.NormalOptions.Count() == 0)
                {
                    continue;
                }

                if (voting.Limit == LimitType.Time)
                {
                    //限時已過，投票結束
                    TimeSpan? date = voting.DeadLine - DateTime.Now;
                    string dateString = date.ToString();
                    if (dateString.StartsWith("-") && voting.End == false)
                    {
                        voting.End = true;
                    }
                }

                if (voting.Limit == LimitType.Number)
                {
                    //限定票數已過，投票結束
                    int TotalVotesCount = 0;
                    foreach (var option in voting.NormalOptions)
                    {
                        TotalVotesCount += option.Votes.Count();
                    }
                    if (TotalVotesCount > voting.NumberLimit && voting.End == false)
                    {
                        voting.End = true;
                    }
                }

                if (voting.Limit == LimitType.Value)
                {
                    //限定總值已過，投票結束
                    if (voting.TotalCoins > voting.CoinLimit && voting.End == false)
                    {
                        voting.End = true;
                    }
                }
            }

            //Add views
            Episode.Views++;

            await CheckMissionWatchArticle();

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EpisodeExists(Episode.EpisodeID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            if (!string.IsNullOrEmpty(ToastsJson))
            {
                Toasts = System.Text.Json.JsonSerializer.Deserialize<List<Toast>>(ToastsJson);
            }

            var toasts = await Helpers.AchievementHelper.ReadEpisodeAsync(_context, UserID);

            Toasts.AddRange(toasts);

            if (Episode.OwnerID == UserID)
            {
                CanCreateVoting = true;
            }

            return Page();
        }

        private async Task CheckMissionWatchArticle()
        {
            //Check mission:WatchArticle
            string UserID = _userManager.GetUserId(User);
            if (UserID != null)
            {
                Profile userProfile = await _context.Profiles
                .Where(p => p.ProfileID == UserID)
                .Include(p => p.Missions)
                .FirstOrDefaultAsync();
                if (userProfile.Missions.WatchArticle != true)
                {
                    userProfile.Missions.WatchArticle = true;
                    userProfile.AwardCoins();
                    StatusMessage = "每週任務：看一篇文章 已完成，獲得5 虛擬In幣。";
                }

                // 檢查是否完成所有任務，若完成會自動加獎勵幣
                userProfile.Missions.CheckCompletion(userProfile);
            }
        }

        private bool EpisodeExists(int id)
        {
            return _context.Episodes.Any(e => e.EpisodeID == id);
        }
    }
}