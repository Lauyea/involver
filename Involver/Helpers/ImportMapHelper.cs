using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Involver.Helpers;

/// <summary>
/// 產生前端 ES module 使用的 import map，讓瀏覽器不需打包工具即可用套件名稱 import。
/// </summary>
public static class ImportMapHelper
{
    /// <summary>
    /// import map 的對應表：key 為 JS 中 import 的名稱，value 為 wwwroot 底下的實際路徑。
    /// </summary>
    private static readonly IReadOnlyDictionary<string, string> _imports = new Dictionary<string, string>
    {
        ["ckeditor5"] = "/lib/ckeditor5/dist/browser/ckeditor5.js",
        ["ckeditor5/translations/zh.js"] = "/lib/ckeditor5/dist/translations/zh.js",
        ["/js/ckeditor-setup.js"] = "/js/ckeditor-setup.js",
        // Chart.js 以 LibMan 鎖定版本；ESM 版本依賴 @kurkle/color
        ["chart.js"] = "/lib/chart.js/dist/chart.js",
        ["@kurkle/color"] = "/lib/kurkle-color/dist/color.esm.js",
        ["/js/charts/chart-theme.js"] = "/js/charts/chart-theme.js",
    };

    /// <summary>
    /// 建立 import map，並在每個路徑加上檔案版本 (?v=hash)，確保檔案更新後瀏覽器會重新下載。
    /// </summary>
    /// <param name="fileVersionProvider">ASP.NET Core 的檔案版本提供者</param>
    /// <param name="pathBase">目前請求的 PathBase</param>
    /// <returns>可傳給 <c>&lt;script type="importmap" asp-importmap="..."&gt;</c> 的 import map</returns>
    public static ImportMapDefinition Create(IFileVersionProvider fileVersionProvider, PathString pathBase)
    {
        var imports = _imports.ToDictionary(
            entry => entry.Key,
            entry => fileVersionProvider.AddFileVersionToPath(pathBase, entry.Value));

        return new ImportMapDefinition(imports, scopes: null, integrity: null);
    }
}
