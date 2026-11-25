using static Involver.Areas.Identity.Pages.Profile.ManageNavPages;

namespace Involver.Models.ViewModels.Profiles;

/// <summary>
/// 個人檔案頂部：橫幅、頭像、身分、數據、動作按鈕與分頁標籤
/// </summary>
public class ProfileHeaderViewModel
{
    /// <summary>
    /// 沒有設定橫幅時的預設橫幅
    /// </summary>
    public const string DefaultBannerUrl = "https://i.imgur.com/xcKo0kB.jpg";

    /// <summary>
    /// ProfileID
    /// </summary>
    public required string ProfileId { get; init; }

    /// <summary>
    /// 用戶名
    /// </summary>
    public required string UserName { get; init; }

    /// <summary>
    /// 頭像網址（沒有設定時為 Gravatar）
    /// </summary>
    public required string AvatarUrl { get; init; }

    /// <summary>
    /// 會員設定的橫幅網址；沒有設定時為 null，顯示 <see cref="DefaultBannerUrl"/>
    /// </summary>
    public string BannerUrl { get; init; }

    /// <summary>
    /// 是否為付費會員
    /// </summary>
    public bool Prime { get; init; }

    /// <summary>
    /// 是否為特約作家
    /// </summary>
    public bool Professional { get; init; }

    /// <summary>
    /// 是否被封鎖
    /// </summary>
    public bool Banned { get; init; }

    /// <summary>
    /// 發表的創作與一般文章數
    /// </summary>
    public int WorkCount { get; init; }

    /// <summary>
    /// 追蹤者人數
    /// </summary>
    public int FollowerCount { get; init; }

    /// <summary>
    /// 追蹤中的創作者人數
    /// </summary>
    public int FollowingCount { get; init; }

    /// <summary>
    /// 瀏覽者是否為本人
    /// </summary>
    public bool IsOwner { get; init; }

    /// <summary>
    /// 瀏覽者是否已登入
    /// </summary>
    public bool IsSignedIn { get; init; }

    /// <summary>
    /// 未登入時，登入後回到的網址（目前頁面）
    /// </summary>
    public string ReturnUrl { get; init; }

    /// <summary>
    /// 瀏覽者是否已追蹤這位會員
    /// </summary>
    public bool IsFollowing { get; init; }

    /// <summary>
    /// 瀏覽者是否可以編輯這個個人檔案
    /// </summary>
    public bool CanEdit { get; init; }

    /// <summary>
    /// 瀏覽者是否可以封鎖這位會員
    /// </summary>
    public bool CanBan { get; init; }

    /// <summary>
    /// 目前所在的分頁 Key
    /// </summary>
    public required string ActiveTab { get; init; }

    /// <summary>
    /// 瀏覽者看得到的分頁
    /// </summary>
    public IReadOnlyList<ProfileTab> Tabs { get; init; } = [];
}
