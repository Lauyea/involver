using System;
using System.Threading.Tasks;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.AchievementModel;
using DataAccess.Models.ArticleModel;
using Involver.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace InvolverTest.Services
{
    [TestFixture]
    public class UserAccountServiceTests
    {
        private const string UserId = "3c83f9c4-e435-4f92-afd5-926cf8ee0ffa";
        private const string OtherId = "other";

        private SqliteConnection _connection = null!;
        private DbContextOptions<ApplicationDbContext> _options = null!;

        [SetUp]
        public async Task SetUp()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = CreateContext();
            context.Database.EnsureCreated();

            var userManager = CreateUserManager(context);
            await userManager.CreateAsync(new InvolverUser { Id = UserId, UserName = "alice", Email = "alice@example.com", PhoneNumber = "0912345678" }, "Passw0rd!");
            await userManager.CreateAsync(new InvolverUser { Id = OtherId, UserName = "bob", Email = "bob@example.com" });
            await userManager.AddLoginAsync((await userManager.FindByIdAsync(UserId))!, new UserLoginInfo("Google", "google-key", "Google"));

            context.Profiles.Add(new Profile { ProfileID = UserId, UserName = "alice", SeqNo = 1, Introduction = "這是 alice 的自我介紹", ImageUrl = "https://example.com/a.png", CanChangeUserName = true });
            context.Profiles.Add(new Profile { ProfileID = OtherId, UserName = "bob", SeqNo = 2 });
            context.Articles.Add(new Article { ArticleID = 1, Title = "保留的文章", Content = "內容", ProfileID = UserId });
            context.Notifications.Add(new Notification { ProfileID = UserId, Title = "通知" });
            await context.SaveChangesAsync();
        }

        [TearDown]
        public void TearDown()
        {
            _connection?.Dispose();
        }

        private ApplicationDbContext CreateContext() => new TestDbContext(_options);

        /// <summary>
        /// 調整只有 SQL Server 支援的設定，讓模型可以在 SQLite 建立
        /// </summary>
        private class TestDbContext(DbContextOptions<ApplicationDbContext> options) : ApplicationDbContext(options)
        {
            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.Entity<Profile>().Property(p => p.SeqNo).ValueGeneratedNever();
                modelBuilder.Entity<ProfileAchievement>().Property(p => p.SeqNo).ValueGeneratedNever();
                modelBuilder.Entity<ProfileAchievement>().HasIndex(p => p.SeqNo).IsUnique(false);
                modelBuilder.Entity<Article>().Property(a => a.CreateTime).HasDefaultValueSql("CURRENT_TIMESTAMP");
            }
        }

        /// <summary>
        /// 建立與 Program.cs 相同用戶名與 Email 規則的 UserManager
        /// </summary>
        private static UserManager<InvolverUser> CreateUserManager(ApplicationDbContext context)
        {
            var options = new IdentityOptions();
            options.User.RequireUniqueEmail = true;
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";

            return new UserManager<InvolverUser>(
                new UserStore<InvolverUser>(context),
                Options.Create(options),
                new PasswordHasher<InvolverUser>(),
                [new UserValidator<InvolverUser>()],
                [new PasswordValidator<InvolverUser>()],
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                null!,
                NullLogger<UserManager<InvolverUser>>.Instance);
        }

        [Test]
        public async Task ChangeUserNameAsync_WithNewName_UpdatesBothTables()
        {
            using (var context = CreateContext())
            {
                var userManager = CreateUserManager(context);
                var service = new UserAccountService(context, userManager);

                var error = await service.ChangeUserNameAsync((await userManager.FindByIdAsync(UserId))!, "alice2");

                Assert.That(error, Is.Null);
            }

            using (var context = CreateContext())
            {
                var user = await context.Users.AsNoTracking().SingleAsync(u => u.Id == UserId);
                var profile = await context.Profiles.AsNoTracking().SingleAsync(p => p.ProfileID == UserId);
                Assert.That(user.UserName, Is.EqualTo("alice2"));
                Assert.That(profile.UserName, Is.EqualTo("alice2"));
                Assert.That(profile.CanChangeUserName, Is.False);
            }
        }

        [Test]
        public async Task ChangeUserNameAsync_WhenNameTaken_ReturnsErrorAndChangesNothing()
        {
            using (var context = CreateContext())
            {
                var userManager = CreateUserManager(context);
                var service = new UserAccountService(context, userManager);

                var error = await service.ChangeUserNameAsync((await userManager.FindByIdAsync(UserId))!, "bob");

                Assert.That(error, Is.Not.Null);
            }

            using (var context = CreateContext())
            {
                Assert.That((await context.Users.AsNoTracking().SingleAsync(u => u.Id == UserId)).UserName, Is.EqualTo("alice"));
                Assert.That((await context.Profiles.AsNoTracking().SingleAsync(p => p.ProfileID == UserId)).UserName, Is.EqualTo("alice"));
            }
        }

        [Test]
        public async Task AnonymizeAsync_WithExistingUser_ClearsPersonalDataAndKeepsContent()
        {
            using (var context = CreateContext())
            {
                var userManager = CreateUserManager(context);
                var service = new UserAccountService(context, userManager);

                await service.AnonymizeAsync((await userManager.FindByIdAsync(UserId))!);
            }

            using (var context = CreateContext())
            {
                var user = await context.Users.AsNoTracking().SingleAsync(u => u.Id == UserId);
                var profile = await context.Profiles.AsNoTracking().SingleAsync(p => p.ProfileID == UserId);
                var expectedName = UserAccountService.DeletedUserNamePrefix + "3c83f9c4e435";

                Assert.That(user.UserName, Is.EqualTo(expectedName));
                Assert.That(user.Email, Does.EndWith("@" + UserAccountService.DeletedEmailDomain));
                Assert.That(user.PasswordHash, Is.Null);
                Assert.That(user.PhoneNumber, Is.Null);
                Assert.That(user.LockoutEnd, Is.EqualTo(DateTimeOffset.MaxValue));
                Assert.That(await context.UserLogins.AnyAsync(l => l.UserId == UserId), Is.False);

                Assert.That(profile.UserName, Is.EqualTo(expectedName));
                Assert.That(profile.Introduction, Is.Null);
                Assert.That(profile.ImageUrl, Is.Null);
                Assert.That(await context.Notifications.AnyAsync(n => n.ProfileID == UserId), Is.False);

                // 作品保留
                Assert.That(await context.Articles.AnyAsync(a => a.ProfileID == UserId), Is.True);
                // 別人不受影響
                Assert.That((await context.Profiles.AsNoTracking().SingleAsync(p => p.ProfileID == OtherId)).UserName, Is.EqualTo("bob"));
            }
        }

        [Test]
        public void SaveChanges_WhenProfileHasNoUser_ThrowsDbUpdateException()
        {
            using var context = CreateContext();
            context.Profiles.Add(new Profile { ProfileID = "no-user", UserName = "nouser", SeqNo = 3 });

            Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        }
    }
}
