using DataAccess.Common;
using DataAccess.Data;

using Involver.Areas.Identity.Pages.Profile;
using Involver.Authorization.Profile;
using Involver.Extensions;
using Involver.Models.ViewModels.Profiles;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Views.Shared.Components.ProfileHeader;

/// <summary>
/// 個人檔案頂部：橫幅、頭像、身分徽章、數據、追蹤與編輯按鈕，以及分頁標籤。個人檔案的每一頁共用
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="userManager">使用者管理</param>
/// <param name="authorizationService">授權（編輯、封鎖）</param>
public class ProfileHeaderViewComponent(
    ApplicationDbContext context,
    UserManager<InvolverUser> userManager,
    IAuthorizationService authorizationService) : ViewComponent
{
    /// <summary>
    /// 顯示個人檔案頂部
    /// </summary>
    /// <param name="profileId">個人檔案的 ProfileID</param>
    /// <param name="activeTab">目前所在的分頁 Key（<see cref="ManageNavPages"/>）</param>
    /// <returns>頂部區塊；找不到會員時不顯示</returns>
    public async Task<IViewComponentResult> InvokeAsync(string profileId, string activeTab)
    {
        var cancellationToken = HttpContext.RequestAborted;
        var profile = await context.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProfileID == profileId, cancellationToken);
        if (profile == null)
        {
            return Content(string.Empty);
        }

        var userId = userManager.GetUserId(UserClaimsPrincipal);
        var isOwner = userId == profileId;

        var novelCount = await context.Novels
            .CountAsync(n => n.ProfileID == profileId && !n.IsDeleted && !n.Block, cancellationToken);
        var articleCount = await context.Articles
            .CountAsync(a => a.ProfileID == profileId && !a.IsDeleted && !a.Block && a.Type == ArticleType.General, cancellationToken);
        var followerCount = await context.Follows
            .CountAsync(f => f.ProfileID == profileId, cancellationToken);
        var followingCount = await context.Follows
            .CountAsync(f => f.FollowerID == profileId && f.ProfileID != null, cancellationToken);
        var isFollowing = userId != null && !isOwner && await context.Follows
            .AnyAsync(f => f.ProfileID == profileId && f.FollowerID == userId, cancellationToken);

        var avatarUrl = profile.ImageUrl;
        if (string.IsNullOrEmpty(avatarUrl))
        {
            var user = await userManager.FindByIdAsync(profileId);
            avatarUrl = $"https://www.gravatar.com/avatar/{(user?.Email ?? string.Empty).ToMd5()}?d=retro";
        }

        var canEdit = (await authorizationService.AuthorizeAsync(UserClaimsPrincipal, profile, ProfileOperations.Update)).Succeeded;
        var canBan = (await authorizationService.AuthorizeAsync(UserClaimsPrincipal, profile, ProfileOperations.Ban)).Succeeded;

        return View(new ProfileHeaderViewModel
        {
            ProfileId = profile.ProfileID,
            UserName = profile.UserName,
            AvatarUrl = avatarUrl,
            BannerUrl = string.IsNullOrEmpty(profile.BannerImageUrl) ? null : profile.BannerImageUrl,
            Prime = profile.Prime,
            Professional = profile.Professional,
            Banned = profile.Banned,
            WorkCount = novelCount + articleCount,
            FollowerCount = followerCount,
            FollowingCount = followingCount,
            IsOwner = isOwner,
            IsSignedIn = userId != null,
            ReturnUrl = HttpContext.Request.PathBase + HttpContext.Request.Path + HttpContext.Request.QueryString,
            IsFollowing = isFollowing,
            CanEdit = canEdit,
            CanBan = canBan,
            ActiveTab = activeTab ?? ManageNavPages.Index,
            Tabs = ManageNavPages.Tabs.Where(t => !t.OwnerOnly || isOwner).ToList(),
        });
    }
}
