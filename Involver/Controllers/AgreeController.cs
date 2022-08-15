using Involver.Common;
using Involver.Data;
using Involver.Models;
using Involver.Services.NotificationSetterService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers
{
    [AllowAnonymous]
    [Route("[controller]/[action]")]
    [ApiController]
    public class AgreeController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<InvolverUser> _userManager;
        private readonly INotificationSetter _notificationSetter;

        public AgreeController(ApplicationDbContext context, 
            UserManager<InvolverUser> userManager,
            INotificationSetter notificationSetter)
        {
            _context = context;
            _userManager = userManager;
            _notificationSetter = notificationSetter;
        }

        public string OwenrID { get; set; }

        [HttpGet]
        public async Task<IActionResult> AgreeMessage(int messageId)
        {
            OwenrID = _userManager.GetUserId(User);

            if (OwenrID == null)
            {
                return Challenge();
            }

            Agree ExistingAgree = await _context.Agrees
                .Where(a => a.MessageID == messageId)
                .Where(a => a.ProfileID == OwenrID)
                .FirstOrDefaultAsync();

            Message message = null;

            if (ExistingAgree == null)
            {
                Agree agree = new Agree
                {
                    MessageID = messageId,
                    ProfileID = OwenrID,
                    UpdateTime = DateTime.Now
                };
                _context.Agrees.Add(agree);

                //Check mission:BeAgreed //CheckMissionBeAgreed
                message = await _context.Messages
                    .Where(c => c.MessageID == messageId)
                    .Include(c => c.Profile)
                    .Include(c => c.Agrees)
                    .FirstOrDefaultAsync();

                await CheckMissions(message);
            }
            else
            {
                message = await _context.Messages
                    .Where(c => c.MessageID == messageId)
                    .Include(c => c.Agrees)
                    .FirstOrDefaultAsync();
                _context.Agrees.Remove(ExistingAgree);
                await _context.SaveChangesAsync();
            }

            if (message != null)
            {
                return Content(message.Agrees.Count().ToString());
            }
            else
            {
                return BadRequest();
            }
        }

        private async Task CheckMissions(Message message)
        {
            string userId = message.ProfileID;
            Profile Commenter = await _context.Profiles
                .Where(p => p.ProfileID == userId)
                .Include(p => p.Missions)
                .FirstOrDefaultAsync();
            if (Commenter.Missions.BeAgreed != true)
            {
                Commenter.Missions.BeAgreed = true;
                Commenter.VirtualCoins += 5;
                _context.Attach(Commenter).State = EntityState.Modified;
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

            await _context.SaveChangesAsync();

            var toasts = await Helpers.AchievementHelper.GetAgreeCountAsync(_context, Commenter.ProfileID);

            if (OwenrID != Commenter.ProfileID)
            {
                // Set notification
                var url = $"{Request.Scheme}://{Request.Host}/Comments/Details?id={message.CommentID}";

                await _notificationSetter.ForMessageBeAgreedAsync(message.Content, Commenter.ProfileID, url, toasts);
            }
        }

        [HttpGet]
        public async Task<IActionResult> AgreeComment(int commentId)
        {
            OwenrID = _userManager.GetUserId(User);

            if (OwenrID == null)
            {
                return Challenge();
            }

            Agree ExistingAgree = await _context.Agrees
                .Where(a => a.CommentID == commentId)
                .Where(a => a.ProfileID == OwenrID)
                .FirstOrDefaultAsync();

            Comment comment = null;

            if (ExistingAgree == null)
            {
                Agree agree = new Agree
                {
                    CommentID = commentId,
                    ProfileID = OwenrID,
                    UpdateTime = DateTime.Now
                };
                _context.Agrees.Add(agree);

                //Check mission:BeAgreed //CheckMissionBeAgreed
                comment = await _context.Comments
                    .Where(c => c.CommentID == commentId)
                    .Include(c => c.Profile)
                    .Include(c => c.Agrees)
                    .FirstOrDefaultAsync();

                await CheckMissions(comment);
            }
            else
            {
                comment = await _context.Comments
                    .Where(c => c.CommentID == commentId)
                    .Include(c => c.Agrees)
                    .FirstOrDefaultAsync();
                _context.Agrees.Remove(ExistingAgree);
                await _context.SaveChangesAsync();
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

        private async Task CheckMissions(Comment comment)
        {
            string userId = comment.ProfileID;
            Profile Commenter = await _context.Profiles
                .Where(p => p.ProfileID == userId)
                .Include(p => p.Missions)
                .FirstOrDefaultAsync();
            if (Commenter.Missions.BeAgreed != true)
            {
                Commenter.Missions.BeAgreed = true;
                Commenter.VirtualCoins += 5;
                _context.Attach(Commenter).State = EntityState.Modified;
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

            await _context.SaveChangesAsync();

            var toasts = await Helpers.AchievementHelper.GetAgreeCountAsync(_context, Commenter.ProfileID);

            var from = string.Empty;
            var fromId = string.Empty;

            if (comment.ArticleID != null)
            {
                from = Parameters.Articles;
                fromId = comment.ArticleID.ToString();
            }
            else if(comment.NovelID != null)
            {
                from = Parameters.Novels;
                fromId = comment.NovelID.ToString();
            }
            else if(comment.EpisodeID != null)
            {
                from = Parameters.Episodes;
                fromId = comment.EpisodeID.ToString();
            }
            else if(comment.AnnouncementID != null)
            {
                from = Parameters.Announcements;
                fromId = comment.AnnouncementID.ToString();
            }
            else if(comment.FeedbackID != null)
            {
                from = Parameters.Feedbacks;
                fromId = comment.FeedbackID.ToString();
            }
            else
            {
                from = string.Empty;
                fromId = string.Empty;
            }

            if (OwenrID != Commenter.ProfileID)
            {
                // Set notification
                var url = $"{Request.Scheme}://{Request.Host}/{from}/Details?id={fromId}";

                await _notificationSetter.ForCommentBeAgreedAsync(comment.Content, Commenter.ProfileID, url, toasts);
            }
        }
    }
}
