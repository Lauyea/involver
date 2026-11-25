using DataAccess.Common;
using DataAccess.Data;

using Involver.Common;
using Involver.Models.ViewModels;
using Involver.Services;
using Involver.Services.NotificationSetterService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile;

/// <summary>
/// 個人檔案「互動」分頁底下的通知（只有本人看得到）
/// </summary>
[AllowAnonymous]
public class NotificationsModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService,
    INotificationInbox notificationInbox) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    /// <summary>
    /// 未讀數最多算到這個數字
    /// </summary>
    private const int UnreadCountCap = 100;

    /// <summary>
    /// 個人檔案
    /// </summary>
    public DataAccess.Models.Profile Profile { get; set; }

    /// <summary>
    /// 第一頁的通知
    /// </summary>
    public List<NotificationItemViewModel> Notifications { get; set; } = [];

    /// <summary>
    /// 是否只顯示未讀（網址的 ?filter=unread）
    /// </summary>
    public bool UnreadOnly { get; set; }

    /// <summary>
    /// 未讀數（最多 <see cref="UnreadCountCap"/>）
    /// </summary>
    public int UnreadCount { get; set; }

    /// <summary>
    /// 顯示通知第一頁
    /// </summary>
    /// <param name="id">Profile ID（只能看自己的）</param>
    /// <param name="filter">unread：只看未讀</param>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task<IActionResult> OnGetAsync(string id, string filter, CancellationToken cancellationToken)
    {
        Profile = await Context.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProfileID == id, cancellationToken);

        if (Profile == null)
        {
            return NotFound();
        }

        var userId = UserManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        if (id != userId)
        {
            // 只能看自己的通知
            return Forbid();
        }

        UnreadOnly = IsUnreadFilter(filter);
        Notifications = await notificationInbox.GetItemsAsync(userId, 0, Parameters.PageSize, UnreadOnly, cancellationToken);
        UnreadCount = await notificationInbox.CountUnreadAsync(userId, UnreadCountCap, cancellationToken);

        return Page();
    }

    /// <summary>
    /// 無限捲動：載入下一頁
    /// </summary>
    /// <param name="id">Profile ID（只能看自己的）</param>
    /// <param name="pageIndex">頁碼（從 1 開始）</param>
    /// <param name="filter">unread：只看未讀</param>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task<IActionResult> OnGetLoadMoreAsync(string id, int pageIndex, string filter, CancellationToken cancellationToken)
    {
        var userId = UserManager.GetUserId(User);

        if (userId == null || id != userId)
        {
            return Forbid();
        }

        var skip = Math.Max(0, pageIndex - 1) * Parameters.PageSize;
        var items = await notificationInbox.GetItemsAsync(userId, skip, Parameters.PageSize, IsUnreadFilter(filter), cancellationToken);

        return Partial("_NotificationItemsPartial", new NotificationListViewModel(items, InMenu: false));
    }

    /// <summary>
    /// 全部標為已讀
    /// </summary>
    /// <param name="id">Profile ID（只能標自己的）</param>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task<IActionResult> OnPostReadAllAsync(string id, CancellationToken cancellationToken)
    {
        var userId = UserManager.GetUserId(User);

        if (userId == null || id != userId)
        {
            return Forbid();
        }

        await notificationInbox.MarkAllReadAsync(userId, cancellationToken);

        return RedirectToPage(new { id });
    }

    /// <summary>
    /// 網址的 filter 是否為只看未讀
    /// </summary>
    private static bool IsUnreadFilter(string filter) => string.Equals(filter, "unread", StringComparison.OrdinalIgnoreCase);
}
