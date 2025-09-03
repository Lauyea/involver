using Involver.Common;
using DataAccess.Data;
using Involver.Extensions;
using DataAccess.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using DataAccess.Common;

namespace Involver.Services.NotificationSetterService
{
    public class NotificationSetter : INotificationSetter
    {
        private readonly ApplicationDbContext _context;

        public NotificationSetter(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task ForMessageAsync(int commentId, string userId, string message, string url)
        {
            var comment = await _context.Comments.Where(c => c.CommentID == commentId).FirstOrDefaultAsync().ConfigureAwait(false);

            // check comment is existed
            if(comment == null)
            {
                return;
            }

            var commenterId = comment.ProfileID;

            // if commenter is user, than do not set the notification
            if (commenterId == userId)
            {
                return;
            }

            string commentContent = comment.Content.StripHTML();

            if (commentContent.Length > Parameters.SmallContentLength)
            {
                commentContent = string.Concat(commentContent.AsSpan(0, Parameters.SmallContentLength), "...");
            }

            if (message.Length > Parameters.SmallContentLength)
            {
                message = string.Concat(message.AsSpan(0, Parameters.SmallContentLength), "...");
            }

            commentContent = commentContent.Replace("\r\n", "  ");

            message = message.Replace("\r\n", "  ");

            string title = $"有人在你的評論：「{commentContent}」留言給你。<br/>{message}";

            Notification notification = new()
            {
                CreatedDate = DateTime.Now,
                Title = title,
                IsRead = false,
                Url = url,
                ProfileID = commenterId
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
        }

        public async Task ForCommentAsync(string from, int fromId, string url, string userId, string comment)
        {
            string title = string.Empty;
            string userToBeNotifiy = string.Empty;

            switch (from)
            {
                case "Novels":
                    var novel = await _context.Novels.Where(n => n.NovelID == fromId).FirstOrDefaultAsync().ConfigureAwait(false);

                    // check exist
                    if (novel == null)
                    {
                        return;
                    }

                    // if commenter is user, than do not set the notification
                    if (novel.ProfileID == userId)
                    {
                        return;
                    }

                    comment = comment.StripHTML();

                    if (comment.Length > Parameters.SmallContentLength)
                    {
                        comment = string.Concat(comment.AsSpan(0, Parameters.SmallContentLength), "...");
                    }

                    title = $"有人在你的創作《{novel.Title}》留下評論：{comment}";

                    userToBeNotifiy = novel.ProfileID;
                    break;
                case "Episodes":
                    var episode = await _context.Episodes.Where(e => e.EpisodeID == fromId).FirstOrDefaultAsync().ConfigureAwait(false);

                    // check exist
                    if (episode == null)
                    {
                        return;
                    }

                    // if commenter is user, than do not set the notification
                    if (episode.OwnerID == userId)
                    {
                        return;
                    }

                    comment = comment.StripHTML();

                    if (comment.Length > Parameters.SmallContentLength)
                    {
                        comment = string.Concat(comment.AsSpan(0, Parameters.SmallContentLength), "...");
                    }

                    title = $"有人在你的章節《{episode.Title}》留下評論：{comment}";

                    userToBeNotifiy = episode.OwnerID;
                    break;
                case "Articles":
                    var article = await _context.Articles.Where(a => a.ArticleID == fromId).FirstOrDefaultAsync().ConfigureAwait(false);

                    // check exist
                    if (article == null)
                    {
                        return;
                    }

                    // if commenter is user, than do not set the notification
                    if (article.ProfileID == userId)
                    {
                        return;
                    }

                    comment = comment.StripHTML();

                    if (comment.Length > Parameters.SmallContentLength)
                    {
                        comment = string.Concat(comment.AsSpan(0, Parameters.SmallContentLength), "...");
                    }

                    title = $"有人在你的文章《{article.Title}》留下評論：{comment}";

                    userToBeNotifiy = article.ProfileID;
                    break;
                case "Feedbacks":
                    var feedback = await _context.Feedbacks.Where(f => f.FeedbackID == fromId).FirstOrDefaultAsync().ConfigureAwait(false);

                    // check exist
                    if (feedback == null)
                    {
                        return;
                    }

                    // if commenter is user, than do not set the notification
                    if (feedback.OwnerID == userId)
                    {
                        return;
                    }

                    comment = comment.StripHTML();

                    if (comment.Length > Parameters.SmallContentLength)
                    {
                        comment = string.Concat(comment.AsSpan(0, Parameters.SmallContentLength), "...");
                    }

                    title = $"有人在你的意見回饋《{feedback.Title}》留下評論：{comment}";

                    userToBeNotifiy = feedback.OwnerID;
                    break;
                default:
                    break;
            }

            Notification notification = new()
            {
                CreatedDate = DateTime.Now,
                Title = title,
                IsRead = false,
                Url = url,
                ProfileID = userToBeNotifiy
            };

            _context.Notifications.Add(notification);

            await _context.SaveChangesAsync();
        }

        public async Task ForFeedbackAcceptAsync(string feedbackTitle, string userId, string url, List<Toast> toasts)
        {
            string title = $"你的意見《{feedbackTitle}》被接受了。";

            if (toasts.Count == 0)
            {
                Notification notification = new()
                {
                    CreatedDate = DateTime.Now,
                    Title = title,
                    IsRead = false,
                    Url = url,
                    ProfileID = userId
                };

                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                return;
            }

            foreach(var toast in toasts)
            {
                string badgeColor = string.Empty;

                if (toast.Award == Parameters.BronzeBadgeAward)
                {
                    badgeColor = "bronze";
                }
                else if (toast.Award == Parameters.SilverBadgeAward)
                {
                    badgeColor = "silver";
                }
                else
                {
                    badgeColor = "gold";
                }

                title = $"你的意見《{feedbackTitle}》被接受了。<br/>獲得成就 <span class=\"dot mr-1 {badgeColor} \"></span> {toast.Header}: {toast.Body}";

                Notification notification = new()
                {
                    CreatedDate = DateTime.Now,
                    Title = title,
                    IsRead = false,
                    Url = url,
                    ProfileID = userId
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        public async Task ForMessageBeAgreedAsync(string messageContent, string commenterId, string url, List<Toast> toasts)
        {
            messageContent = messageContent.StripHTML();

            if (messageContent.Length > Parameters.SmallContentLength)
            {
                messageContent = messageContent[..Parameters.SmallContentLength] + "...";
            }

            string title = $"有人在你的留言「{messageContent}」留下了一個讚。";

            var existingNotification = await _context.Notifications
                .Where(n => n.Title == title && n.ProfileID == commenterId && n.IsRead == false)
                .FirstOrDefaultAsync();

            if (existingNotification != null)
            {
                return;
            }

            if (toasts.Count == 0)
            {
                Notification notification = new()
                {
                    CreatedDate = DateTime.Now,
                    Title = title,
                    IsRead = false,
                    Url = url,
                    ProfileID = commenterId
                };

                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                return;
            }

            foreach (var toast in toasts)
            {
                string badgeColor = string.Empty;

                if (toast.Award == Parameters.BronzeBadgeAward)
                {
                    badgeColor = "bronze";
                }
                else if (toast.Award == Parameters.SilverBadgeAward)
                {
                    badgeColor = "silver";
                }
                else
                {
                    badgeColor = "gold";
                }

                title = $"有人在你的留言「{messageContent}」留下了一個讚。<br/>獲得成就 <span class=\"dot mr-1 {badgeColor} \"></span> {toast.Header}: {toast.Body}";

                Notification notification = new()
                {
                    CreatedDate = DateTime.Now,
                    Title = title,
                    IsRead = false,
                    Url = url,
                    ProfileID = commenterId
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        public async Task ForCommentBeAgreedAsync(string commentContent, string commenterId, string url, List<Toast> toasts)
        {
            commentContent = commentContent.StripHTML();

            if (commentContent.Length > Parameters.SmallContentLength)
            {
                commentContent = commentContent[..Parameters.SmallContentLength] + "...";
            }

            string title = $"有人在你的評論「{commentContent}」留下了一個讚。";

            var existingNotification = await _context.Notifications
                .Where(n => n.Title == title && n.ProfileID == commenterId && n.IsRead == false)
                .FirstOrDefaultAsync();

            if (existingNotification != null)
            {
                return;
            }

            if (toasts.Count == 0)
            {
                Notification notification = new()
                {
                    CreatedDate = DateTime.Now,
                    Title = title,
                    IsRead = false,
                    Url = url,
                    ProfileID = commenterId
                };

                _context.Notifications.Add(notification);

                await _context.SaveChangesAsync();

                return;
            }

            foreach (var toast in toasts)
            {
                string badgeColor = string.Empty;

                if (toast.Award == Parameters.BronzeBadgeAward)
                {
                    badgeColor = "bronze";
                }
                else if (toast.Award == Parameters.SilverBadgeAward)
                {
                    badgeColor = "silver";
                }
                else
                {
                    badgeColor = "gold";
                }

                title = $"有人在你的評論「{commentContent}」留下了一個讚。<br/>獲得成就 <span class=\"dot mr-1 {badgeColor} \"></span> {toast.Header}: {toast.Body}";

                Notification notification = new()
                {
                    CreatedDate = DateTime.Now,
                    Title = title,
                    IsRead = false,
                    Url = url,
                    ProfileID = commenterId
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }
    }
}
