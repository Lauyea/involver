using DataAccess.Data;

using Involver.Authorization.Admin;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Admin.Pages.Users;

/// <summary>
/// 用戶管理：搜尋（用戶名、ID）、篩選與分頁
/// </summary>
/// <param name="context">資料庫內容</param>
[Authorize(Policy = AdminPolicies.Users)]
public class IndexModel(ApplicationDbContext context) : AdminPageModel
{
    /// <summary>
    /// 每頁筆數
    /// </summary>
    public const int PageSize = 25;

    /// <summary>
    /// 用戶列表的一列
    /// </summary>
    public class UserRow
    {
        /// <summary>用戶 ID</summary>
        public string ProfileId { get; init; }

        /// <summary>用戶名</summary>
        public string UserName { get; init; }

        /// <summary>註冊日</summary>
        public DateTime EnrollmentDate { get; init; }

        /// <summary>最後登入</summary>
        public DateTime LastTimeLogin { get; init; }

        /// <summary>Prime 會員</summary>
        public bool Prime { get; init; }

        /// <summary>特約作家</summary>
        public bool Professional { get; init; }

        /// <summary>封鎖</summary>
        public bool Banned { get; init; }

        /// <summary>管理角色</summary>
        public List<string> Roles { get; set; } = [];
    }

    /// <summary>
    /// 搜尋用戶名或 ID
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string Search { get; set; }

    /// <summary>
    /// 只列 Prime 會員
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool Prime { get; set; }

    /// <summary>
    /// 只列特約作家
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool Professional { get; set; }

    /// <summary>
    /// 只列被封鎖的用戶
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool Banned { get; set; }

    /// <summary>
    /// 只列有管理角色的用戶
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public bool HasRole { get; set; }

    /// <summary>
    /// 頁碼
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public int PageIndex { get; set; } = 1;

    /// <summary>
    /// 總頁數
    /// </summary>
    public int TotalPages { get; private set; }

    /// <summary>
    /// 符合條件的用戶數
    /// </summary>
    public int TotalCount { get; private set; }

    /// <summary>
    /// 本頁的用戶
    /// </summary>
    public List<UserRow> Users { get; private set; } = [];

    /// <summary>
    /// 載入用戶列表
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var query = context.Profiles.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var keyword = Search.Trim();
            query = query.Where(p => p.UserName.Contains(keyword) || p.ProfileID == keyword);
        }

        if (Prime)
        {
            query = query.Where(p => p.Prime);
        }

        if (Professional)
        {
            query = query.Where(p => p.Professional);
        }

        if (Banned)
        {
            query = query.Where(p => p.Banned);
        }

        if (HasRole)
        {
            query = query.Where(p => context.UserRoles.Any(ur => ur.UserId == p.ProfileID));
        }

        TotalCount = await query.CountAsync(cancellationToken);
        TotalPages = Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
        PageIndex = Math.Clamp(PageIndex, 1, TotalPages);

        Users = await query
            .OrderByDescending(p => p.EnrollmentDate)
            .Skip((PageIndex - 1) * PageSize)
            .Take(PageSize)
            .Select(p => new UserRow
            {
                ProfileId = p.ProfileID,
                UserName = p.UserName,
                EnrollmentDate = p.EnrollmentDate,
                LastTimeLogin = p.LastTimeLogin,
                Prime = p.Prime,
                Professional = p.Professional,
                Banned = p.Banned,
            })
            .ToListAsync(cancellationToken);

        var ids = Users.Select(u => u.ProfileId).ToList();
        var roles = await context.UserRoles
            .Where(ur => ids.Contains(ur.UserId))
            .Join(context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        foreach (var user in Users)
        {
            user.Roles = roles.Where(r => r.UserId == user.ProfileId).Select(r => r.Name).OrderBy(n => n).ToList();
        }
    }
}
