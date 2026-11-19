using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

using DataAccess.Data;
using DataAccess.Models.Dashboard;
using DataAccess.Services.Recommendations;

using Involver.Authorization.Admin;
using Involver.Services.Admin;
using Involver.Services.Recommendations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Admin.Pages.Recommendations;

/// <summary>
/// 推薦設定：權重與門檻（版本化，可以回到上一版）、各區塊點擊率、短時間暴增的作品。
/// 只有 *Administrators 可以查看和修改；每次修改都寫入 <see cref="AdminAuditLog"/>。
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="userManager">帳號管理</param>
/// <param name="auditService">管理者操作紀錄</param>
/// <param name="settingsService">推薦設定</param>
/// <param name="anomalyService">點擊率與異常警示</param>
[Authorize(Policy = AdminPolicies.Administrators)]
public class IndexModel(
    ApplicationDbContext context,
    UserManager<InvolverUser> userManager,
    IAdminAuditService auditService,
    IRecommendationSettingsService settingsService,
    IRecommendationAnomalyService anomalyService) : AdminPageModel
{
    /// <summary>
    /// 表單上的一個欄位
    /// </summary>
    /// <param name="Name">屬性名稱</param>
    /// <param name="Label">顯示名稱</param>
    /// <param name="Group">分組</param>
    /// <param name="Value">目前的值</param>
    /// <param name="DefaultValue">程式預設值</param>
    /// <param name="Min">最小值</param>
    /// <param name="Max">最大值</param>
    /// <param name="IsInteger">是否為整數</param>
    public record SettingField(string Name, string Label, string Group, string Value, string DefaultValue, string Min, string Max, bool IsInteger);

    /// <summary>
    /// 權重與門檻
    /// </summary>
    [BindProperty]
    public RecommendationSettings Input { get; set; } = new();

    /// <summary>
    /// 修改說明
    /// </summary>
    [BindProperty]
    [StringLength(200)]
    [Display(Name = "修改說明")]
    public string Note { get; set; }

    /// <summary>
    /// 目前啟用的設定
    /// </summary>
    public ActiveRecommendationSettings Active { get; private set; }

    /// <summary>
    /// 所有版本
    /// </summary>
    public List<RecommendationSettingVersion> Versions { get; private set; } = [];

    /// <summary>
    /// 表單欄位，依分組
    /// </summary>
    public List<IGrouping<string, SettingField>> FieldGroups { get; private set; } = [];

    /// <summary>
    /// 各區塊、各設定版本的點擊率
    /// </summary>
    public List<SectionCtr> Ctr { get; private set; } = [];

    /// <summary>
    /// 短時間暴增的作品
    /// </summary>
    public List<ContentSpike> Spikes { get; private set; } = [];

    /// <summary>
    /// 最近一次計算推薦的時間（UTC）
    /// </summary>
    public DateTime? LastComputedAt { get; private set; }

    /// <summary>
    /// 最近一次計算有推薦的會員數
    /// </summary>
    public int RecommendedUsers { get; private set; }

    /// <summary>
    /// 載入設定、版本、點擊率與異常警示
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
        Input = Active.Settings.Clone();
    }

    /// <summary>
    /// 儲存成新版本並啟用
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostSaveAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        var version = await settingsService.SaveAsync(Input, Note, userManager.GetUserId(User), cancellationToken);
        StatusMessage = $"已儲存並啟用 v{version}。下一次排程計算（每小時）後生效，也可以按「立即重新計算」。";
        return RedirectToPage(new { Days });
    }

    /// <summary>
    /// 啟用指定版本
    /// </summary>
    /// <param name="version">版本號</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostActivateAsync(int version, CancellationToken cancellationToken)
    {
        if (!await settingsService.ActivateAsync(version, cancellationToken))
        {
            return NotFound();
        }

        StatusMessage = $"已啟用 v{version}。";
        return RedirectToPage(new { Days });
    }

    /// <summary>
    /// 回到上一版
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostRollbackAsync(CancellationToken cancellationToken)
    {
        var version = await settingsService.RollbackAsync(cancellationToken);
        StatusMessage = version is { } v ? $"已回到 v{v}。" : "錯誤：沒有上一版可以回復。";
        return RedirectToPage(new { Days });
    }

    /// <summary>
    /// 立即重新計算所有活躍會員的推薦（平常由每小時的排程計算）
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostRecomputeAsync(CancellationToken cancellationToken)
    {
        // 和排程（Azure Function）一樣：整批刪除、寫入所有會員的推薦，預設 30 秒逾時可能不夠
        context.Database.SetCommandTimeout(TimeSpan.FromMinutes(5));
        var result = await new RecommendationEngine(context).RunAsync(DateTime.Now, cancellationToken);
        var version = result.SettingVersion is { } v ? $"v{v}" : "程式預設值";

        await auditService.AddAsync(
            AdminAuditAction.RecommendationRecomputed,
            RecommendationSettingsService.AuditTargetType,
            result.SettingVersion?.ToString(CultureInfo.InvariantCulture),
            after: $"{version}：{result.Users} 位會員、{result.Rows} 筆推薦",
            cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        StatusMessage = $"已用 {version} 重新計算：{result.Users} 位會員、{result.Rows} 筆推薦。";
        return RedirectToPage(new { Days });
    }

    /// <summary>
    /// 建立表單欄位：顯示名稱、分組與範圍取自 <see cref="RecommendationSettings"/> 的屬性
    /// </summary>
    /// <param name="values">目前的值</param>
    /// <returns>表單欄位</returns>
    public static List<SettingField> BuildFields(RecommendationSettings values)
    {
        var defaults = new RecommendationSettings();
        return typeof(RecommendationSettings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .Select(p =>
            {
                var display = p.GetCustomAttribute<DisplayAttribute>();
                var range = p.GetCustomAttribute<RangeAttribute>();
                return new SettingField(
                    p.Name,
                    display?.GetName() ?? p.Name,
                    display?.GetGroupName() ?? "其他",
                    Format(p.GetValue(values)),
                    Format(p.GetValue(defaults)),
                    Format(range?.Minimum),
                    Format(range?.Maximum),
                    p.PropertyType == typeof(int));
            })
            .ToList();
    }

    private static string Format(object value) => Convert.ToString(value, CultureInfo.InvariantCulture);

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        Active = await settingsService.GetActiveAsync(cancellationToken);
        Versions = await settingsService.GetVersionsAsync(cancellationToken);
        FieldGroups = BuildFields(ModelState.IsValid ? Active.Settings : Input)
            .GroupBy(f => f.Group)
            .ToList();
        Ctr = await anomalyService.GetCtrAsync(Days, cancellationToken);
        Spikes = await anomalyService.GetSpikesAsync(Active.Settings, DateTime.Now, cancellationToken);

        var latest = await context.UserRecommendations
            .GroupBy(r => r.ComputedAt)
            .OrderByDescending(g => g.Key)
            .Select(g => new { ComputedAt = g.Key, Users = g.Select(r => r.ProfileId).Distinct().Count() })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);
        LastComputedAt = latest?.ComputedAt;
        RecommendedUsers = latest?.Users ?? 0;

        ViewData["ShowPeriodSwitch"] = true;
    }
}
