#nullable enable

using DataAccess.Data;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Involver.Services;

/// <inheritdoc />
public class UserAccountService(
    ApplicationDbContext context,
    UserManager<InvolverUser> userManager) : IUserAccountService
{
    /// <summary>
    /// 匿名化後的用戶名前綴；只用 Identity 允許的字元，兩邊用同一個名字
    /// </summary>
    public const string DeletedUserNamePrefix = "deleted-";

    /// <summary>
    /// 匿名化後的 Email 網域（RFC 2606 保留網域，不會寄得出去）
    /// </summary>
    public const string DeletedEmailDomain = "deleted.invalid";

    /// <inheritdoc />
    public async Task<string?> ChangeUserNameAsync(InvolverUser user, string newUserName, CancellationToken cancellationToken = default)
    {
        if (await context.Profiles.AnyAsync(p => p.UserName == newUserName && p.ProfileID != user.Id, cancellationToken))
        {
            return "已有重複的用戶名";
        }

        // UserManager 與 context 是同一個 DbContext，兩邊的更新在同一個交易內完成
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        var result = await userManager.SetUserNameAsync(user, newUserName);
        if (!result.Succeeded)
        {
            return string.Join("；", result.Errors.Select(e => e.Description));
        }

        await context.Profiles
            .Where(p => p.ProfileID == user.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.UserName, newUserName)
                .SetProperty(p => p.CanChangeUserName, false), cancellationToken);

        await transaction.CommitAsync(cancellationToken);
        return null;
    }

    /// <inheritdoc />
    public async Task AnonymizeAsync(InvolverUser user, CancellationToken cancellationToken = default)
    {
        // ProfileID 是 GUID，取前 12 碼當作識別即可，碰撞機率可忽略；Unique 索引仍會擋下萬一的重複
        var anonymousName = $"{DeletedUserNamePrefix}{user.Id.Replace("-", string.Empty)[..12]}";

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

        // 外部登入、角色、Claims 與驗證 Token 都移除，帳號無法再登入
        foreach (var login in await userManager.GetLoginsAsync(user))
        {
            await userManager.RemoveLoginAsync(user, login.LoginProvider, login.ProviderKey);
        }

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Count > 0)
        {
            await userManager.RemoveFromRolesAsync(user, roles);
        }

        var claims = await userManager.GetClaimsAsync(user);
        if (claims.Count > 0)
        {
            await userManager.RemoveClaimsAsync(user, claims);
        }

        await context.UserTokens
            .Where(t => t.UserId == user.Id)
            .ExecuteDeleteAsync(cancellationToken);

        user.UserName = anonymousName;
        user.NormalizedUserName = userManager.NormalizeName(anonymousName);
        user.Email = $"{user.Id}@{DeletedEmailDomain}";
        user.NormalizedEmail = userManager.NormalizeEmail(user.Email);
        user.EmailConfirmed = false;
        user.PasswordHash = null;
        user.PhoneNumber = null;
        user.PhoneNumberConfirmed = false;
        user.TwoFactorEnabled = false;
        user.LockoutEnabled = true;
        user.LockoutEnd = DateTimeOffset.MaxValue;

        // 換掉 SecurityStamp，其他裝置上的登入狀態會失效
        await userManager.UpdateSecurityStampAsync(user);

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"匿名化使用者 '{user.Id}' 失敗：{string.Join("；", result.Errors.Select(e => e.Description))}");
        }

        // Profile 保留（作品、留言、流水帳都指向它），只清掉個人資料
        await context.Profiles
            .Where(p => p.ProfileID == user.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.UserName, anonymousName)
                .SetProperty(p => p.Introduction, (string?)null)
                .SetProperty(p => p.ImageUrl, (string?)null)
                .SetProperty(p => p.BannerImageUrl, (string?)null)
                .SetProperty(p => p.CanChangeUserName, false), cancellationToken);

        // 只屬於本人的私人紀錄直接刪除：通知、瀏覽紀錄
        await context.Notifications
            .Where(n => n.ProfileID == user.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await context.Set<ArticleViewer>()
            .Where(v => v.ProfileID == user.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await context.Set<NovelViewer>()
            .Where(v => v.ProfileID == user.Id)
            .ExecuteDeleteAsync(cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }
}
