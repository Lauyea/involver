using System;
using System.Threading.Tasks;
using DataAccess.Data;
using DataAccess.Models;
using Involver.Pages.Functions;
using Involver.Pages.Preview;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Routing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InvolverTest.Pages
{
    [TestFixture]
    public class PreviewPagesTests
    {
        private SqliteConnection _connection = null!;
        private DbContextOptions<ApplicationDbContext> _options = null!;
        private PasswordHasher<Preview> _passwordHasher = null!;

        [SetUp]
        public void SetUp()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options;

            using var context = new ApplicationDbContext(_options);
            context.Database.EnsureCreated();

            _passwordHasher = new PasswordHasher<Preview>();
        }

        [TearDown]
        public void TearDown()
        {
            _connection?.Dispose();
        }

        private ApplicationDbContext CreateContext() => new ApplicationDbContext(_options);

        [Test]
        public async Task PreviewModel_Post_SamePasswords_AddsModelError()
        {
            using var context = CreateContext();
            var pageModel = new PreviewModel(context, _passwordHasher)
            {
                Input = new PreviewModel.InputModel
                {
                    Title = "測試小說",
                    ContentHtml = "<p>小說內容</p>",
                    ReadPassword = "samePassword123",
                    RevokePassword = "samePassword123",
                    ExpiresAt = DateTime.Now.AddDays(7)
                }
            };

            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
            pageModel.PageContext = new PageContext(actionContext);

            var result = await pageModel.OnPostAsync();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(pageModel.ModelState.ContainsKey("Input.RevokePassword"), Is.True);
        }

        [Test]
        public async Task PreviewModel_Post_PastExpiry_AddsModelError()
        {
            using var context = CreateContext();
            var pageModel = new PreviewModel(context, _passwordHasher)
            {
                Input = new PreviewModel.InputModel
                {
                    Title = "測試小說",
                    ContentHtml = "<p>小說內容</p>",
                    ReadPassword = "readPassword123",
                    RevokePassword = "revokePassword456",
                    ExpiresAt = DateTime.Now.AddDays(-1)
                }
            };

            var httpContext = new DefaultHttpContext();
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
            pageModel.PageContext = new PageContext(actionContext);

            var result = await pageModel.OnPostAsync();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(pageModel.ModelState.ContainsKey("Input.ExpiresAt"), Is.True);
        }

        [Test]
        public async Task PreviewModel_Post_ValidInput_CreatesPreviewWithSecureTokenAndHashedPasswords()
        {
            using var context = CreateContext();
            var pageModel = new PreviewModel(context, _passwordHasher)
            {
                Input = new PreviewModel.InputModel
                {
                    Title = "測試小說",
                    ContentHtml = "<p>小說內容</p>",
                    ReadPassword = "readPassword123",
                    RevokePassword = "revokePassword456",
                    ExpiresAt = DateTime.Now.AddDays(7)
                }
            };

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Scheme = "https";
            httpContext.Request.Host = new HostString("involver.tw");
            var modelState = new ModelStateDictionary();
            var actionContext = new ActionContext(httpContext, new RouteData(), new PageActionDescriptor(), modelState);
            pageModel.PageContext = new PageContext(actionContext);

            var result = await pageModel.OnPostAsync();

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(pageModel.IsCreated, Is.True);
            Assert.That(pageModel.PreviewUrl, Does.StartWith("https://involver.tw/Preview/"));

            // Verify saved in DB
            using var verifyContext = CreateContext();
            var saved = await verifyContext.Previews.FirstOrDefaultAsync();
            Assert.That(saved, Is.Not.Null);
            Assert.That(saved!.Token, Is.Not.Empty);
            Assert.That(saved.Title, Is.EqualTo("測試小說"));
            Assert.That(saved.PasswordHash, Is.Not.EqualTo("readPassword123")); // Must not be cleartext
            Assert.That(saved.RevokePasswordHash, Is.Not.EqualTo("revokePassword456")); // Must not be cleartext

            // Verify hashes are valid
            var readVerify = _passwordHasher.VerifyHashedPassword(saved, saved.PasswordHash, "readPassword123");
            Assert.That(readVerify, Is.EqualTo(PasswordVerificationResult.Success));

            var revokeVerify = _passwordHasher.VerifyHashedPassword(saved, saved.RevokePasswordHash, "revokePassword456");
            Assert.That(revokeVerify, Is.EqualTo(PasswordVerificationResult.Success));
        }

        [Test]
        public async Task PreviewIndexModel_Get_EmptyToken_RedirectsToFunctions()
        {
            using var context = CreateContext();
            var pageModel = new Involver.Pages.Preview.IndexModel(context);

            var result = await pageModel.OnGetAsync(string.Empty);

            Assert.That(result, Is.InstanceOf<RedirectToPageResult>());
            var redirectResult = (RedirectToPageResult)result;
            Assert.That(redirectResult.PageName, Is.EqualTo("/Functions/Preview"));
        }

        [Test]
        public async Task PreviewIndexModel_Get_TokenNotFound_ReturnsNotFound()
        {
            using var context = CreateContext();
            var pageModel = new Involver.Pages.Preview.IndexModel(context);

            var result = await pageModel.OnGetAsync("non-existent-token");

            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task PreviewIndexModel_Get_ValidToken_SetsPropertiesCorrectly()
        {
            using var context = CreateContext();
            var preview = new Preview
            {
                Token = "valid-token-789",
                Title = "測試標題",
                ContentHtml = "<p>內文</p>",
                PasswordHash = "hash1",
                RevokePasswordHash = "hash2",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(3),
                RevokedAt = null
            };
            context.Previews.Add(preview);
            await context.SaveChangesAsync();

            var pageModel = new Involver.Pages.Preview.IndexModel(context);
            var result = await pageModel.OnGetAsync(preview.Token);

            Assert.That(result, Is.InstanceOf<PageResult>());
            Assert.That(pageModel.Token, Is.EqualTo("valid-token-789"));
            Assert.That(pageModel.IsExpired, Is.False);
            Assert.That(pageModel.IsRevoked, Is.False);
        }
    }
}
