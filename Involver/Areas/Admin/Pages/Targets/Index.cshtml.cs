using System.ComponentModel.DataAnnotations;

using DataAccess.Data;
using DataAccess.Models.Dashboard;

using Involver.Authorization.Admin;
using Involver.Extensions;
using Involver.Services.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Admin.Pages.Targets;

/// <summary>
/// KPI 目標值：設定後，總覽上對應的 KPI 卡片會顯示達成進度（OKR 的 Key Result）
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="userManager">帳號管理</param>
/// <param name="auditService">管理者操作紀錄</param>
[Authorize(Policy = AdminPolicies.Administrators)]
public class IndexModel(
    ApplicationDbContext context,
    UserManager<InvolverUser> userManager,
    IAdminAuditService auditService) : AdminPageModel
{
    /// <summary>
    /// 設定目標的表單
    /// </summary>
    public class TargetInput
    {
        /// <summary>指標</summary>
        [Display(Name = "指標")]
        public KpiMetric Metric { get; set; } = KpiMetric.WeeklyVoters;

        /// <summary>期間</summary>
        [Display(Name = "期間")]
        public KpiPeriod Period { get; set; } = KpiPeriod.Quarter;

        /// <summary>期間內的任一天，會換算成期間的第一天</summary>
        [Display(Name = "期間（任一天）")]
        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Now);

        /// <summary>目標值</summary>
        [Range(1, 100000000, ErrorMessage = "目標值必須大於 0")]
        [Display(Name = "目標值")]
        public decimal TargetValue { get; set; }

        /// <summary>備註</summary>
        [StringLength(200)]
        [Display(Name = "備註")]
        public string Note { get; set; }
    }

    /// <summary>
    /// 設定目標的表單
    /// </summary>
    [BindProperty]
    public TargetInput Input { get; set; } = new();

    /// <summary>
    /// 進行中與未來的目標
    /// </summary>
    public List<KpiTarget> ActiveTargets { get; private set; } = [];

    /// <summary>
    /// 已結束的目標（最近 20 筆）
    /// </summary>
    public List<KpiTarget> PastTargets { get; private set; } = [];

    /// <summary>
    /// 今天
    /// </summary>
    public DateOnly Today { get; } = DateOnly.FromDateTime(DateTime.Now);

    /// <summary>
    /// 載入目標
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    /// <summary>
    /// 新增或更新目標；同一指標、同一期間只會有一個目標
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

        var start = KpiTargetPeriods.GetStart(Input.Period, Input.Date);
        var target = await context.KpiTargets.FirstOrDefaultAsync(
            t => t.Metric == Input.Metric && t.Period == Input.Period && t.PeriodStart == start, cancellationToken);
        var before = target == null ? null : $"{target.TargetValue:N0}";

        if (target == null)
        {
            target = new KpiTarget { Metric = Input.Metric, Period = Input.Period, PeriodStart = start };
            context.KpiTargets.Add(target);
        }

        target.TargetValue = Input.TargetValue;
        target.Note = Input.Note?.Trim();
        target.CreatedByProfileId = userManager.GetUserId(User);
        target.UpdatedAt = DateTime.UtcNow;

        var label = $"{Input.Metric.GetDisplayName()} {KpiTargetPeriods.GetLabel(Input.Period, start)}";
        await auditService.AddAsync(
            AdminAuditAction.KpiTargetSaved, "KpiTarget", label,
            before: before == null ? null : $"{label} 目標 {before}",
            after: $"{label} 目標 {Input.TargetValue:N0}",
            cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        StatusMessage = $"已設定 {label} 的目標為 {Input.TargetValue:N0}。";
        return RedirectToPage();
    }

    /// <summary>
    /// 刪除目標
    /// </summary>
    /// <param name="id">目標 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostDeleteAsync(int id, CancellationToken cancellationToken)
    {
        var target = await context.KpiTargets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (target == null)
        {
            return NotFound();
        }

        var label = $"{target.Metric.GetDisplayName()} {KpiTargetPeriods.GetLabel(target.Period, target.PeriodStart)}";
        context.KpiTargets.Remove(target);
        await auditService.AddAsync(
            AdminAuditAction.KpiTargetDeleted, "KpiTarget", label,
            before: $"{label} 目標 {target.TargetValue:N0}",
            after: "已刪除",
            cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        StatusMessage = $"已刪除 {label} 的目標。";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var targets = await context.KpiTargets
            .OrderByDescending(t => t.PeriodStart)
            .ThenBy(t => t.Metric)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        ActiveTargets = targets.Where(t => KpiTargetPeriods.GetEnd(t.Period, t.PeriodStart) > Today).ToList();
        PastTargets = targets.Except(ActiveTargets).Take(20).ToList();
    }
}
