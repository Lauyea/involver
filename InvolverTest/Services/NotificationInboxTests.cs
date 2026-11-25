using System;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;

using Involver.Services.NotificationSetterService;

using InvolverTest.Infrastructure;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

namespace InvolverTest.Services
{
    /// <summary>
    /// 讀取通知：觸發者、已讀狀態，以及開啟通知時修正舊資料的連結
    /// </summary>
    [TestFixture]
    public class NotificationInboxTests
    {
        private const string OwnerId = "owner";
        private const string ActorId = "actor";

        private SqliteTestDatabase _database = null!;

        [SetUp]
        public void SetUp()
        {
            _database = new SqliteTestDatabase();
            using var context = _database.CreateContext();

            context.Users.AddRange(
                new InvolverUser { Id = OwnerId, UserName = OwnerId, Email = "owner@example.com" },
                new InvolverUser { Id = ActorId, UserName = ActorId, Email = "actor@example.com" });
            context.Profiles.AddRange(
                new Profile { ProfileID = OwnerId, UserName = "擁有者", SeqNo = 1 },
                new Profile { ProfileID = ActorId, UserName = "留言的人", SeqNo = 2, ImageUrl = "https://example.com/a.png" });
            context.Novels.Add(new Novel { NovelID = 5, Title = "小說", Introduction = "介紹介紹", ProfileID = OwnerId });
            context.Comments.Add(new Comment { CommentID = 50, Content = "評論", NovelID = 5, ProfileID = OwnerId });
            context.SaveChanges();
        }

        [TearDown]
        public void TearDown()
        {
            _database.Dispose();
        }

        [Test]
        public async Task OpenAsync_LegacyRemovedCommentPage_RedirectsToCommentReplies()
        {
            var id = await AddNotificationAsync("https://involver.tw/Comments/Details?id=50");

            await using var context = _database.CreateContext();
            var url = await new NotificationInbox(context).OpenAsync(OwnerId, id);

            Assert.That(url, Is.EqualTo("/Novels/Details/5#comment-50-messages"));
        }

        [Test]
        public async Task OpenAsync_LegacyWrongSection_RebuildsFromComment()
        {
            // 舊版留言被讚的通知固定連到 /Episodes，EpisodeID 是 null
            var id = await AddNotificationAsync("https://localhost:5001/Episodes/Details?id=#comment-50");

            await using var context = _database.CreateContext();
            var url = await new NotificationInbox(context).OpenAsync(OwnerId, id);

            Assert.That(url, Is.EqualTo("/Novels/Details/5#comment-50"));
        }

        [Test]
        public async Task OpenAsync_LegacyLinkToDeletedComment_ReturnsNull()
        {
            var id = await AddNotificationAsync("https://localhost:5001/Episodes/Details?id=#comment-999");

            await using var context = _database.CreateContext();
            Assert.That(await new NotificationInbox(context).OpenAsync(OwnerId, id), Is.Null);
        }

        [Test]
        public async Task OpenAsync_LegacyAbsoluteUrl_ReturnsLocalPathAndMarksRead()
        {
            var id = await AddNotificationAsync("https://localhost:5302/Feedbacks/Details?id=7");

            await using (var context = _database.CreateContext())
            {
                var url = await new NotificationInbox(context).OpenAsync(OwnerId, id);
                Assert.That(url, Is.EqualTo("/Feedbacks/Details?id=7"));
            }

            await using var verify = _database.CreateContext();
            Assert.That((await verify.Notifications.SingleAsync()).IsRead, Is.True);
        }

        [TestCase("https://localhost:5203/Novels/Details/?id=172", "/Novels/Details/172")]
        [TestCase("/Novels/Details?id=172#comment-9", "/Novels/Details/172#comment-9")]
        public async Task OpenAsync_NovelQueryString_RewritesToRoute(string storedUrl, string expected)
        {
            var id = await AddNotificationAsync(storedUrl);

            await using var context = _database.CreateContext();
            Assert.That(await new NotificationInbox(context).OpenAsync(OwnerId, id), Is.EqualTo(expected));
        }

        [TestCase("//evil.example.com/path")]
        [TestCase("/\\evil.example.com")]
        [TestCase("javascript:alert(1)")]
        public async Task OpenAsync_WithExternalUrl_ReturnsNull(string storedUrl)
        {
            var id = await AddNotificationAsync(storedUrl);

            await using var context = _database.CreateContext();
            Assert.That(await new NotificationInbox(context).OpenAsync(OwnerId, id), Is.Null);
        }

        [Test]
        public async Task OpenAsync_OtherUsersNotification_ReturnsNullAndStaysUnread()
        {
            var id = await AddNotificationAsync("/Novels/Details?id=5");

            await using (var context = _database.CreateContext())
            {
                Assert.That(await new NotificationInbox(context).OpenAsync(ActorId, id), Is.Null);
            }

            await using var verify = _database.CreateContext();
            Assert.That((await verify.Notifications.SingleAsync()).IsRead, Is.False);
        }

        [Test]
        public async Task GetItemsAsync_UnreadOnly_ReturnsActorAndFallbackName()
        {
            await AddNotificationAsync("/a", actorId: ActorId);
            await AddNotificationAsync("/b", actorId: "deleted-user");
            await AddNotificationAsync("/c", isRead: true);

            await using var context = _database.CreateContext();
            var items = await new NotificationInbox(context).GetItemsAsync(OwnerId, 0, 10, unreadOnly: true);

            Assert.Multiple(() =>
            {
                Assert.That(items, Has.Count.EqualTo(2));
                Assert.That(items.Select(i => i.ActorName), Is.EquivalentTo(new[] { "留言的人", "有人" }));
                Assert.That(items.Single(i => i.ActorName == "留言的人").ActorImageUrl, Is.EqualTo("https://example.com/a.png"));
                Assert.That(items.Select(i => i.IconClass), Is.All.EqualTo("fa-comment"));
            });
        }

        [Test]
        public async Task MarkAllReadAsync_OnlyMarksOwnNotifications()
        {
            await AddNotificationAsync("/a");
            await AddNotificationAsync("/b", profileId: ActorId);

            await using (var context = _database.CreateContext())
            {
                await new NotificationInbox(context).MarkAllReadAsync(OwnerId);
            }

            await using var verify = _database.CreateContext();
            var inbox = new NotificationInbox(verify);
            var ownerUnread = await inbox.CountUnreadAsync(OwnerId, 100);
            var otherUnread = await inbox.CountUnreadAsync(ActorId, 100);

            Assert.Multiple(() =>
            {
                Assert.That(ownerUnread, Is.Zero);
                Assert.That(otherUnread, Is.EqualTo(1));
            });
        }

        private async Task<int> AddNotificationAsync(string url, string? actorId = null, bool isRead = false, string profileId = OwnerId)
        {
            await using var context = _database.CreateContext();
            var notification = new Notification
            {
                ProfileID = profileId,
                Title = "評論了你的創作《小說》",
                Type = NotificationType.Comment,
                ActorID = actorId,
                Url = url,
                IsRead = isRead,
                CreatedDate = DateTime.Now,
            };
            context.Notifications.Add(notification);
            await context.SaveChangesAsync();
            return notification.Id;
        }
    }
}
