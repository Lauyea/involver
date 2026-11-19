using DataAccess.Models.StatisticalData;

using Involver.Services.Admin;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Creator;

/// <summary>
/// 創作者中心頁面的共同基底：只看登入者本人的數據，期間切換（7 / 30 / 90 天）
/// </summary>
/// <param name="userManager">帳號管理</param>
public abstract class CreatorPageModel(UserManager<DataAccess.Data.InvolverUser> userManager) : PageModel
{
    /// <summary>
    /// 期間天數；不在選項內時改成預設的 30 天
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int Days { get; set; } = DashboardPeriod.Default;

    /// <summary>
    /// 登入者的 ProfileID
    /// </summary>
    protected string UserId => userManager.GetUserId(User);

    /// <summary>
    /// 綁定完成後統一期間天數
    /// </summary>
    /// <param name="context">頁面處理器執行內容</param>
    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        Days = DashboardPeriod.Normalize(Days);
        ViewData["Days"] = Days;
        ViewData["ContainerClass"] = "container-xxl";
        base.OnPageHandlerExecuting(context);
    }
}

/// <summary>
/// 創作者收入的來源分類（依讀者付款的交易類型）
/// </summary>
public static class CreatorIncomeSource
{
    /// <summary>
    /// 分類名稱，順序就是圖表的順序
    /// </summary>
    public static readonly string[] Names = ["投票", "直接贊助作品", "直接贊助創作者", "每月贊助", "其他"];

    /// <summary>
    /// 依讀者付款的交易類型分類收入
    /// </summary>
    /// <param name="spendType">讀者付款的交易類型；找不到對應的付款時為 null</param>
    /// <returns>分類名稱</returns>
    public static string Classify(CoinTransactionType? spendType) => spendType switch
    {
        CoinTransactionType.Vote => "投票",
        CoinTransactionType.InvolveNovel or CoinTransactionType.InvolveArticle => "直接贊助作品",
        CoinTransactionType.InvolveCreator => "直接贊助創作者",
        CoinTransactionType.MonthlyInvolveNovel or CoinTransactionType.MonthlyInvolveCreator => "每月贊助",
        _ => "其他",
    };
}
