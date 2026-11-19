using Involver.Services.Admin;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Areas.Admin.Pages;

/// <summary>
/// 管理員後台頁面的共同基底：期間切換（7 / 30 / 90 天）與操作結果訊息
/// </summary>
public abstract class AdminPageModel : PageModel
{
    /// <summary>
    /// 期間天數（網址的 days 參數）；不在選項內時改成預設的 30 天
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int Days { get; set; } = DashboardPeriod.Default;

    /// <summary>
    /// 操作結果訊息；以「錯誤」開頭時顯示為錯誤
    /// </summary>
    [TempData]
    public string StatusMessage { get; set; }

    /// <summary>
    /// 綁定完成後統一期間天數，並提供給版面的期間切換
    /// </summary>
    /// <param name="context">頁面處理器執行內容</param>
    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        Days = DashboardPeriod.Normalize(Days);
        ViewData["Days"] = Days;
        base.OnPageHandlerExecuting(context);
    }
}
