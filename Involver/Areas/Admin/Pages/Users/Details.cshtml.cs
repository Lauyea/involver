using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.Dashboard;

using Involver.Authorization.Admin;
using Involver.Authorization.Profile;
using Involver.Services.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Admin.Pages.Users;

/// <summary>
/// 用戶詳細資料：任命角色、設定特約作家、封鎖。所有變更都寫入管理者操作紀錄
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="userManager">帳號管理</param>
/// <param name="roleManager">角色管理</param>
/// <param name="authorizationService">授權服務</param>
/// <param name="auditService">管理者操作紀錄</param>
[Authorize(Policy = AdminPolicies.Users)]
public class DetailsModel(
    ApplicationDbContext context,
    UserManager<InvolverUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IAuthorizationService authorizationService,
    IAdminAuditService auditService) : AdminPageModel
{
    /// <summary>
    /// 用戶 ID
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Id { get; set; }

    /// <summary>
    /// 要任命的角色
    /// </summary>
    [BindProperty]
    public string Role { get; set; }

    /// <summary>
    /// 用戶資料
    /// </summary>
    public Profile Profile { get; private set; }

    /// <summary>
    /// Email
    /// </summary>
    public string Email { get; private set; }

    /// <summary>
    /// 目前的角色
    /// </summary>
    public IList<string> Roles { get; private set; } = [];

    /// <summary>
    /// 作品數
    /// </summary>
    public int NovelCount { get; private set; }

    /// <summary>
    /// 文章數
    /// </summary>
    public int ArticleCount { get; private set; }

    /// <summary>
    /// 目前的使用者能否任命角色
    /// </summary>
    public bool CanAssignRoles { get; private set; }

    /// <summary>
    /// 這位用戶的最近幾筆管理操作紀錄
    /// </summary>
    public List<AdminAuditLog> RecentLogs { get; private set; } = [];

    /// <summary>
    /// 載入用戶資料
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>頁面；找不到用戶時 404</returns>
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        return await LoadAsync(cancellationToken) ? Page() : NotFound();
    }

    /// <summary>
    /// 封鎖或解除封鎖
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostToggleBanAsync(CancellationToken cancellationToken)
    {
        var profile = await context.Profiles.FirstOrDefaultAsync(p => p.ProfileID == Id, cancellationToken);
        if (profile == null)
        {
            return NotFound();
        }

        if (profile.ProfileID == userManager.GetUserId(User))
        {
            StatusMessage = "錯誤：不能封鎖自己。";
            return RedirectToPage(new { Id });
        }

        profile.Banned = !profile.Banned;
        await auditService.AddAsync(
            profile.Banned ? AdminAuditAction.UserBanned : AdminAuditAction.UserUnbanned,
            "User", profile.ProfileID,
            before: $"{profile.UserName}：{(profile.Banned ? "正常" : "封鎖")}",
            after: profile.Banned ? "封鎖" : "正常",
            cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        StatusMessage = profile.Banned ? $"已封鎖 {profile.UserName}。" : $"已解除封鎖 {profile.UserName}。";
        return RedirectToPage(new { Id });
    }

    /// <summary>
    /// 設定或取消特約作家
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostToggleProfessionalAsync(CancellationToken cancellationToken)
    {
        var profile = await context.Profiles.FirstOrDefaultAsync(p => p.ProfileID == Id, cancellationToken);
        if (profile == null)
        {
            return NotFound();
        }

        profile.Professional = !profile.Professional;
        await auditService.AddAsync(
            profile.Professional ? AdminAuditAction.ProfessionalGranted : AdminAuditAction.ProfessionalRevoked,
            "User", profile.ProfileID,
            before: $"{profile.UserName}：{(profile.Professional ? "一般作者" : "特約作家")}",
            after: profile.Professional ? "特約作家" : "一般作者",
            cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        StatusMessage = profile.Professional ? $"已將 {profile.UserName} 設為特約作家。" : $"已取消 {profile.UserName} 的特約作家。";
        return RedirectToPage(new { Id });
    }

    /// <summary>
    /// 任命角色（只有用戶管理員可以操作）
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostAddRoleAsync(CancellationToken cancellationToken)
    {
        if (!(await authorizationService.AuthorizeAsync(User, AdminPolicies.RoleAssignment)).Succeeded)
        {
            return Forbid();
        }

        StatusMessage = await RoleAssignment.AddAsync(context, userManager, roleManager, auditService, Id, Role, cancellationToken);
        return RedirectToPage(new { Id });
    }

    /// <summary>
    /// 移除角色（只有用戶管理員可以操作）
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostRemoveRoleAsync(CancellationToken cancellationToken)
    {
        if (!(await authorizationService.AuthorizeAsync(User, AdminPolicies.RoleAssignment)).Succeeded)
        {
            return Forbid();
        }

        StatusMessage = await RoleAssignment.RemoveAsync(context, userManager, auditService, User, Id, Role, cancellationToken);
        return RedirectToPage(new { Id });
    }

    private async Task<bool> LoadAsync(CancellationToken cancellationToken)
    {
        Profile = await context.Profiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProfileID == Id, cancellationToken);
        if (Profile == null)
        {
            return false;
        }

        var user = await userManager.FindByIdAsync(Id);
        Email = user?.Email;
        Roles = user == null ? [] : (await userManager.GetRolesAsync(user)).OrderBy(r => r).ToList();
        NovelCount = await context.Novels.CountAsync(n => n.ProfileID == Id && !n.IsDeleted, cancellationToken);
        ArticleCount = await context.Articles.CountAsync(a => a.ProfileID == Id && !a.IsDeleted, cancellationToken);
        CanAssignRoles = (await authorizationService.AuthorizeAsync(User, AdminPolicies.RoleAssignment)).Succeeded;

        RecentLogs = await context.AdminAuditLogs
            .Where(l => l.TargetType == "User" && l.TargetId == Id)
            .OrderByDescending(l => l.CreatedAt)
            .Take(10)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return true;
    }
}

/// <summary>
/// 任命、移除角色的共用邏輯（用戶詳細資料、任命管理兩頁共用）
/// </summary>
public static class RoleAssignment
{
    /// <summary>
    /// 任命角色；角色不存在時先建立
    /// </summary>
    /// <returns>給管理者看的結果訊息</returns>
    public static async Task<string> AddAsync(
        ApplicationDbContext context,
        UserManager<InvolverUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IAdminAuditService auditService,
        string userId,
        string role,
        CancellationToken cancellationToken)
    {
        if (!AdminPolicies.AllRoles.Contains(role))
        {
            return "錯誤：角色不正確。";
        }

        var user = await userManager.FindByIdAsync(userId ?? string.Empty);
        if (user == null)
        {
            return "錯誤：找不到用戶。";
        }

        if (await userManager.IsInRoleAsync(user, role))
        {
            return $"錯誤：用戶已經是 {role}。";
        }

        // 任命與稽核紀錄在同一個交易中提交（UserManager 與頁面共用同一個 DbContext）
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        var result = await userManager.AddToRoleAsync(user, role);
        if (!result.Succeeded)
        {
            return $"錯誤：{string.Join("、", result.Errors.Select(e => e.Description))}";
        }

        var userName = await GetUserNameAsync(context, userId, cancellationToken);
        await auditService.AddAsync(AdminAuditAction.RoleAdded, "User", userId, before: userName, after: role, cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        // 讓對方的登入 cookie 在下次驗證時更新角色
        await userManager.UpdateSecurityStampAsync(user);
        return $"已任命 {userName} 為 {role}。";
    }

    /// <summary>
    /// 移除角色。不能移除自己的用戶管理員角色，避免沒有人能再任命
    /// </summary>
    /// <returns>給管理者看的結果訊息</returns>
    public static async Task<string> RemoveAsync(
        ApplicationDbContext context,
        UserManager<InvolverUser> userManager,
        IAdminAuditService auditService,
        System.Security.Claims.ClaimsPrincipal currentUser,
        string userId,
        string role,
        CancellationToken cancellationToken)
    {
        if (userId == userManager.GetUserId(currentUser) && role == Profiles.ProfileAdministratorsRole)
        {
            return "錯誤：不能移除自己的用戶管理員角色。";
        }

        var user = await userManager.FindByIdAsync(userId ?? string.Empty);
        if (user == null || string.IsNullOrEmpty(role) || !await userManager.IsInRoleAsync(user, role))
        {
            return "錯誤：用戶沒有這個角色。";
        }

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var result = await userManager.RemoveFromRoleAsync(user, role);
        if (!result.Succeeded)
        {
            return $"錯誤：{string.Join("、", result.Errors.Select(e => e.Description))}";
        }

        var userName = await GetUserNameAsync(context, userId, cancellationToken);
        await auditService.AddAsync(AdminAuditAction.RoleRemoved, "User", userId, before: $"{userName}：{role}", after: "已移除", cancellationToken: cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        await userManager.UpdateSecurityStampAsync(user);
        return $"已移除 {userName} 的 {role}。";
    }

    private static async Task<string> GetUserNameAsync(ApplicationDbContext context, string userId, CancellationToken cancellationToken)
    {
        return await context.Profiles
            .Where(p => p.ProfileID == userId)
            .Select(p => p.UserName)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken) ?? userId;
    }
}
