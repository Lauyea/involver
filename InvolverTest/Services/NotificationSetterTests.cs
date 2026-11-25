using System.Linq;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;

using Involver.Common;
using Involver.Services.NotificationSetterService;

using InvolverTest.Infrastructure;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

namespace InvolverTest.Services
{
    /// <summary>
    /// 通知的建立：對象、種類、觸發者，以及連到評論所在頁面的網址
    /// </summary>
    [TestFixture]
    public class NotificationSetterTests
    {
        private const string AuthorId = "author";
        private const string ReaderId = "reader";
        private const string Reader2Id = "reader2";
        private const string Reader3Id = "reader3";

        private SqliteTestDatabase _database = null!;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            using var context = _database.CreateContext();

            AddUser(context, AuthorId, 1);
            AddUser(context, ReaderId, 2);
            AddUser(context, Reader2Id, 3);
            AddUser(context, Reader3Id, 4);

            context.Novels.Add(new Novel { NovelID = 1, Title = "<b>小說</b>", Introduction = "介紹介紹", ProfileID = AuthorId });
            context.Episodes.Add(new Episode { EpisodeID = 1, Title = "第一章", Content = "內容", NovelID = 1, OwnerID = AuthorId });
            context.Articles.AddRange(
                new Article { ArticleID = 1, Title = "一般文章", Content = "內容", ProfileID = AuthorId, Type = ArticleType.General },
                new Article { ArticleID = 2, Title = "我的建議", Content = "內容", ProfileID = AuthorId, Type = ArticleType.Feedback },
                new Article { ArticleID = 3, Title = "公告", Content = "內容", ProfileID = AuthorId, Type = ArticleType.Announcement });
            context.Comments.AddRange(
                new Comment { CommentID = 10, Content = "<p>小說評論</p>", NovelID = 1, ProfileID = ReaderId },
                new Comment { CommentID = 11, Content = "<p>意見回饋的評論</p>", ArticleID = 2, ProfileID = ReaderId },
                new Comment { CommentID = 12, Content = "<p>作者自己的評論</p>", ArticleID = 1, ProfileID = AuthorId },
                new Comment { CommentID = 13, Content = "<p>章節評論</p>", EpisodeID = 1, ProfileID = ReaderId });
            context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _database.Dispose();
        }

        [Test]
        public async Task ForCommentAsync_OnFeedback_LinksToFeedbacksPage()
        {
            await using (var context = _database.CreateContext())
            {
                var comment = await context.Comments.SingleAsync(c => c.CommentID == 11);
                await new NotificationSetter(context).ForCommentAsync(comment);
            }

            await using var verify = _database.CreateContext();
            var notification = await verify.Notifications.SingleAsync();

            Assert.Multiple(() =>
            {
                Assert.That(notification.ProfileID, Is.EqualTo(AuthorId));
                Assert.That(notification.ActorID, Is.EqualTo(ReaderId));
                Assert.That(notification.Type, Is.EqualTo(NotificationType.Comment));
                Assert.That(notification.Url, Is.EqualTo("/Feedbacks/Details?id=2#comment-11"));
                Assert.That(notification.Title, Is.EqualTo("評論了你的意見回饋《我的建議》"));
                Assert.That(notification.Excerpt, Is.EqualTo("意見回饋的評論"));
            });
        }

        [Test]
        public async Task ForCommentAsync_WithHtmlInWorkTitle_EncodesTitle()
        {
            await using (var context = _database.CreateContext())
            {
                var comment = await context.Comments.SingleAsync(c => c.CommentID == 10);
                await new NotificationSetter(context).ForCommentAsync(comment);
            }

            await using var verify = _database.CreateContext();
            var notification = await verify.Notifications.SingleAsync();

            Assert.Multiple(() =>
            {
                Assert.That(notification.Title, Is.EqualTo("評論了你的創作《&lt;b&gt;小說&lt;/b&gt;》"));
                Assert.That(notification.Url, Is.EqualTo("/Novels/Details/1#comment-10"));
            });
        }

        [Test]
        public async Task ForCommentAsync_WhenAuthorCommentsOwnWork_DoesNotNotify()
        {
            await using (var context = _database.CreateContext())
            {
                var comment = await context.Comments.SingleAsync(c => c.CommentID == 12);
                await new NotificationSetter(context).ForCommentAsync(comment);
            }

            await using var verify = _database.CreateContext();
            Assert.That(await verify.Notifications.CountAsync(), Is.Zero);
        }

        [Test]
        public async Task ForReplyAsync_NotifiesCommenterAndEarlierRepliers_WithRepliesAnchor()
        {
            await using (var context = _database.CreateContext())
            {
                // reader2 先留言，reader3 留言兩次；這次由 reader3 再留言
                context.Messages.AddRange(
                    new Message { MessageID = 1, CommentID = 13, ProfileID = Reader2Id, Content = "先留言" },
                    new Message { MessageID = 2, CommentID = 13, ProfileID = Reader3Id, Content = "我也是" });
                var message = new Message { MessageID = 3, CommentID = 13, ProfileID = Reader3Id, Content = "<p>新的留言</p>" };
                context.Messages.Add(message);
                await context.SaveChangesAsync();

                await new NotificationSetter(context).ForReplyAsync(message);
            }

            await using var verify = _database.CreateContext();
            var notifications = await verify.Notifications.OrderBy(n => n.ProfileID).ToListAsync();

            Assert.Multiple(() =>
            {
                Assert.That(notifications.Select(n => n.ProfileID), Is.EqualTo(new[] { ReaderId, Reader2Id }));
                Assert.That(notifications.Select(n => n.Url), Is.All.EqualTo("/Episodes/Details?id=1#comment-13-messages"));
                Assert.That(notifications.Select(n => n.ActorID), Is.All.EqualTo(Reader3Id));
                Assert.That(notifications.Select(n => n.Type), Is.All.EqualTo(NotificationType.Reply));
                Assert.That(notifications[0].Title, Is.EqualTo("在你對章節《第一章》的評論底下留言"));
                Assert.That(notifications[1].Title, Is.EqualTo("也在章節《第一章》的一則評論底下留言"));
            });
        }

        [Test]
        public async Task ForReplyAgreedAsync_OnNovelComment_LinksToNovelPage()
        {
            // 舊版固定連到 /Episodes/Details，評論在小說頁時會 404
            await using (var context = _database.CreateContext())
            {
                var message = new Message { MessageID = 1, CommentID = 10, ProfileID = Reader2Id, Content = "留言" };
                context.Messages.Add(message);
                await context.SaveChangesAsync();

                await new NotificationSetter(context).ForReplyAgreedAsync(message, ReaderId, []);
            }

            await using var verify = _database.CreateContext();
            var notification = await verify.Notifications.SingleAsync();

            Assert.Multiple(() =>
            {
                Assert.That(notification.ProfileID, Is.EqualTo(Reader2Id));
                Assert.That(notification.Type, Is.EqualTo(NotificationType.ReplyAgreed));
                Assert.That(notification.Url, Is.EqualTo("/Novels/Details/1#comment-10-messages"));
            });
        }

        [Test]
        public async Task ForCommentAgreedAsync_WhenUnreadExists_SkipsDuplicateButKeepsAchievements()
        {
            await using (var context = _database.CreateContext())
            {
                var setter = new NotificationSetter(context);
                var comment = await context.Comments.SingleAsync(c => c.CommentID == 10);

                await setter.ForCommentAgreedAsync(comment, AuthorId, []);
                await setter.ForCommentAgreedAsync(comment, Reader2Id,
                    [new Toast { Header = "受歡迎", Body = "被讚 10 次", Award = Parameters.SilverBadgeAward }]);
            }

            await using var verify = _database.CreateContext();
            var notifications = await verify.Notifications.OrderBy(n => n.Id).ToListAsync();

            Assert.Multiple(() =>
            {
                Assert.That(notifications.Select(n => n.Type), Is.EqualTo(new[] { NotificationType.CommentAgreed, NotificationType.Achievement }));
                Assert.That(notifications[0].ActorID, Is.EqualTo(AuthorId));
                Assert.That(notifications[1].Title, Is.EqualTo("你獲得了銀牌成就「受歡迎」"));
                Assert.That(notifications[1].Url, Is.EqualTo("/Identity/Profile/Achievements?id=reader"));
            });
        }

        [Test]
        public async Task ForFeedbackAcceptAsync_LinksToFeedbackWithoutActor()
        {
            await using (var context = _database.CreateContext())
            {
                await new NotificationSetter(context).ForFeedbackAcceptAsync(2, "我的建議", AuthorId, []);
            }

            await using var verify = _database.CreateContext();
            var notification = await verify.Notifications.SingleAsync();

            Assert.Multiple(() =>
            {
                Assert.That(notification.Type, Is.EqualTo(NotificationType.FeedbackAccepted));
                Assert.That(notification.ActorID, Is.Null);
                Assert.That(notification.Url, Is.EqualTo("/Feedbacks/Details?id=2"));
            });
        }

        private static void AddUser(ApplicationDbContext context, string id, int seqNo)
        {
            context.Users.Add(new InvolverUser { Id = id, UserName = id, Email = $"{id}@example.com" });
            context.Profiles.Add(new Profile { ProfileID = id, UserName = id, SeqNo = seqNo });
        }
    }
}
