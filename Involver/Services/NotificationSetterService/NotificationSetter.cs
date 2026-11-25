using System.Net;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;

using Involver.Common;
using Involver.Extensions;

using Microsoft.EntityFrameworkCore;

namespace Involver.Services.NotificationSetterService
{
    /// <inheritdoc cref="INotificationSetter"/>
    public class NotificationSetter(ApplicationDbContext context) : INotificationSetter
    {
        /// <summary>
        /// 通知摘要從內容截取的字數（不含結尾的「...」）
        /// </summary>
        private const int ExcerptLength = 60;

        /// <inheritdoc />
        public async Task ForCommentAsync(Comment comment, CancellationToken cancellationToken = default)
        {
            var target = await CommentTargets.ResolveAsync(context, comment, cancellationToken);

            // 作品不存在，或作者評論自己的作品時不通知
            if (target == null || string.IsNullOrEmpty(target.OwnerId) || target.OwnerId == comment.ProfileID)
            {
                return;
            }

            context.Notifications.Add(Create(
                target.OwnerId,
                NotificationType.Comment,
                $"評論了你的{target.KindLabel}《{Encode(target.Title)}》",
                ToExcerpt(comment.Content),
                target.CommentUrl(comment.CommentID),
                comment.ProfileID));

            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task ForReplyAsync(Message message, CancellationToken cancellationToken = default)
        {
            var comment = await context.Comments
                .Where(c => c.CommentID == message.CommentID)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (comment == null)
            {
                return;
            }

            var target = await CommentTargets.ResolveAsync(context, comment, cancellationToken);

            if (target == null)
            {
                return;
            }

            var actorId = message.ProfileID;
            var url = target.RepliesUrl(comment.CommentID);
            var excerpt = ToExcerpt(message.Content);
            var workTitle = $"{target.KindLabel}《{Encode(target.Title)}》";

            // 評論者：有人在你的評論底下留言
            if (comment.ProfileID != actorId)
            {
                context.Notifications.Add(Create(
                    comment.ProfileID,
                    NotificationType.Reply,
                    $"在你對{workTitle}的評論底下留言",
                    excerpt,
                    url,
                    actorId));
            }

            // 之前在同一則評論留言過的人：讓對話可以接續下去
            var participantIds = await context.Messages
                .Where(m => m.CommentID == comment.CommentID
                    && m.MessageID != message.MessageID
                    && m.ProfileID != null
                    && m.ProfileID != actorId
                    && m.ProfileID != comment.ProfileID)
                .Select(m => m.ProfileID)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var participantId in participantIds)
            {
                context.Notifications.Add(Create(
                    participantId,
                    NotificationType.Reply,
                    $"也在{workTitle}的一則評論底下留言",
                    excerpt,
                    url,
                    actorId));
            }

            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task ForCommentAgreedAsync(Comment comment, string agreerId, List<Toast> toasts, CancellationToken cancellationToken = default)
        {
            var target = await CommentTargets.ResolveAsync(context, comment, cancellationToken);

            if (target != null)
            {
                await AddAgreedOnceAsync(Create(
                    comment.ProfileID,
                    NotificationType.CommentAgreed,
                    $"讚了你對{target.KindLabel}《{Encode(target.Title)}》的評論",
                    ToExcerpt(comment.Content),
                    target.CommentUrl(comment.CommentID),
                    agreerId), cancellationToken);
            }

            AddAchievements(comment.ProfileID, toasts);

            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task ForReplyAgreedAsync(Message message, string agreerId, List<Toast> toasts, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(message.ProfileID))
            {
                return;
            }

            var target = await CommentTargets.ResolveAsync(context, message.CommentID, cancellationToken);

            if (target != null)
            {
                await AddAgreedOnceAsync(Create(
                    message.ProfileID,
                    NotificationType.ReplyAgreed,
                    $"讚了你在{target.KindLabel}《{Encode(target.Title)}》的留言",
                    ToExcerpt(message.Content),
                    target.RepliesUrl(message.CommentID),
                    agreerId), cancellationToken);
            }

            AddAchievements(message.ProfileID, toasts);

            await context.SaveChangesAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task ForFeedbackAcceptAsync(int feedbackId, string feedbackTitle, string userId, List<Toast> toasts, CancellationToken cancellationToken = default)
        {
            context.Notifications.Add(Create(
                userId,
                NotificationType.FeedbackAccepted,
                $"你的意見回饋《{Encode(feedbackTitle)}》已被接受，謝謝你的建議！",
                null,
                $"/{Parameters.Feedbacks}/Details?id={feedbackId}",
                null));

            AddAchievements(userId, toasts);

            await context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// 按讚通知：同一個對象還有未讀的同種通知時不再新增，避免連續被讚時洗版
        /// </summary>
        /// <param name="notification">要新增的通知</param>
        /// <param name="cancellationToken">取消權杖</param>
        private async Task AddAgreedOnceAsync(Notification notification, CancellationToken cancellationToken)
        {
            var exists = await context.Notifications
                .AnyAsync(n => n.ProfileID == notification.ProfileID
                    && !n.IsRead
                    && n.Type == notification.Type
                    && n.Url == notification.Url
                    && n.Excerpt == notification.Excerpt, cancellationToken);

            if (!exists)
            {
                context.Notifications.Add(notification);
            }
        }

        /// <summary>
        /// 每個新成就各建立一則通知，連到成就頁
        /// </summary>
        /// <param name="userId">獲得成就的人</param>
        /// <param name="toasts">新成就</param>
        private void AddAchievements(string userId, List<Toast> toasts)
        {
            foreach (var toast in toasts ?? [])
            {
                var tier = toast.Award switch
                {
                    Parameters.BronzeBadgeAward => "銅牌",
                    Parameters.SilverBadgeAward => "銀牌",
                    _ => "金牌",
                };

                context.Notifications.Add(Create(
                    userId,
                    NotificationType.Achievement,
                    $"你獲得了{tier}成就「{Encode(toast.Header)}」",
                    Truncate(toast.Body),
                    $"/Identity/Profile/Achievements?id={WebUtility.UrlEncode(userId)}",
                    null));
            }
        }

        /// <summary>
        /// 建立一筆未讀通知
        /// </summary>
        private static Notification Create(string profileId, NotificationType type, string title, string excerpt, string url, string actorId) => new()
        {
            ProfileID = profileId,
            Type = type,
            Title = title,
            Excerpt = string.IsNullOrEmpty(excerpt) ? null : excerpt,
            Url = url,
            ActorID = actorId,
            CreatedDate = DateTime.Now,
            IsRead = false,
        };

        /// <summary>
        /// 標題會以 HTML 顯示（為了相容舊資料），使用者輸入的文字要先編碼
        /// </summary>
        private static string Encode(string text) => WebUtility.HtmlEncode(text ?? string.Empty);

        /// <summary>
        /// 把 HTML 內容轉成通知用的純文字摘要
        /// </summary>
        private static string ToExcerpt(string html) => Truncate((html ?? string.Empty).ToPlainTextPreview(ExcerptLength));

        /// <summary>
        /// 確保摘要不超過資料庫欄位長度
        /// </summary>
        private static string Truncate(string text)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= Parameters.NotificationExcerptLength)
            {
                return text;
            }

            return string.Concat(text.AsSpan(0, Parameters.NotificationExcerptLength - 3), "...");
        }
    }
}
