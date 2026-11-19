using Involver.Models.ViewModels.Dashboard;

using Microsoft.AspNetCore.Mvc;

namespace Involver.Views.Shared.Components.KpiCard;

/// <summary>
/// KPI 卡片：數值、與上一期的比較、目標值進度。管理員後台與創作者中心共用
/// </summary>
public class KpiCardViewComponent : ViewComponent
{
    /// <summary>
    /// 顯示 KPI 卡片
    /// </summary>
    /// <param name="card">卡片內容</param>
    /// <returns>卡片</returns>
    public IViewComponentResult Invoke(KpiCardViewModel card)
    {
        return View(card);
    }
}
