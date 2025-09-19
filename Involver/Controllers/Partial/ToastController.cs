using DataAccess.Data;

using Involver.Common;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Involver.Controllers.Partial
{
    [Route("[controller]/[action]")]
    [ApiController]
    public class ToastController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<InvolverUser> _userManager;

        public ToastController(ApplicationDbContext context, UserManager<InvolverUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Toast> Toasts { get; set; } = new();

        [HttpGet]
        public async Task<ActionResult> GetAgreeToasts()
        {
            var userId = _userManager.GetUserId(User);

            var toasts = await Helpers.AchievementHelper.AgreeCountAsync(_context, userId);

            Toasts.AddRange(toasts);

            return PartialView("~/Pages/_Toasts.cshtml", Toasts);
        }
    }
}