using DataAccess.Data;

using Involver.Areas.Admin.Pages.Users;
using Involver.Authorization.Admin;
using Involver.Services.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Admin.Pages.People;

/// <summary>
/// A-5 任命管理：列出所有管理角色與成員；只有用戶管理員可以新增、移除
/// </summary>
/// <param name="context">資料庫內容</param>
/// <param name="userManager">帳號管理</param>
/// <param name="roleManager">角色管理</param>
/// <param name="authorizationService">授權服務</param>
/// <param name="auditService">管理者操作紀錄</param>
[Authorize(Policy = AdminPolicies.Users)]
public class IndexModel(
    ApplicationDbContext context,
    UserManager<InvolverUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IAuthorizationService authorizationService,
    IAdminAuditService auditService) : AdminPageModel
{
    /// <summary>
    /// 角色的一位成員
    /// </summary>
    /// <param name="UserId">用戶 ID</param>
    /// <param name="UserName">用戶名</param>
    public record Member(string UserId, string UserName);

    /// <summary>
    /// 各角色的成員，依 <see cref="AdminPolicies.AllRoles"/> 的順序
    /// </summary>
    public List<(string Role, List<Member> Members)> Roles { get; private set; } = [];

    /// <summary>
    /// 目前的使用者能否任命角色
    /// </summary>
    public bool CanAssignRoles { get; private set; }

    /// <summary>
    /// 載入角色與成員
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        CanAssignRoles = (await authorizationService.AuthorizeAsync(User, AdminPolicies.RoleAssignment)).Succeeded;

        var memberships = await context.UserRoles
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .Join(context.Profiles, x => x.UserId, p => p.ProfileID, (x, p) => new { x.UserId, x.Name, p.UserName })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        Roles = AdminPolicies.AllRoles
            .Select(role => (role, memberships
                .Where(m => m.Name == role)
                .Select(m => new Member(m.UserId, m.UserName))
                .OrderBy(m => m.UserName, StringComparer.Ordinal)
                .ToList()))
            .ToList();
    }

    /// <summary>
    /// 任命角色；用戶可以用用戶名或用戶 ID 指定
    /// </summary>
    /// <param name="role">角色</param>
    /// <param name="user">用戶名或用戶 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostAddAsync(string role, string user, CancellationToken cancellationToken)
    {
        if (!(await authorizationService.AuthorizeAsync(User, AdminPolicies.RoleAssignment)).Succeeded)
        {
            return Forbid();
        }

        var keyword = user?.Trim() ?? string.Empty;
        var userId = await context.Profiles
            .Where(p => p.UserName == keyword || p.ProfileID == keyword)
            .Select(p => p.ProfileID)
            .FirstOrDefaultAsync(cancellationToken);

        StatusMessage = userId == null
            ? $"錯誤：找不到用戶「{keyword}」。"
            : await RoleAssignment.AddAsync(context, userManager, roleManager, auditService, userId, role, cancellationToken);
        return RedirectToPage();
    }

    /// <summary>
    /// 移除角色
    /// </summary>
    /// <param name="role">角色</param>
    /// <param name="userId">用戶 ID</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>回到本頁</returns>
    public async Task<IActionResult> OnPostRemoveAsync(string role, string userId, CancellationToken cancellationToken)
    {
        if (!(await authorizationService.AuthorizeAsync(User, AdminPolicies.RoleAssignment)).Succeeded)
        {
            return Forbid();
        }

        StatusMessage = await RoleAssignment.RemoveAsync(context, userManager, auditService, User, userId, role, cancellationToken);
        return RedirectToPage();
    }
}
