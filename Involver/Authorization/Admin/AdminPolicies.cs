using Involver.Authorization.Article;
using Involver.Authorization.Comment;
using Involver.Authorization.Message;
using Involver.Authorization.Novel;
using Involver.Authorization.Profile;
using Involver.Authorization.ProfitSharing;
using Involver.Authorization.Tag;
using Involver.Authorization.Voting;

using Microsoft.AspNetCore.Authorization;

using PaymentRoles = Involver.Authorization.Payment.Payments;

namespace Involver.Authorization.Admin;

/// <summary>
/// 管理員後台（Areas/Admin）的權限 policy。
/// 整個 Area 要求「擁有任一 *Managers 或 *Administrators 角色」，各區塊再依角色細分；
/// 側欄也用同一組 policy 判斷要顯示哪些項目。
/// </summary>
public static class AdminPolicies
{
    /// <summary>
    /// 進入後台（總覽）：任一管理角色
    /// </summary>
    public const string Access = "Admin.Access";

    /// <summary>
    /// 用戶管理（查詢、封鎖、設定特約作家）：用戶的管理者與管理員
    /// </summary>
    public const string Users = "Admin.Users";

    /// <summary>
    /// 任命或移除角色：只有用戶管理員
    /// </summary>
    public const string RoleAssignment = "Admin.RoleAssignment";

    /// <summary>
    /// 訂單、金流、網站支出：金流的管理者與管理員
    /// </summary>
    public const string Payments = "Admin.Payments";

    /// <summary>
    /// 分潤清單與標記撥款：分潤的管理者與管理員
    /// </summary>
    public const string Payouts = "Admin.Payouts";

    /// <summary>
    /// 標籤管理：小說、文章的管理者與管理員（同 <see cref="TagManagers"/>）
    /// </summary>
    public const string Tags = "Admin.Tags";

    /// <summary>
    /// 內容統計：小說、文章、留言的管理者與管理員
    /// </summary>
    public const string Content = "Admin.Content";

    /// <summary>
    /// 管理者活動紀錄、KPI 目標：任一 *Administrators 角色
    /// </summary>
    public const string Administrators = "Admin.Administrators";

    /// <summary>
    /// 所有管理角色。新增角色時要加在這裡，任命管理才列得出來
    /// </summary>
    public static readonly string[] AllRoles =
    [
        Profiles.ProfileAdministratorsRole,
        Profiles.ProfileManagersRole,
        PaymentRoles.PaymentAdministratorsRole,
        PaymentRoles.PaymentManagersRole,
        ProfitSharings.ProfitSharingAdministratorsRole,
        ProfitSharings.ProfitSharingManagersRole,
        Novels.NovelAdministratorsRole,
        Novels.NovelManagersRole,
        Articles.ArticleAdministratorsRole,
        Articles.ArticleManagersRole,
        Comments.CommentAdministratorsRole,
        Comments.CommentManagersRole,
        Messages.MessageAdministratorsRole,
        Messages.MessageManagersRole,
        Votings.VotingAdministratorsRole,
        Votings.VotingManagersRole,
    ];

    /// <summary>
    /// 所有 *Administrators 角色
    /// </summary>
    public static readonly string[] AdministratorRoles = AllRoles
        .Where(r => r.EndsWith("Administrators", StringComparison.Ordinal))
        .ToArray();

    /// <summary>
    /// 註冊後台用的 policy
    /// </summary>
    /// <param name="options">授權設定</param>
    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(Access, p => p.RequireRole(AllRoles));
        options.AddPolicy(Users, p => p.RequireRole(
            Profiles.ProfileAdministratorsRole,
            Profiles.ProfileManagersRole));
        options.AddPolicy(RoleAssignment, p => p.RequireRole(Profiles.ProfileAdministratorsRole));
        options.AddPolicy(Payments, p => p.RequireRole(
            PaymentRoles.PaymentAdministratorsRole,
            PaymentRoles.PaymentManagersRole));
        options.AddPolicy(Payouts, p => p.RequireRole(
            ProfitSharings.ProfitSharingAdministratorsRole,
            ProfitSharings.ProfitSharingManagersRole));
        options.AddPolicy(Tags, p => p.RequireRole(TagManagers.Roles));
        options.AddPolicy(Content, p => p.RequireRole(
            Novels.NovelAdministratorsRole,
            Novels.NovelManagersRole,
            Articles.ArticleAdministratorsRole,
            Articles.ArticleManagersRole,
            Comments.CommentAdministratorsRole,
            Comments.CommentManagersRole));
        options.AddPolicy(Administrators, p => p.RequireRole(AdministratorRoles));
    }
}
