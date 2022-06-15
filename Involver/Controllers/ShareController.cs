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
    public class ShareController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<InvolverUser> _userManager;

        string statusMessage = "";

        public ShareController(ApplicationDbContext context, UserManager<InvolverUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<ActionResult> Get()
        {
            //Check mission:ShareCreation //CheckMissionShareCreation
            string UserID = _userManager.GetUserId(User);
            if (UserID != null)
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
                    statusMessage = "每週任務：分享一次創作 已完成，獲得5 虛擬In幣。";
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

            return Content(statusMessage);
        }
    }
}
