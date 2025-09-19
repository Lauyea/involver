using DataAccess.Data;
using DataAccess.Models;

using Involver.Authorization.Comment;
using Involver.Extensions;

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
                    userProfile.AwardCoins();
                    statusMessage = "每週任務：分享一次創作 已完成，獲得5 虛擬In幣。";
                }

                // 檢查是否完成所有任務，若完成會自動加獎勵幣
                userProfile.Missions.CheckCompletion(userProfile);

                await _context.SaveChangesAsync();
            }

            return Content(statusMessage);
        }
    }
}