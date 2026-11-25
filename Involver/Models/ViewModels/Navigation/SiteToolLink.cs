namespace Involver.Models.ViewModels.Navigation;

/// <summary>
/// 一個免費工具（頁首的工具選單、工具箱頁共用）
/// </summary>
/// <param name="Title">工具名稱</param>
/// <param name="Description">一句話說明工具的用途</param>
/// <param name="PagePath">
/// 工具頁的 Razor Page 路徑（絕對路徑），由 asp-page 產生網址；
/// 不用相對網址，否則從 /Functions（結尾沒有斜線）點進去會變成 /CreateCover 這種少了一層的路徑
/// </param>
/// <param name="IconClass">Font Awesome 圖示的 class</param>
public record SiteToolLink(string Title, string Description, string PagePath, string IconClass);
