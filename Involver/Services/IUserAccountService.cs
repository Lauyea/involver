#nullable enable

using DataAccess.Data;

namespace Involver.Services;

/// <summary>
/// 同時維護 AspNetUsers 與 Profiles 的帳號操作。
/// AspNetUsers.UserName 是用戶名的唯一來源，Profiles.UserName 只是顯示用副本，
/// 兩邊都必須透過這個服務一起修改，才不會不同步。
/// </summary>
public interface IUserAccountService
{
    /// <summary>
    /// 變更用戶名（同時改 AspNetUsers 與 Profiles）
    /// </summary>
    /// <param name="user">要變更的使用者</param>
    /// <param name="newUserName">新的用戶名</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>失敗原因；成功時為 null</returns>
    Task<string?> ChangeUserNameAsync(InvolverUser user, string newUserName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 刪除帳號：匿名化並鎖定，保留作品、留言與 In 幣流水帳
    /// </summary>
    /// <param name="user">要刪除的使用者</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task AnonymizeAsync(InvolverUser user, CancellationToken cancellationToken = default);
}
