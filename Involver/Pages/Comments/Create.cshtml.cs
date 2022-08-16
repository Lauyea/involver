using Involver.Authorization.Comment;
using Involver.Common;
using Involver.Data;
using Involver.Helpers;
using Involver.Models;
using Involver.Models.NovelModel;
using Involver.Services.NotificationSetterService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Involver.Pages.Comments
{
    public class CreateModel : DI_BasePageModel
    {
        private readonly INotificationSetter _notificationSetter;

        public CreateModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            UserManager<InvolverUser> userManager,
            INotificationSetter notificationSetter)
            : base(context, authorizationService, userManager)
        {
            _notificationSetter = notificationSetter;
        }

        public IActionResult OnGet(string from, int? fromID)
        {
            From = from;
            UserID = _userManager.GetUserId(User);
            if (from == Parameters.Episodes)
            {
                DetermindEpisodeOwner(fromID);
            }
            return Page();
        }

        [BindProperty]
        public Comment Comment { get; set; }
        [BindProperty]
        public Dice Dice { get; set; }
        [BindProperty]
        [Range(0,12)]
        public int RollTimes { get; set; }
        public string From { get; set; }
        public bool IsEpisodeOwner { get; set; } = false;
        public string UserID { get; set; }

        public string ErrorMessage { get; set; }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(string from, int? fromID)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Comment.Content.Length > Parameters.CommentLength)
            {
                Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user.Banned)
            {
                return Forbid();
            }

            From = from;
            UserID = _userManager.GetUserId(User);
            Comment.ProfileID = UserID;

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                        User, Comment,
                                                        CommentOperations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            if (from != Parameters.Feedbacks &&
                from != Parameters.Announcements &&
                from != Parameters.Episodes &&
                from != Parameters.Novels &&
                from != Parameters.Articles)
            {
                ErrorMessage = "沒有指定的評論頁面";
                return Page();
            }

            int commentCount = 0;

            Comment.UpdateTime = DateTime.Now;

            if (from == Parameters.Feedbacks)
            {
                Comment.FeedbackID = fromID;

                var feedback = await _context.Feedbacks
                    .Where(f => f.FeedbackID == fromID.Value)
                    .Include(f => f.Comments)
                    .FirstOrDefaultAsync().ConfigureAwait(false);

                commentCount = feedback.Comments.Count;
            }
            else if (from == Parameters.Announcements)
            {
                Comment.AnnouncementID = fromID;
            }
            else if (from == Parameters.Articles)
            {
                Comment.ArticleID = fromID;

                var article = await _context.Articles
                    .Where(a => a.ArticleID == fromID.Value)
                    .Include(a => a.Comments)
                    .FirstOrDefaultAsync().ConfigureAwait(false);

                commentCount = article.Comments.Count;
            }
            else if (from == Parameters.Novels)
            {
                Comment.NovelID = fromID;

                var novel = await _context.Novels
                    .Where(n => n.NovelID == fromID.Value)
                    .Include(n => n.Comments)
                    .FirstOrDefaultAsync().ConfigureAwait(false);

                commentCount = novel.Comments.Count;
            }
            else if (from == Parameters.Episodes)
            {
                Comment.EpisodeID = fromID;

                var episode = await _context.Episodes
                    .Where(e => e.EpisodeID == fromID.Value)
                    .Include(e => e.Comments)
                    .FirstOrDefaultAsync().ConfigureAwait(false);

                commentCount = episode.Comments.Count;
            }

            Comment.Dices = new List<Dice>();

            Random random = new();

            bool hasDices = Dice != null && from == Parameters.Episodes && Dice.Sides != 0;

            if (hasDices)
            {
                while (RollTimes > 0)
                {
                    Dice newDice = new()
                    {
                        Sides = Dice.Sides,
                        Value = random.Next(1, Dice.Sides + 1)
                    };
                    Comment.Dices.Add(newDice);
                    RollTimes--;
                }

                GetDiceTotal();
            }

            var strToDice = Comment.Content;

            var hasChange = DiceHelper.ReplaceRollDiceString(ref strToDice);

            if (hasChange)
            {
                Comment.Dices.Add(new Dice { CommentID = Comment.CommentID, Sides = 0, Value = 0 });
            }

            Comment.Content = strToDice;

            _context.Comments.Add(Comment);
            await CheckMissionLeaveComment();

            await _context.SaveChangesAsync();

            //Set notification
            if(from != Parameters.Announcements)
            {
                var areaTotalPages = (int)Math.Ceiling(commentCount / (double)Parameters.CommetPageSize);

                var url = $"{Request.Scheme}://{Request.Host}/{from}/Details?id={fromID}&pageIndex={areaTotalPages}#{Comment.CommentID}";

                await _notificationSetter.ForCommentAsync(from, fromID ?? 0, url, UserID, Comment.Content);
            }

            if (from != null)
            {
                var toasts = await Helpers.AchievementHelper.CommentCountAsync(_context, UserID);

                Toasts.AddRange(toasts);

                if (hasChange || hasDices)
                {
                    toasts = await Helpers.AchievementHelper.RollDicesAsync(_context, UserID);

                    Toasts.AddRange(toasts);
                }

                ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

                return RedirectToPage("/" + from + "/Details", "OnGet", new { id = fromID }, "CommentHead");
            }

            return Page();
        }

        private void GetDiceTotal()
        {
            int DiceTotal = 0;
            foreach (Dice dice in Comment.Dices)
            {
                DiceTotal += dice.Value;
            }
            int NumberOfDices = Comment.Dices.Count();
            int Sides = Comment.Dices.FirstOrDefault().Sides;
            string DiceTotalString = NumberOfDices + "D" + Sides + ": " + DiceTotal;
            Comment.Content = Comment.Content.Replace("DiceTotal", DiceTotalString);
        }

        private async Task CheckMissionLeaveComment()
        {
            //Check mission:LeaveComment
            Profile Commenter = await _context.Profiles
                .Where(p => p.ProfileID == UserID)
                .Include(p => p.Missions)
                .FirstOrDefaultAsync();
            if (Commenter.Missions.LeaveComment != true)
            {
                Commenter.Missions.LeaveComment = true;
                Commenter.VirtualCoins += 5;
                _context.Attach(Commenter).State = EntityState.Modified;
                //StatusMessage = "每週任務：留一則評論 已完成，獲得5 虛擬In幣。";
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
                _context.Attach(Commenter).State = EntityState.Modified;
            }
        }

        private async void DetermindEpisodeOwner(int? fromID)
        {
            Episode episode = await _context.Episodes
                                .Where(e => e.EpisodeID == fromID)
                                .FirstOrDefaultAsync();
            if (episode.OwnerID == UserID)
            {
                IsEpisodeOwner = true;
            }
        }
    }
}
