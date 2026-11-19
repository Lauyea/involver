using Involver.Authorization.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Involver.Views.Shared.Components.AdminSidebar;

/// <summary>
/// 管理員後台的側欄。只列出目前使用者有權限的項目
/// </summary>
/// <param name="authorizationService">授權服務</param>
public class AdminSidebarViewComponent(IAuthorizationService authorizationService) : ViewComponent
{
    /// <summary>
    /// 側欄的一個項目
    /// </summary>
    /// <param name="Section">分組名稱</param>
    /// <param name="Text">顯示文字</param>
    /// <param name="Page">Admin Area 內的頁面路徑</param>
    /// <param name="Icon">Font Awesome 圖示</param>
    /// <param name="Policy">需要的權限 policy</param>
    public record SidebarItem(string Section, string Text, string Page, string Icon, string Policy);

    /// <summary>
    /// 側欄的一個項目與是否為目前頁面
    /// </summary>
    /// <param name="Item">項目</param>
    /// <param name="IsActive">是否為目前頁面</param>
    public record SidebarLink(SidebarItem Item, bool IsActive);

    /// <summary>
    /// 所有項目；順序就是側欄的順序
    /// </summary>
    public static readonly IReadOnlyList<SidebarItem> Items =
    [
        new(null, "總覽", "/Index", "fa-gauge", AdminPolicies.Access),
        new("用戶與人員", "用戶管理", "/Users/Index", "fa-users", AdminPolicies.Users),
        new("用戶與人員", "任命管理", "/People/Index", "fa-user-shield", AdminPolicies.Users),
        new("用戶與人員", "活動紀錄", "/People/AuditLog", "fa-clock-rotate-left", AdminPolicies.Administrators),
        new("金流", "金流總覽", "/Finance/Index", "fa-sack-dollar", AdminPolicies.Payments),
        new("金流", "訂單", "/Payments/Index", "fa-receipt", AdminPolicies.Payments),
        new("金流", "分潤清單", "/Payouts/Index", "fa-hand-holding-dollar", AdminPolicies.Payouts),
        new("內容", "內容統計", "/ContentStats/Index", "fa-chart-column", AdminPolicies.Content),
        new("內容", "標籤", "/Tags/Index", "fa-tags", AdminPolicies.Tags),
        new("內容", "分類", "/Tags/Categories", "fa-shapes", AdminPolicies.Tags),
        new("內容", "合併與別名", "/Tags/Aliases", "fa-code-merge", AdminPolicies.Tags),
        new("設定", "KPI 目標", "/Targets/Index", "fa-bullseye", AdminPolicies.Administrators),
        new("設定", "推薦設定", "/Recommendations/Index", "fa-wand-magic-sparkles", AdminPolicies.Administrators),
    ];

    /// <summary>
    /// 顯示側欄
    /// </summary>
    /// <returns>側欄</returns>
    public async Task<IViewComponentResult> InvokeAsync()
    {
        var currentPage = ViewContext.RouteData.Values["page"] as string ?? string.Empty;
        var links = new List<SidebarLink>();

        foreach (var item in Items)
        {
            var result = await authorizationService.AuthorizeAsync(UserClaimsPrincipal, item.Policy);
            if (result.Succeeded)
            {
                links.Add(new SidebarLink(item, IsActive(item.Page, currentPage)));
            }
        }

        return View(links);
    }

    /// <summary>
    /// 目前頁面是否屬於這個項目：同一頁，或同資料夾的 Index 項目底下的其他頁（例如用戶詳細資料）
    /// </summary>
    /// <param name="itemPage">項目的頁面路徑</param>
    /// <param name="currentPage">目前頁面路徑</param>
    /// <returns>是否為目前頁面</returns>
    public static bool IsActive(string itemPage, string currentPage)
    {
        if (string.Equals(itemPage, currentPage, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (!itemPage.EndsWith("/Index", StringComparison.Ordinal) || itemPage == "/Index")
        {
            return false;
        }

        var folder = itemPage[..^"Index".Length];
        var isSiblingItem = Items.Any(i => string.Equals(i.Page, currentPage, StringComparison.OrdinalIgnoreCase));
        return !isSiblingItem && currentPage.StartsWith(folder, StringComparison.OrdinalIgnoreCase);
    }
}
