using DataAccess.Common;
using DataAccess.Data;

using Involver.Models.ViewModels.Navigation;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Involver.Views.Shared.Components.AnnouncementBanner;

/// <summary>
/// 頁首上方的最新公告橫幅：近期有新公告時顯示，使用者關閉後記在 cookie，同一則公告不再顯示
/// </summary>
public class AnnouncementBannerViewComponent(ApplicationDbContext context, IMemoryCache cache) : ViewComponent
{
    /// <summary>
    /// 記錄已關閉公告 ID 的 cookie 名稱（由 site.js 的 initAnnouncementBanner 寫入）
    /// </summary>
    public const string DismissedCookieName = "dismissed-announcement";

    /// <summary>
    /// 公告發布後顯示橫幅的天數
    /// </summary>
    public const int RecentDays = 14;

    private const string CacheKey = "AnnouncementBanner:Latest";

    /// <summary>
    /// 每個頁面都會查詢，快取幾分鐘就好；新公告最晚在快取到期後出現
    /// </summary>
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 輸出最新公告的橫幅；沒有近期公告或已關閉時不輸出任何內容
    /// </summary>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var latest = await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            var since = DateTime.Now.AddDays(-RecentDays);

            return await context.Articles
                .Where(a => a.Type == ArticleType.Announcement
                    && !a.IsDeleted
                    && !a.Block
                    && a.CreateTime >= since)
                .OrderByDescending(a => a.CreateTime)
                .Select(a => new AnnouncementBannerViewModel(a.ArticleID, a.Title))
                .AsNoTracking()
                .FirstOrDefaultAsync(HttpContext.RequestAborted);
        });

        if (latest == null
            || Request.Cookies[DismissedCookieName] == latest.ArticleId.ToString())
        {
            return Content(string.Empty);
        }

        return View(latest);
    }
}
