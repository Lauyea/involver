using Involver.Common;
using Involver.Data;
using Involver.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using System;

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

            string commentContent;

            if (comment.Content.Length < Parameters.SmallContentLength)
            {
                commentContent = comment.Content;
            }
            else
            {
                commentContent = string.Concat(comment.Content.AsSpan(0, Parameters.SmallContentLength), "...");
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

                    if (comment.Length > Parameters.SmallContentLength)
                    {
                        comment = string.Concat(comment.AsSpan(0, Parameters.SmallContentLength), "...");
                    }

                    title = $"有人在你的創作《{novel.Title}》留下評論：<br/>{comment}";

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

                    if (comment.Length > Parameters.SmallContentLength)
                    {
                        comment = string.Concat(comment.AsSpan(0, Parameters.SmallContentLength), "...");
                    }

                    title = $"有人在你的章節《{episode.Title}》留下評論：<br/>{comment}";

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

                    if (comment.Length > Parameters.SmallContentLength)
                    {
                        comment = string.Concat(comment.AsSpan(0, Parameters.SmallContentLength), "...");
                    }

                    title = $"有人在你的文章《{article.Title}》留下評論：<br/>{comment}";

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

                    if (comment.Length > Parameters.SmallContentLength)
                    {
                        comment = string.Concat(comment.AsSpan(0, Parameters.SmallContentLength), "...");
                    }

                    title = $"有人在你的意見回饋《{feedback.Title}》留下評論：<br/>{comment}";

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
    }
}
