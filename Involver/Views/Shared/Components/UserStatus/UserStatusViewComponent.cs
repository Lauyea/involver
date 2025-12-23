using DataAccess.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace Involver.Views.Shared.Components.UserStatus
{
    public class UserStatusViewComponent : ViewComponent
    {
        private readonly UserManager<InvolverUser> _userManager;

        public UserStatusViewComponent(UserManager<InvolverUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var user = await _userManager.GetUserAsync(UserClaimsPrincipal);
            return View(user);
        }
    }
}
