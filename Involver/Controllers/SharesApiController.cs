using DataAccess.Data;
using DataAccess.Models;

using Involver.Helpers;
using Involver.Services;

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
        private readonly IMissionService _missionService;

        public SharesApiController(
            ApplicationDbContext context,
            UserManager<InvolverUser> userManager,
            IMissionService missionService)
        {
            _context = context;
            _userManager = userManager;
            _missionService = missionService;
        }

        public class ShareRequest
        {
            public int ContentId { get; set; }
            public string ContentType { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> PostShareAsync([FromBody] ShareRequest shareRequest)
        {
            //Check mission:ShareCreation //CheckMissionShareCreation
            string userID = _userManager.GetUserId(User);

            if (userID != null)
            {
                // 任務旗標與獎勵由任務服務原子更新，完成所有任務時會自動加獎勵幣
                await _missionService.CompleteAsync(userID, MissionType.ShareCreation);
            }

            return Ok();
        }
    }
}
