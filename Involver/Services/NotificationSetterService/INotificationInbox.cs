using Involver.Models.ViewModels;

namespace Involver.Services.NotificationSetterService;

/// <summary>
/// 讀取與標記使用者自己的通知
/// </summary>
public interface INotificationInbox
{
    /// <summary>
    /// 取得通知（由新到舊），並帶出觸發者的名稱與頭像
    /// </summary>
    /// <param name="userId">登入者 ID</param>
    /// <param name="skip">略過的筆數</param>
    /// <param name="take">取得的筆數</param>
    /// <param name="unreadOnly">只取未讀</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task<List<NotificationItemViewModel>> GetItemsAsync(string userId, int skip, int take, bool unreadOnly, CancellationToken cancellationToken = default);

    /// <summary>
    /// 計算未讀數，最多算到 <paramref name="cap"/>
    /// </summary>
    /// <param name="userId">登入者 ID</param>
    /// <param name="cap">上限</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task<int> CountUnreadAsync(string userId, int cap, CancellationToken cancellationToken = default);

    /// <summary>
    /// 將登入者的通知全部設為已讀
    /// </summary>
    /// <param name="userId">登入者 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 開啟一則通知：設為已讀，並回傳要前往的站內網址
    /// </summary>
    /// <param name="userId">登入者 ID（只能開自己的通知）</param>
    /// <param name="id">通知 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>站內相對網址；通知不存在、不屬於登入者或網址無效時為 null</returns>
    Task<string> OpenAsync(string userId, int id, CancellationToken cancellationToken = default);
}
