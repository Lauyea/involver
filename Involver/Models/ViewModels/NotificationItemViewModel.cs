using DataAccess.Common;

namespace Involver.Models.ViewModels;

/// <summary>
/// 通知列表的一筆通知（導覽列下拉選單與通知頁共用）
/// </summary>
public class NotificationItemViewModel
{
    /// <summary>
    /// 通知 ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 通知種類
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// 種類對應的 Font Awesome 圖示 class，例如 <c>fa-comment</c>
    /// </summary>
    public string IconClass { get; set; }

    /// <summary>
    /// 種類的 BEM modifier，決定圖示的顏色，例如 <c>comment</c>
    /// </summary>
    public string Tone { get; set; }

    /// <summary>
    /// 觸發者名稱；系統通知與舊資料為 null
    /// </summary>
    public string ActorName { get; set; }

    /// <summary>
    /// 觸發者頭像網址；沒有觸發者時為 null
    /// </summary>
    public string ActorImageUrl { get; set; }

    /// <summary>
    /// 通知文字（可能含舊資料的 HTML，顯示時要經過 AntiXss 消毒）
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 內容摘要（純文字）
    /// </summary>
    public string Excerpt { get; set; }

    /// <summary>
    /// 是否已讀
    /// </summary>
    public bool IsRead { get; set; }

    /// <summary>
    /// 建立時間
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// 相對時間，例如「3小時前」
    /// </summary>
    public string TimeAgo { get; set; }
}

/// <summary>
/// 通知列表
/// </summary>
/// <param name="Items">通知</param>
/// <param name="InMenu">是否顯示在導覽列的下拉選單（連結要加上 dropdown-item 以支援方向鍵）</param>
public record NotificationListViewModel(IReadOnlyList<NotificationItemViewModel> Items, bool InMenu);
