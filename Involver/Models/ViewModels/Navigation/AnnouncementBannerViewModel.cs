namespace Involver.Models.ViewModels.Navigation;

/// <summary>
/// 頁首上方的最新公告橫幅
/// </summary>
/// <param name="ArticleId">公告的文章 ID</param>
/// <param name="Title">公告標題</param>
public record AnnouncementBannerViewModel(int ArticleId, string Title);
