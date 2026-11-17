using DataAccess.Data;

using Involver.Extensions;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Involver.Views.Shared.Components.UserStatus
{
    /// <summary>
    /// 依使用者是否為會員決定是否載入廣告
    /// </summary>
    public class UserStatusViewComponent : ViewComponent
    {
        private readonly UserManager<InvolverUser> _userManager;
        private readonly ApplicationDbContext _context;

        public UserStatusViewComponent(UserManager<InvolverUser> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        /// <summary>
        /// 回傳使用者是否為會員（Prime 以 Profiles 為唯一來源）
        /// </summary>
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var isPrime = await _context.IsPrimeAsync(_userManager.GetUserId(UserClaimsPrincipal));
            return View(isPrime);
        }
    }
}
