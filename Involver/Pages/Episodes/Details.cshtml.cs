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
            UserID = UserManager.GetUserId(User);
            ShowCommentByCreator = showCommentByCreator ?? false;
            if (id == null)
            {
                return NotFound();
            }

            Episode = await Context.Episodes
                .Include(e => e.Novel).FirstOrDefaultAsync(m => m.EpisodeID == id);

            Novel = Episode.Novel;

            if (Episode == null)
            {
                return NotFound();
            }

            await SetComments(id, pageIndex, ShowCommentByCreator);

            PreviousEpisode = await Context.Episodes
                .Where(e => e.NovelID == Novel.NovelID)
                .Where(e => e.EpisodeID < id)
                .OrderByDescending(e => e.EpisodeID)
                .FirstOrDefaultAsync();

            NextEpisode = await Context.Episodes
                .Where(e => e.NovelID == Novel.NovelID)
                .Where(e => e.EpisodeID > id)
                .OrderBy(e => e.EpisodeID)
                .FirstOrDefaultAsync();

            Votings = await Context.Votings
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
                        Context.Attach(voting).State = EntityState.Modified;
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
                        Context.Attach(voting).State = EntityState.Modified;
                    }
                }

                if (voting.Limit == Voting.LimitType.Value)
                {
                    //限定總值已過，投票結束
                    if (voting.TotalCoins > voting.CoinLimit && voting.End == false)
                    {
                        voting.End = true;
                        Context.Attach(voting).State = EntityState.Modified;
                    }
                }
            }
            if (CountDownArray != null)
            {
                CountDownArrayJSON = JsonSerializer.Serialize(CountDownArray);
            }

            //Add views
            Episode.Views++;
            Context.Attach(Episode).State = EntityState.Modified;
            CheckMissionWatchArticle();

            try
            {
                await Context.SaveChangesAsync();
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
            string UserID = UserManager.GetUserId(User);
            if (UserID != null)
            {
                Profile userProfile = Context.Profiles
                .Where(p => p.ProfileID == UserID)
                .Include(p => p.Missions)
                .FirstOrDefault();
                if (userProfile.Missions.WatchArticle != true)
                {
                    userProfile.Missions.WatchArticle = true;
                    userProfile.VirtualCoins += 5;
                    Context.Attach(userProfile).State = EntityState.Modified;
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
                    Context.Attach(userProfile).State = EntityState.Modified;
                }
            }
        }

        private bool EpisodeExists(int id)
        {
            return Context.Episodes.Any(e => e.EpisodeID == id);
        }

        private async Task SetComments(int? id, int? pageIndex, bool ShowCommentByCreator)
        {
            IQueryable<Comment> comments = from c in Context.Comments
                                           select c;
            if (ShowCommentByCreator == true)
            {
                comments = comments
                .Include(c => c.Agrees)
                .Include(c => c.Messages)
                .Include(c => c.Profile)
                .Include(c => c.Dices)
                .Include(c => c.Episode)
                    .ThenInclude(e => e.Novel)
                        .ThenInclude(n => n.Involvers)
                .Where(c => c.EpisodeID == id)
                .Where(c => c.ProfileID == Episode.OwnerID)
                .OrderBy(c => c.CommentID);

                int pageSize = 100;
                Comments = await PaginatedList<Comment>.CreateAsync(
                    comments, pageIndex ?? 1, pageSize);
            }
            else
            {
                comments = comments
                .Include(c => c.Agrees)
                .Include(c => c.Messages)
                .Include(c => c.Profile)
                .Include(c => c.Dices)
                .Include(c => c.Episode)
                    .ThenInclude(e => e.Novel)
                        .ThenInclude(n => n.Involvers)
                .Where(c => c.EpisodeID == id)
                .OrderBy(c => c.CommentID);

                int pageSize = 10;
                Comments = await PaginatedList<Comment>.CreateAsync(
                    comments, pageIndex ?? 1, pageSize);
            }
        }

        public async Task<IActionResult> OnPostAgreeAsync(int id, int fromID, int pageIndex)
        {
            string OwenrID = UserManager.GetUserId(User);

            if(OwenrID == null)
            {
                return Challenge();
            }

            Agree ExistingAgree = await Context.Agrees
                .Where(a => a.CommentID == id)
                .Where(a => a.ProfileID == OwenrID)
                .FirstOrDefaultAsync();

            if (ExistingAgree == null)
            {
                Agree agree = new Agree
                {
                    CommentID = id,
                    ProfileID = OwenrID,
                    UpdateTime = DateTime.Now
                };
                Context.Agrees.Add(agree);

                //Check mission:BeAgreed //CheckMissionBeAgreed
                Comment comment = await Context.Comments
                    .Where(c => c.CommentID == id)
                    .Include(c => c.Profile)
                    .FirstOrDefaultAsync();
                string UserID = comment.ProfileID;
                Profile Commenter = await Context.Profiles
                    .Where(p => p.ProfileID == UserID)
                    .Include(p => p.Missions)
                    .FirstOrDefaultAsync();
                if (Commenter.Missions.BeAgreed != true)
                {
                    Commenter.Missions.BeAgreed = true;
                    Commenter.VirtualCoins += 5;
                    Context.Attach(Commenter).State = EntityState.Modified;
                }
                //Check other missions
                Missions missions = Commenter.Missions;
                if (missions.WatchArticle
                    && missions.Vote
                    && missions.LeaveComment
                    && missions.ViewAnnouncement
                    && missions.ShareCreation
                    && missions.BeAgreed)
                {
                    Commenter.Missions.CompleteOtherMissions = true;
                    Context.Attach(Commenter).State = EntityState.Modified;
                }

                await Context.SaveChangesAsync();
            }
            else
            {
                Context.Agrees.Remove(ExistingAgree);
                await Context.SaveChangesAsync();
            }

            return RedirectToPage("./Details", "OnGet", new { id = fromID, pageIndex }, id.ToString());
        }

        public async Task<IActionResult> OnPostAgreeCommentAsync(int id)
        {
            string OwenrID = UserManager.GetUserId(User);

            if (OwenrID == null)
            {
                return Challenge();
            }

            Agree ExistingAgree = await Context.Agrees
                .Where(a => a.CommentID == id)
                .Where(a => a.ProfileID == OwenrID)
                .FirstOrDefaultAsync();

            Comment comment = null;

            if (ExistingAgree == null)
            {
                Agree agree = new Agree
                {
                    CommentID = id,
                    ProfileID = OwenrID,
                    UpdateTime = DateTime.Now
                };
                Context.Agrees.Add(agree);

                //Check mission:BeAgreed //CheckMissionBeAgreed
                comment = await Context.Comments
                    .Where(c => c.CommentID == id)
                    .Include(c => c.Profile)
                    .Include(c => c.Agrees)
                    .FirstOrDefaultAsync();
                string UserID = comment.ProfileID;
                Profile Commenter = await Context.Profiles
                    .Where(p => p.ProfileID == UserID)
                    .Include(p => p.Missions)
                    .FirstOrDefaultAsync();
                if (Commenter.Missions.BeAgreed != true)
                {
                    Commenter.Missions.BeAgreed = true;
                    Commenter.VirtualCoins += 5;
                    Context.Attach(Commenter).State = EntityState.Modified;
                }
                //Check other missions
                Missions missions = Commenter.Missions;
                if (missions.WatchArticle
                    && missions.Vote
                    && missions.LeaveComment
                    && missions.ViewAnnouncement
                    && missions.ShareCreation
                    && missions.BeAgreed)
                {
                    Commenter.Missions.CompleteOtherMissions = true;
                    Context.Attach(Commenter).State = EntityState.Modified;
                }

                await Context.SaveChangesAsync();
            }
            else
            {
                comment = await Context.Comments
                    .Where(c => c.CommentID == id)
                    .Include(c => c.Agrees)
                    .FirstOrDefaultAsync();
                Context.Agrees.Remove(ExistingAgree);
                await Context.SaveChangesAsync();
            }

            if (comment != null)
            {
                return Content(comment.Agrees.Count().ToString());
            }
            else
            {
                return BadRequest();
            }
        }

        public class CountDown
        {
            public DateTime? countDownTime { get; set; }
            public string id { get; set; }
        }

        public async Task<IActionResult> OnPostBlockAsync(int id, int fromID, int pageIndex)
        {
            string OwenrID = UserManager.GetUserId(User);

            if (OwenrID == null)
            {
                return Challenge();
            }

            Comment comment = await Context.Comments
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
            Context.Attach(comment).State = EntityState.Modified;
            await Context.SaveChangesAsync();

            return RedirectToPage("./Details", "OnGet", new { id = fromID, pageIndex }, "CommentHead");
        }

        public async Task<IActionResult> OnPostShareAsync(int id)
        {
            //Check mission:ShareCreation //CheckMissionShareCreation
            string UserID = UserManager.GetUserId(User);
            if(UserID != null)
            {
                Profile userProfile = await Context.Profiles
                .Where(p => p.ProfileID == UserID)
                .Include(p => p.Missions)
                .FirstOrDefaultAsync();
                if (userProfile.Missions.ShareCreation != true)
                {
                    userProfile.Missions.ShareCreation = true;
                    userProfile.VirtualCoins += 5;
                    Context.Attach(userProfile).State = EntityState.Modified;
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
                    Context.Attach(userProfile).State = EntityState.Modified;
                }
                await Context.SaveChangesAsync();
            }
            return RedirectToPage("./Details", "OnGet", new { id }, "Share");
        }
    }
}
