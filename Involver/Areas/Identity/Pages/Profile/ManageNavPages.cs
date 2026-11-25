using Microsoft.AspNetCore.Mvc.Rendering;

namespace Involver.Areas.Identity.Pages.Profile;

/// <summary>
/// 個人檔案的分頁標籤與各分頁底下的子分頁（chip）
/// </summary>
public static class ManageNavPages
{
    /// <summary>
    /// 總覽
    /// </summary>
    public static string Index => "Index";

    /// <summary>
    /// 作品（創作、文章）
    /// </summary>
    public static string Creations => "Creations";

    /// <summary>
    /// 追蹤（作品、文章、創作者、追蹤者）
    /// </summary>
    public static string Follow => "Follow";

    /// <summary>
    /// 互動（評論、留言、贊同、通知）
    /// </summary>
    public static string Interaction => "Interaction";

    /// <summary>
    /// Involving（Involving、Involvers）；刻意用英文，和網站名稱 Involver 呼應
    /// </summary>
    public static string Involving => "Involving";

    /// <summary>
    /// 成就
    /// </summary>
    public static string Achievements => "Achievements";

    /// <summary>
    /// 我的書房（書架、閱讀紀錄、每週任務），只有本人看得到
    /// </summary>
    public static string Bookshelf => "Bookshelf";

    /// <summary>
    /// 分頁標籤
    /// </summary>
    /// <param name="Key">對應頁面設定的 ViewData["ActivePage"]</param>
    /// <param name="Page">頁面路徑</param>
    /// <param name="Label">顯示文字</param>
    /// <param name="Icon">Font Awesome 圖示</param>
    /// <param name="OwnerOnly">是否只有本人看得到</param>
    public record ProfileTab(string Key, string Page, string Label, string Icon, bool OwnerOnly = false);

    /// <summary>
    /// 分頁底下的子分頁
    /// </summary>
    /// <param name="Page">頁面路徑</param>
    /// <param name="Label">顯示文字</param>
    /// <param name="OwnerOnly">是否只有本人看得到</param>
    public record ProfileSubPage(string Page, string Label, bool OwnerOnly = false);

    /// <summary>
    /// 所有分頁標籤，依顯示順序
    /// </summary>
    public static IReadOnlyList<ProfileTab> Tabs { get; } =
    [
        new(Index, "/Profile/Index", "總覽", "fa-user"),
        new(Creations, "/Profile/Creations", "作品", "fa-pen-nib"),
        new(Follow, "/Profile/Follow", "追蹤", "fa-user-check"),
        new(Interaction, "/Profile/Interaction", "互動", "fa-comments"),
        new(Involving, "/Profile/Involving", "Involving", "fa-hand-holding-heart"),
        new(Achievements, "/Profile/Achievements", "成就", "fa-trophy"),
        new(Bookshelf, "/Profile/Bookshelf", "我的書房", "fa-book-open-reader", OwnerOnly: true),
    ];

    /// <summary>
    /// 某個分頁底下的子分頁；沒有子分頁時回傳空集合
    /// </summary>
    /// <param name="tab">分頁的 Key</param>
    /// <returns>子分頁</returns>
    public static IReadOnlyList<ProfileSubPage> SubPages(string tab) => tab switch
    {
        "Creations" =>
        [
            new("/Profile/Creations", "創作"),
            new("/Profile/Articles", "文章"),
        ],
        "Follow" =>
        [
            new("/Profile/Follow", "作品"),
            new("/Profile/FollowArticles", "文章"),
            new("/Profile/FollowCreators", "創作者"),
            new("/Profile/Followers", "追蹤者"),
        ],
        "Interaction" =>
        [
            new("/Profile/Interaction", "評論"),
            new("/Profile/Messages", "留言"),
            new("/Profile/Agrees", "贊同"),
            new("/Profile/Notifications", "通知", OwnerOnly: true),
        ],
        "Involving" =>
        [
            new("/Profile/Involving", "Involving"),
            new("/Profile/Involvers", "Involvers"),
        ],
        "Bookshelf" =>
        [
            new("/Profile/Bookshelf", "書架", OwnerOnly: true),
            new("/Profile/Vieweds", "閱讀紀錄", OwnerOnly: true),
            new("/Profile/ViewedArticles", "看過的文章", OwnerOnly: true),
            new("/Profile/Missions", "每週任務", OwnerOnly: true),
        ],
        _ => [],
    };

    /// <summary>
    /// 目前頁面所屬的分頁
    /// </summary>
    /// <param name="viewContext">ViewContext</param>
    /// <returns>分頁的 Key</returns>
    public static string GetActivePage(ViewContext viewContext) => viewContext.ViewData["ActivePage"] as string ?? Index;

    /// <summary>
    /// 目前個人檔案的 ProfileID（由各頁面以 ViewData["ProfileID"] 設定）
    /// </summary>
    /// <param name="viewContext">ViewContext</param>
    /// <returns>ProfileID</returns>
    public static string GetProfileID(ViewContext viewContext) => viewContext.ViewData["ProfileID"] as string;
}
