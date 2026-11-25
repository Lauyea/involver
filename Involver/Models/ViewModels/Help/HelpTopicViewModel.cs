namespace Involver.Models.ViewModels.Help;

/// <summary>
/// 說明中心的一張主題卡片
/// </summary>
/// <param name="Title">主題名稱</param>
/// <param name="Description">一句話說明這頁可以找到什麼</param>
/// <param name="IconClass">Font Awesome 圖示的 class，例如 <c>fa-solid fa-coins</c></param>
/// <param name="PagePath">Razor Page 路徑，例如 <c>/Descriptions/InCoin</c></param>
public record HelpTopicViewModel(string Title, string Description, string IconClass, string PagePath);

/// <summary>
/// 說明中心的一組主題（例如「使用說明」「政策」）
/// </summary>
/// <param name="Title">分組標題</param>
/// <param name="Topics">分組內的主題</param>
public record HelpTopicGroupViewModel(string Title, IReadOnlyList<HelpTopicViewModel> Topics);
