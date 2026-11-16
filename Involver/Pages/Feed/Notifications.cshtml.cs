using DataAccess.Data;
using DataAccess.Models;

using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Feed;

/// <summary>
/// 導覽列的通知下拉選單（每個頁面都會以 fetch 載入）
/// </summary>
/// <remarks>
/// 一律以登入者本人的 ID 查詢，不採用前端傳入的 userId，避免讀取或標記別人的通知。
/// 未登入時回傳空白區塊，因此 GET 仍允許匿名存取。
/// </remarks>
[AllowAnonymous]
//[ResponseCache(Duration = 60, VaryByQueryKeys = new string[] { "userId" })] // 暫停通知 Cache。通知即時化。
public class NotificationsModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    /// <summary>
    /// 下拉選單顯示的通知筆數
    /// </summary>
    private const int DropdownSize = 10;

    /// <summary>
    /// 未讀數最多算到這個數字，超過時畫面顯示「99+」
    /// </summary>
    private const int UnreadCountCap = 100;

    /// <summary>
    /// 最新的通知
    /// </summary>
    public List<Notification> Notifications { get; set; }

    /// <summary>
    /// 登入者 ID；未登入時為 null
    /// </summary>
    public string UserId { get; set; }

    /// <summary>
    /// 未讀通知數（最多 <see cref="UnreadCountCap"/>）
    /// </summary>
    public int Count { get; set; } = 0;

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

        Notifications = await Context.Notifications
            .Where(n => n.ProfileID == UserId)
            .OrderByDescending(n => n.CreatedDate)
            .Take(DropdownSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Count = await Context.Notifications
            .Where(n => n.ProfileID == UserId && !n.IsRead)
            .Take(UnreadCountCap)
            .CountAsync(cancellationToken);

        return Page();
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

        await Context.Notifications
            .Where(n => n.ProfileID == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);

        return new NoContentResult();
    }
}
