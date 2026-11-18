using System.Security.Claims;

using Involver.Authorization.Article;
using Involver.Authorization.Novel;

namespace Involver.Authorization.Tag;

/// <summary>
/// 標籤管理權限。目前沒有獨立的標籤角色，沿用小說、文章的管理員與管理者角色。
/// </summary>
public static class TagManagers
{
    /// <summary>
    /// 可以管理標籤（核准待審標籤、看重複報表）、新增標籤直接生效的角色
    /// </summary>
    public static readonly string[] Roles =
    [
        Novels.NovelAdministratorsRole,
        Novels.NovelManagersRole,
        Articles.ArticleAdministratorsRole,
        Articles.ArticleManagersRole,
    ];

    /// <summary>
    /// 使用者是否可以管理標籤
    /// </summary>
    /// <param name="user">目前的使用者</param>
    /// <returns>屬於任一管理角色時為 true</returns>
    public static bool IsTagManager(ClaimsPrincipal user)
    {
        return user != null && Roles.Any(user.IsInRole);
    }
}
