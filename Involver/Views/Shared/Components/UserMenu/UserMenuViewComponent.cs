using DataAccess.Data;

using Involver.Authorization.Admin;
using Involver.Extensions;
using Involver.Models.ViewModels.Navigation;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Views.Shared.Components.UserMenu;

/// <summary>
/// 頁首的頭像選單：個人檔案、我的書房、創作者中心、帳號、主題切換、公告與回報、登出
/// </summary>
public class UserMenuViewComponent(
    ApplicationDbContext context,
    UserManager<InvolverUser> userManager,
    IAuthorizationService authorizationService) : ViewComponent
{
    /// <summary>
    /// 輸出登入者的頭像選單；未登入時不輸出任何內容
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = userManager.GetUserId(UserClaimsPrincipal);

        if (string.IsNullOrEmpty(userId))
        {
            return Content(string.Empty);
        }

        var cancellationToken = HttpContext.RequestAborted;
        var imageUrl = await context.Profiles
            .Where(p => p.ProfileID == userId)
            .Select(p => p.ImageUrl)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        // 和個人檔案、評論區一樣，沒有設定頭像時用 Gravatar
        if (string.IsNullOrEmpty(imageUrl))
        {
            var email = await context.Users
                .Where(u => u.Id == userId)
                .Select(u => u.Email)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);
            imageUrl = $"https://www.gravatar.com/avatar/{(email ?? string.Empty).ToMd5()}?d=retro";
        }

        var canAccessAdmin = (await authorizationService.AuthorizeAsync(UserClaimsPrincipal, AdminPolicies.Access)).Succeeded;

        return View(new UserMenuViewModel(userId, userManager.GetUserName(UserClaimsPrincipal), imageUrl, canAccessAdmin));
    }
}
