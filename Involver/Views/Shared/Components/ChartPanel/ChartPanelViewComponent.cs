using Involver.Models.ViewModels.Dashboard;

using Microsoft.AspNetCore.Mvc;

namespace Involver.Views.Shared.Components.ChartPanel;

/// <summary>
/// 圖表區塊：輸出 &lt;canvas data-chart-config&gt;，由 /js/charts/chart-panels.js 建立 Chart.js 圖表。
/// 使用的頁面要載入 chart-panels.js 模組。管理員後台與創作者中心共用
/// </summary>
public class ChartPanelViewComponent : ViewComponent
{
    /// <summary>
    /// 顯示圖表區塊
    /// </summary>
    /// <param name="panel">圖表內容</param>
    /// <returns>圖表區塊</returns>
    public IViewComponentResult Invoke(ChartPanelViewModel panel)
    {
        return View(panel);
    }
}
