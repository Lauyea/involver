namespace Involver.Models.ViewModels.Help;

/// <summary>
/// 說明與政策頁的標頭（_HelpPageHeaderPartial）
/// </summary>
/// <param name="Title">頁面標題</param>
/// <param name="Lead">標題下方的一句說明</param>
/// <param name="UpdatedOn">最後更新日期；政策頁才需要，其他說明頁傳 null</param>
public record HelpPageHeaderViewModel(string Title, string Lead, DateOnly? UpdatedOn = null);
