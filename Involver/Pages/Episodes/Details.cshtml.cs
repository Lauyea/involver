using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Involver.Data;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Models;
using System.Text.Json;
using Involver.Common;

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
        public PaginatedList<Comment> Comments { get; set; }
        public List<Voting> Votings { get; set; }
        public string CountDownArrayJSON { get; set; }
        public CountDown[] CountDownArray { get; set; }
        public bool ShowCommentByCreator { get; set; } = false;
        public string UserID { get; set; }
        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, int? pageIndex, bool? showCommentByCreator)
        {
            UserID = _userManager.GetUserId(User);
            ShowCommentByCreator = showCommentByCreator ?? false;
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

            await SetComments(id, pageIndex, ShowCommentByCreator);

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

            var i = 1;
            CountDownArray = new CountDown[Votings.Count - 1]; //有個空Voting
            foreach (Voting voting in Votings)
            {
                if (voting.NormalOptions.Count() == 0)
                {
                    continue;
                }

                if (voting.Limit == Voting.LimitType.Time)
                {
                    //限時已過，投票結束
                    TimeSpan? date = voting.DeadLine - DateTime.Now;
                    string dateString = date.ToString();
                    if (dateString.StartsWith("-") && voting.End == false)
                    {
                        voting.End = true;
                        _context.Attach(voting).State = EntityState.Modified;
                    }

                    var countDown = new CountDown()
                    {
                        countDownTime = voting.DeadLine,
                        id = "CountDown" + i
                    };
                    CountDownArray[i - 1] = countDown;
                    i++;
                }

                if (voting.Limit == Voting.LimitType.Number)
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
                        _context.Attach(voting).State = EntityState.Modified;
                    }
                }

                if (voting.Limit == Voting.LimitType.Value)
                {
                    //限定總值已過，投票結束
                    if (voting.TotalCoins > voting.CoinLimit && voting.End == false)
                    {
                        voting.End = true;
                        _context.Attach(voting).State = EntityState.Modified;
                    }
                }
            }
            if (CountDownArray != null)
            {
                CountDownArrayJSON = JsonSerializer.Serialize(CountDownArray);
            }

            //Add views
            Episode.Views++;
            _context.Attach(Episode).State = EntityState.Modified;
            CheckMissionWatchArticle();

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

            return Page();
        }

        private void CheckMissionWatchArticle()
        {
            //Check mission:WatchArticle
            string UserID = _userManager.GetUserId(User);
            if (UserID != null)
            {
                Profile userProfile = _context.Profiles
                .Where(p => p.ProfileID == UserID)
                .Include(p => p.Missions)
                .FirstOrDefault();
                if (userProfile.Missions.WatchArticle != true)
                {
                    userProfile.Missions.WatchArticle = true;
                    userProfile.VirtualCoins += 5;
                    _context.Attach(userProfile).State = EntityState.Modified;
                    StatusMessage = "每週任務：看一篇文章 已完成，獲得5 虛擬In幣。";
                }
                //Check other missions
                Missions missions = userProfile.Missions;
                if (missions.WatchArticle
                    && missions.Vote
                    && missions.LeaveComment
                    && missions.ViewAnnouncement
                    && missions.ShareCreation
                    && missions.BeAgreed)
                {
                    userProfile.Missions.CompleteOtherMissions = true;
                    _context.Attach(userProfile).State = EntityState.Modified;
                }
            }
        }

        private bool EpisodeExists(int id)
        {
            return _context.Episodes.Any(e => e.EpisodeID == id);
        }

        private async Task SetComments(int? id, int? pageIndex, bool ShowCommentByCreator)
        {
            IQueryable<Comment> comments = from c in _context.Comments
                                           select c;
            if (ShowCommentByCreator == true)
            {
                comments = comments
                .Include(c => c.Agrees)
                .Include(c => c.Messages)
                    .ThenInclude(c => c.Profile)
                .Include(c => c.Profile)
                .Include(c => c.Dices)
                .Include(c => c.Episode)
                    .ThenInclude(e => e.Novel)
                        .ThenInclude(n => n.Involvers)
                .Where(c => c.EpisodeID == id)
                .Where(c => c.ProfileID == Episode.OwnerID)
                .OrderBy(c => c.CommentID);

                
                Comments = await PaginatedList<Comment>.CreateAsync(
                    comments, pageIndex ?? 1, Parameters.CommetPageSize);
            }
            else
            {
                comments = comments
                .Include(c => c.Agrees)
                .Include(c => c.Messages)
                    .ThenInclude(c => c.Profile)
                .Include(c => c.Profile)
                .Include(c => c.Dices)
                .Include(c => c.Episode)
                    .ThenInclude(e => e.Novel)
                        .ThenInclude(n => n.Involvers)
                .Where(c => c.EpisodeID == id)
                .OrderBy(c => c.CommentID);

                
                Comments = await PaginatedList<Comment>.CreateAsync(
                    comments, pageIndex ?? 1, Parameters.CommetPageSize);
            }
        }

        public class CountDown
        {
            public DateTime? countDownTime { get; set; }
            public string id { get; set; }
        }

        public async Task<IActionResult> OnPostBlockAsync(int id, int fromID, int pageIndex)
        {
            string OwenrID = _userManager.GetUserId(User);

            if (OwenrID == null)
            {
                return Challenge();
            }

            Comment comment = await _context.Comments
                .Where(c => c.CommentID == id)
                .FirstOrDefaultAsync();
            if(comment.Block == false)
            {
                comment.Block = true;
            }
            else
            {
                comment.Block = false;
            }
            _context.Attach(comment).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return RedirectToPage("./Details", "OnGet", new { id = fromID, pageIndex }, "CommentHead");
        }

        public async Task<IActionResult> OnPostShareAsync(int id)
        {
            //Check mission:ShareCreation //CheckMissionShareCreation
            string UserID = _userManager.GetUserId(User);
            if(UserID != null)
            {
                Profile userProfile = await _context.Profiles
                .Where(p => p.ProfileID == UserID)
                .Include(p => p.Missions)
                .FirstOrDefaultAsync();
                if (userProfile.Missions.ShareCreation != true)
                {
                    userProfile.Missions.ShareCreation = true;
                    userProfile.VirtualCoins += 5;
                    _context.Attach(userProfile).State = EntityState.Modified;
                    StatusMessage = "每週任務：分享一次創作 已完成，獲得5 虛擬In幣。";
                }
                //Check other missions
                Missions missions = userProfile.Missions;
                if (missions.WatchArticle
                    && missions.Vote
                    && missions.LeaveComment
                    && missions.ViewAnnouncement
                    && missions.ShareCreation
                    && missions.BeAgreed)
                {
                    userProfile.Missions.CompleteOtherMissions = true;
                    _context.Attach(userProfile).State = EntityState.Modified;
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToPage("./Details", "OnGet", new { id }, "Share");
        }
    }
}
