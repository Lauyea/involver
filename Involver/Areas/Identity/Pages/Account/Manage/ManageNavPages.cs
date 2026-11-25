using Microsoft.AspNetCore.Mvc.Rendering;

namespace Involver.Areas.Identity.Pages.Account.Manage;

/// <summary>
/// 帳號管理的導覽：分組的頁面清單與目前所在的頁面
/// </summary>
public static class ManageNavPages
{
    /// <summary>
    /// 帳號資料（用戶名、手機）
    /// </summary>
    public static string Index => "Index";

    /// <summary>
    /// 個人檔案（頭像、橫幅、自我介紹）
    /// </summary>
    public static string EditProfile => "EditProfile";

    /// <summary>
    /// Email
    /// </summary>
    public static string Email => "Email";

    /// <summary>
    /// 密碼（變更或設定）
    /// </summary>
    public static string ChangePassword => "ChangePassword";

    /// <summary>
    /// 外部登入
    /// </summary>
    public static string ExternalLogins => "ExternalLogins";

    /// <summary>
    /// 資料處理（下載、刪除）
    /// </summary>
    public static string PersonalData => "PersonalData";

    /// <summary>
    /// 兩步驟驗證（目前沒有放在導覽上）
    /// </summary>
    public static string TwoFactorAuthentication => "TwoFactorAuthentication";

    /// <summary>
    /// 錢包與會員
    /// </summary>
    public static string Wallet => "Wallet";

    /// <summary>
    /// 導覽上的一個頁面
    /// </summary>
    /// <param name="Key">對應頁面設定的 ViewData["ActivePage"]</param>
    /// <param name="Page">頁面路徑（相對於 Account/Manage）</param>
    /// <param name="Label">顯示文字</param>
    /// <param name="Icon">Font Awesome 圖示</param>
    public record NavItem(string Key, string Page, string Label, string Icon);

    /// <summary>
    /// 導覽的分組
    /// </summary>
    /// <param name="Title">分組標題</param>
    /// <param name="Items">頁面</param>
    public record NavGroup(string Title, IReadOnlyList<NavItem> Items);

    /// <summary>
    /// 導覽分組，依顯示順序。外部登入只在網站有設定外部登入時顯示
    /// </summary>
    /// <param name="hasExternalLogins">網站是否有設定外部登入</param>
    /// <returns>分組</returns>
    public static IReadOnlyList<NavGroup> Groups(bool hasExternalLogins) =>
    [
        new("個人資料",
        [
            new(EditProfile, "./EditProfile", "個人檔案", "fa-id-badge"),
            new(Index, "./Index", "帳號資料", "fa-user"),
        ]),
        new("登入與安全",
        [
            new(Email, "./Email", "Email", "fa-envelope"),
            new(ChangePassword, "./ChangePassword", "密碼", "fa-key"),
            .. hasExternalLogins ? [new NavItem(ExternalLogins, "./ExternalLogins", "外部登入", "fa-right-to-bracket")] : Array.Empty<NavItem>(),
        ]),
        new("錢包",
        [
            new(Wallet, "./Wallet", "錢包與會員", "fa-wallet"),
        ]),
        new("隱私",
        [
            new(PersonalData, "./PersonalData", "下載與刪除資料", "fa-shield-halved"),
        ]),
    ];

    /// <summary>
    /// 目前所在的頁面
    /// </summary>
    /// <param name="viewContext">ViewContext</param>
    /// <returns>頁面的 Key</returns>
    public static string GetActivePage(ViewContext viewContext) =>
        viewContext.ViewData["ActivePage"] as string
            ?? System.IO.Path.GetFileNameWithoutExtension(viewContext.ActionDescriptor.DisplayName);
}
