using DataAccess.Data;
using DataAccess.Models;

using Involver.Extensions;
using Involver.Helpers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Controllers
{
    [AllowAnonymous]
    [Route("api/shares")]
    [ApiController]
    public class SharesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<InvolverUser> _userManager;

        public SharesApiController(
            ApplicationDbContext context,
            UserManager<InvolverUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public class ShareRequest
        {
            public int ContentId { get; set; }
            public string ContentType { get; set; }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> PostShareAsync([FromBody] ShareRequest shareRequest)
        {
            //Check mission:ShareCreation //CheckMissionShareCreation
            string userID = _userManager.GetUserId(User);

            if (userID != null)
            {
                Profile userProfile = await _context.Profiles
                .Where(p => p.ProfileID == userID)
                .Include(p => p.Missions)
                .FirstOrDefaultAsync();
                if (userProfile != null && userProfile.Missions.ShareCreation != true)
                {
                    userProfile.Missions.ShareCreation = true;
                    userProfile.AwardCoins();
                }

                // 檢查是否完成所有任務，若完成會自動加獎勵幣
                userProfile.Missions.CheckCompletion(userProfile);

                await _context.SaveChangesAsync();
            }

            return Ok();
        }
    }
}
