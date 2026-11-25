namespace Involver.Models.ViewModels.Navigation;

/// <summary>
/// 頁首的頭像選單（登入後）
/// </summary>
/// <param name="UserId">使用者 ID，用來連到自己的個人檔案</param>
/// <param name="UserName">使用者名稱</param>
/// <param name="AvatarUrl">頭像網址；沒有設定時為 Gravatar</param>
/// <param name="CanAccessAdmin">是否可以進入管理後台</param>
public record UserMenuViewModel(string UserId, string UserName, string AvatarUrl, bool CanAccessAdmin);
