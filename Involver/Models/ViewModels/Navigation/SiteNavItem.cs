namespace Involver.Models.ViewModels.Navigation;

/// <summary>
/// 網站的主要入口（頁首導覽與手機底部導覽列共用）
/// </summary>
/// <param name="Label">顯示名稱</param>
/// <param name="PagePath">Razor Page 路徑，例如 <c>/Novels/Index</c></param>
/// <param name="IconClass">Font Awesome 圖示的 class</param>
/// <param name="PathPrefixes">屬於這個入口的網址前綴；<c>/</c> 只比對首頁本身</param>
public record SiteNavItem(string Label, string PagePath, string IconClass, IReadOnlyList<string> PathPrefixes)
{
    /// <summary>
    /// 目前的網址是否屬於這個入口（用來標示目前所在的頁面）
    /// </summary>
    /// <param name="requestPath">目前請求的路徑，例如 <c>/Novels/Details/3</c></param>
    /// <returns>屬於這個入口時為 true</returns>
    public bool IsActive(string requestPath)
    {
        var path = string.IsNullOrEmpty(requestPath) ? "/" : requestPath;

        return PathPrefixes.Any(prefix => prefix == "/"
            ? path == "/"
            : path.Equals(prefix, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith($"{prefix}/", StringComparison.OrdinalIgnoreCase));
    }
}
