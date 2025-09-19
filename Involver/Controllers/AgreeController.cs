using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;

using Involver.Authorization.Comment;
using Involver.Common;
using Involver.Extensions;
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
            Profile commenter = await _context.Profiles
                .Where(p => p.ProfileID == userId)
                .Include(p => p.Missions)
                .FirstOrDefaultAsync();
            if (commenter.Missions.BeAgreed != true)
            {
                commenter.Missions.BeAgreed = true;
                commenter.AwardCoins();
            }
            // 檢查是否完成所有任務，若完成會自動加獎勵幣
            commenter.Missions.CheckCompletion(commenter);

            await _context.SaveChangesAsync();

            var toasts = await Helpers.AchievementHelper.GetAgreeCountAsync(_context, commenter.ProfileID);

            if (OwenrID != commenter.ProfileID)
            {
                // Set notification
                var url = $"{Request.Scheme}://{Request.Host}/Comments/Details?id={message.CommentID}";

                await _notificationSetter.ForMessageBeAgreedAsync(message.Content, commenter.ProfileID, url, toasts);
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
            Profile commenter = await _context.Profiles
                .Where(p => p.ProfileID == userId)
                .Include(p => p.Missions)
                .FirstOrDefaultAsync();
            if (commenter.Missions.BeAgreed != true)
            {
                commenter.Missions.BeAgreed = true;
                commenter.AwardCoins();
            }
            // 檢查是否完成所有任務，若完成會自動加獎勵幣
            commenter.Missions.CheckCompletion(commenter);

            await _context.SaveChangesAsync();

            var toasts = await Helpers.AchievementHelper.GetAgreeCountAsync(_context, commenter.ProfileID);

            var from = string.Empty;
            var fromId = string.Empty;

            if (comment.ArticleID != null)
            {
                from = Parameters.Articles;
                fromId = comment.ArticleID.ToString();
            }
            else if (comment.NovelID != null)
            {
                from = Parameters.Novels;
                fromId = comment.NovelID.ToString();
            }
            else if (comment.EpisodeID != null)
            {
                from = Parameters.Episodes;
                fromId = comment.EpisodeID.ToString();
            }
            else if (comment.AnnouncementID != null)
            {
                from = Parameters.Announcements;
                fromId = comment.AnnouncementID.ToString();
            }
            else if (comment.FeedbackID != null)
            {
                from = Parameters.Feedbacks;
                fromId = comment.FeedbackID.ToString();
            }
            else
            {
                from = string.Empty;
                fromId = string.Empty;
            }

            if (OwenrID != commenter.ProfileID)
            {
                // Set notification
                var url = $"{Request.Scheme}://{Request.Host}/{from}/Details?id={fromId}";

                await _notificationSetter.ForCommentBeAgreedAsync(comment.Content, commenter.ProfileID, url, toasts);
            }
        }
    }
}