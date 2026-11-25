using System.Text.RegularExpressions;

using DataAccess.Common;
using DataAccess.Data;

using Involver.Extensions;
using Involver.Helpers;
using Involver.Models.ViewModels;

using Microsoft.EntityFrameworkCore;

namespace Involver.Services.NotificationSetterService;

/// <inheritdoc cref="INotificationInbox"/>
public partial class NotificationInbox(ApplicationDbContext context) : INotificationInbox
{
    /// <inheritdoc />
    public async Task<List<NotificationItemViewModel>> GetItemsAsync(string userId, int skip, int take, bool unreadOnly, CancellationToken cancellationToken = default)
    {
        var notifications = await context.Notifications
            .Where(n => n.ProfileID == userId && (!unreadOnly || !n.IsRead))
            .OrderByDescending(n => n.CreatedDate)
            .ThenByDescending(n => n.Id)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var actorIds = notifications
            .Where(n => n.ActorID != null)
            .Select(n => n.ActorID)
            .Distinct()
            .ToList();

        var actors = await context.Profiles
            .Where(p => actorIds.Contains(p.ProfileID))
            .Select(p => new { p.ProfileID, p.UserName, p.ImageUrl })
            .AsNoTracking()
            .ToDictionaryAsync(p => p.ProfileID, cancellationToken);

        // 沒有設定頭像的人和評論區一樣用 Gravatar
        var missingImageIds = actors.Values
            .Where(a => string.IsNullOrEmpty(a.ImageUrl))
            .Select(a => a.ProfileID)
            .ToList();

        var emails = await context.Users
            .Where(u => missingImageIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .AsNoTracking()
            .ToDictionaryAsync(u => u.Id, u => u.Email, cancellationToken);

        return notifications.Select(n =>
        {
            var (icon, tone) = Describe(n.Type);

            string actorName = null;
            string actorImageUrl = null;

            if (n.ActorID != null)
            {
                if (actors.TryGetValue(n.ActorID, out var actor))
                {
                    actorName = actor.UserName;
                    actorImageUrl = !string.IsNullOrEmpty(actor.ImageUrl)
                        ? actor.ImageUrl
                        : $"https://www.gravatar.com/avatar/{(emails.GetValueOrDefault(actor.ProfileID) ?? string.Empty).ToMd5()}?d=retro";
                }
                else
                {
                    actorName = "有人";
                }
            }

            return new NotificationItemViewModel
            {
                Id = n.Id,
                Type = n.Type,
                IconClass = icon,
                Tone = tone,
                ActorName = actorName,
                ActorImageUrl = actorImageUrl,
                Title = n.Title,
                Excerpt = n.Excerpt,
                IsRead = n.IsRead,
                CreatedDate = n.CreatedDate,
                TimeAgo = TimePeriodHelper.Get(n.CreatedDate),
            };
        }).ToList();
    }

    /// <inheritdoc />
    public Task<int> CountUnreadAsync(string userId, int cap, CancellationToken cancellationToken = default)
    {
        return context.Notifications
            .Where(n => n.ProfileID == userId && !n.IsRead)
            .Take(cap)
            .CountAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task MarkAllReadAsync(string userId, CancellationToken cancellationToken = default)
    {
        return context.Notifications
            .Where(n => n.ProfileID == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> OpenAsync(string userId, int id, CancellationToken cancellationToken = default)
    {
        var notification = await context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.ProfileID == userId, cancellationToken);

        if (notification == null)
        {
            return null;
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            await context.SaveChangesAsync(cancellationToken);
        }

        return await ResolveUrlAsync(notification.Url, cancellationToken);
    }

    /// <summary>
    /// 把通知的網址轉成站內相對網址，並修正舊資料的錯誤連結
    /// </summary>
    /// <remarks>
    /// 舊資料的問題：
    /// <list type="bullet">
    /// <item>存成含網域的完整網址（換網域或在不同 port 測試時會連到別的站）</item>
    /// <item>留言通知連到已移除的 /Comments/Details</item>
    /// <item>留言被讚的通知固定連到 /Episodes，評論不在章節時會 404</item>
    /// <item>公告、意見回饋的評論通知連到 /Articles</item>
    /// <item>小說頁用了 ?id=，但小說頁的路由是 /Novels/Details/{id}</item>
    /// </list>
    /// 舊資料的評論連結一律依評論重新找出所在的頁面。
    /// </remarks>
    /// <param name="url">通知存的網址</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>站內相對網址；無效時為 null</returns>
    private async Task<string> ResolveUrlAsync(string url, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        string local;
        bool isLegacy;

        if (url.StartsWith('/'))
        {
            local = url;
            isLegacy = false;
        }
        else if (Uri.TryCreate(url, UriKind.Absolute, out var absolute)
            && (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps))
        {
            local = absolute.PathAndQuery + absolute.Fragment;
            isLegacy = true;
        }
        else
        {
            return null;
        }

        // 防止 //evil.com 或 /\evil.com 這類會被瀏覽器當成外部網址的路徑
        if (local.StartsWith("//") || local.StartsWith("/\\"))
        {
            return null;
        }

        var removedPage = RemovedCommentPageRegex().Match(local);
        if (removedPage.Success)
        {
            var commentId = int.Parse(removedPage.Groups[1].Value);
            var target = await CommentTargets.ResolveAsync(context, commentId, cancellationToken);
            return target?.RepliesUrl(commentId);
        }

        var hashIndex = local.IndexOf('#');
        if (isLegacy && hashIndex >= 0)
        {
            var fragment = CommentFragmentRegex().Match(local[hashIndex..]);
            if (fragment.Success)
            {
                var commentId = int.Parse(fragment.Groups[1].Value);
                var target = await CommentTargets.ResolveAsync(context, commentId, cancellationToken);

                // 評論已刪除時，舊網址多半是錯的（例如 /Episodes/Details?id=），回到通知頁
                if (target == null)
                {
                    return null;
                }

                return fragment.Groups[2].Success
                    ? target.RepliesUrl(commentId)
                    : target.CommentUrl(commentId);
            }
        }

        // 小說頁只接受 /Novels/Details/123，查詢字串形式會 404
        var novelPage = NovelQueryStringRegex().Match(local);
        if (novelPage.Success)
        {
            return $"/Novels/Details/{novelPage.Groups[1].Value}{novelPage.Groups[2].Value}";
        }

        return local;
    }

    /// <summary>
    /// 通知種類對應的圖示與色調
    /// </summary>
    /// <param name="type">通知種類</param>
    private static (string Icon, string Tone) Describe(NotificationType type) => type switch
    {
        NotificationType.Comment => ("fa-comment", "comment"),
        NotificationType.Reply => ("fa-reply", "comment"),
        NotificationType.CommentAgreed or NotificationType.ReplyAgreed => ("fa-thumbs-up", "agree"),
        NotificationType.FeedbackAccepted => ("fa-circle-check", "success"),
        NotificationType.Achievement => ("fa-trophy", "achievement"),
        _ => ("fa-bell", "general"),
    };

    /// <summary>
    /// 已移除的評論頁：/Comments/Details?id=123
    /// </summary>
    [GeneratedRegex(@"^/Comments/Details/?\?id=(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex RemovedCommentPageRegex();

    /// <summary>
    /// 查詢字串形式的小說頁：/Novels/Details?id=123 或 /Novels/Details/?id=123（第 2 組是後面的錨點）
    /// </summary>
    [GeneratedRegex(@"^/Novels/Details/?\?id=(\d+)(#.*)?$", RegexOptions.IgnoreCase)]
    private static partial Regex NovelQueryStringRegex();

    /// <summary>
    /// 評論錨點：#comment-123 或 #comment-123-messages
    /// </summary>
    [GeneratedRegex(@"^#comment-(\d+)(-messages)?$")]
    private static partial Regex CommentFragmentRegex();
}
