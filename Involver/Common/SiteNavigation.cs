using Involver.Models.ViewModels.Navigation;

namespace Involver.Common;

/// <summary>
/// 網站的導覽項目：頁首、手機底部導覽列、頁尾與工具箱頁都從這裡產生，新增頁面時只要改一個地方
/// </summary>
public static class SiteNavigation
{
    /// <summary>
    /// 主要入口，依顯示順序排列
    /// </summary>
    public static IReadOnlyList<SiteNavItem> PrimaryItems { get; } =
    [
        new("首頁", "/Index", "fa-solid fa-house", ["/", "/Index", "/Feed/FollowArticles", "/Feed/TrendingCreations"]),
        new("創作", "/Novels/Index", "fa-solid fa-book-open", ["/Novels", "/Episodes"]),
        new("討論", "/Articles/Index", "fa-solid fa-comments", ["/Articles"]),
        new("工具", "/Functions/Index", "fa-solid fa-toolbox", ["/Functions"]),
    ];

    /// <summary>
    /// 主要入口中的「工具」，頁首會改成顯示工具選單
    /// </summary>
    public const string ToolsPagePath = "/Functions/Index";

    /// <summary>
    /// 免費工具，不用登入就能使用
    /// </summary>
    public static IReadOnlyList<SiteToolLink> Tools { get; } =
    [
        new("製作小說封面", "為你的小說產生獨一無二的封面。", "/Functions/CreateCover", "fa-solid fa-image"),
        new("Markdown 轉換器", "把 Markdown 轉成 HTML，並即時預覽。", "/Functions/MarkdownToHtml", "fa-solid fa-code"),
        new("建立小說試閱", "設定閱讀密碼與期限，產生受保護的試閱連結。", "/Functions/Preview", "fa-solid fa-lock"),
        new("賽程表產生器", "建立單淘汰賽程，支援種子排位、比分與季軍戰。", "/Functions/BracketBuilder", "fa-solid fa-trophy"),
    ];
}
