using DataAccess.Data;

using Involver.Common;
using Involver.Models.ViewModels;
using Involver.Services;
using Involver.Services.NotificationSetterService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Involver.Pages.Feed;

/// <summary>
/// 導覽列的通知下拉選單（每個頁面都會以 fetch 載入），以及開啟通知、全部標為已讀
/// </summary>
/// <remarks>
/// 一律以登入者本人的 ID 查詢，不採用前端傳入的 userId，避免讀取或標記別人的通知。
/// 未登入時回傳空白區塊，因此 GET 仍允許匿名存取。
/// </remarks>
[AllowAnonymous]
public class NotificationsModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService,
    INotificationInbox notificationInbox) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    /// <summary>
    /// 下拉選單顯示的通知筆數
    /// </summary>
    private const int DropdownSize = 8;

    /// <summary>
    /// 未讀數最多算到這個數字，超過時畫面顯示「99+」
    /// </summary>
    private const int UnreadCountCap = 100;

    /// <summary>
    /// 最新的通知
    /// </summary>
    public List<NotificationItemViewModel> Items { get; set; } = [];

    /// <summary>
    /// 登入者 ID；未登入時為 null
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// 未讀通知數（最多 <see cref="UnreadCountCap"/>）
    /// </summary>
    public int UnreadCount { get; set; }

    /// <summary>
    /// 取得登入者的最新通知與未讀數
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        UserId = UserManager.GetUserId(User);

        if (UserId == null)
        {
            return Page();
        }

        Items = await notificationInbox.GetItemsAsync(UserId, 0, DropdownSize, unreadOnly: false, cancellationToken);
        UnreadCount = await notificationInbox.CountUnreadAsync(UserId, UnreadCountCap, cancellationToken);

        return Page();
    }

    /// <summary>
    /// 開啟一則通知：標為已讀，再轉到通知的頁面
    /// </summary>
    /// <param name="id">通知 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task<IActionResult> OnGetOpenAsync(int id, CancellationToken cancellationToken)
    {
        var userId = UserManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        var url = await notificationInbox.OpenAsync(userId, id, cancellationToken);

        // 通知不存在、內容已刪除或網址無效時，回到通知頁
        if (url == null || !Url.IsLocalUrl(url))
        {
            return RedirectToPage("/Profile/Notifications", new { area = "Identity", id = userId });
        }

        return LocalRedirect(url);
    }

    /// <summary>
    /// 將登入者的通知全部設為已讀
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task<IActionResult> OnPostReadAsync(CancellationToken cancellationToken)
    {
        var userId = UserManager.GetUserId(User);

        if (userId == null)
        {
            return Unauthorized();
        }

        await notificationInbox.MarkAllReadAsync(userId, cancellationToken);

        return new NoContentResult();
    }
}
