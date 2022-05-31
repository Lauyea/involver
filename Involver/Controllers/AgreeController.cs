using Involver.Data;
using Involver.Models;
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

        public AgreeController(ApplicationDbContext context, UserManager<InvolverUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> AgreeMessage(int messageId)
        {
            string OwenrID = _userManager.GetUserId(User);

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
                string UserID = message.ProfileID;
                Profile Commenter = await _context.Profiles
                    .Where(p => p.ProfileID == UserID)
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

        [HttpGet]
        public async Task<IActionResult> AgreeComment(int commentId)
        {
            string OwenrID = _userManager.GetUserId(User);

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
                string UserID = comment.ProfileID;
                Profile Commenter = await _context.Profiles
                    .Where(p => p.ProfileID == UserID)
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
    }
}
