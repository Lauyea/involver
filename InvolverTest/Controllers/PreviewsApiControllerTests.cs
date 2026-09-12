using System;
using System.Text.Json;
using System.Threading.Tasks;
using DataAccess.Data;
using DataAccess.Models;
using Involver.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace InvolverTest.Controllers
{
    [TestFixture]
    public class PreviewsApiControllerTests
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

        private Preview CreateSamplePreview(
            string token = "test-token-123",
            string readPassword = "readSecret123",
            string revokePassword = "revokeSecret456",
            DateTime? expiresAt = null,
            DateTime? revokedAt = null)
        {
            var preview = new Preview
            {
                Token = token,
                Title = "測試試閱標題",
                ContentHtml = "<p>這是測試內容</p>",
                PasswordHash = string.Empty,
                RevokePasswordHash = string.Empty,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(7),
                RevokedAt = revokedAt
            };

            preview.PasswordHash = _passwordHasher.HashPassword(preview, readPassword);
            preview.RevokePasswordHash = _passwordHasher.HashPassword(preview, revokePassword);

            return preview;
        }

        [Test]
        public async Task GetContentAsync_TokenNotFound_ReturnsNotFound()
        {
            using var context = CreateContext();
            var controller = new PreviewsApiController(context, _passwordHasher);

            var result = await controller.GetContentAsync("non-existent", new PreviewsApiController.PreviewContentRequest
            {
                Password = "any"
            });

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task GetContentAsync_PreviewRevoked_Returns410Gone()
        {
            using var context = CreateContext();
            var preview = CreateSamplePreview(revokedAt: DateTime.UtcNow.AddMinutes(-5));
            context.Previews.Add(preview);
            await context.SaveChangesAsync();

            var controller = new PreviewsApiController(context, _passwordHasher);

            var result = await controller.GetContentAsync(preview.Token, new PreviewsApiController.PreviewContentRequest
            {
                Password = "readSecret123"
            });

            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result;
            Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status410Gone));
        }

        [Test]
        public async Task GetContentAsync_PreviewExpired_Returns410Gone()
        {
            using var context = CreateContext();
            var preview = CreateSamplePreview(expiresAt: DateTime.UtcNow.AddHours(-2));
            context.Previews.Add(preview);
            await context.SaveChangesAsync();

            var controller = new PreviewsApiController(context, _passwordHasher);

            var result = await controller.GetContentAsync(preview.Token, new PreviewsApiController.PreviewContentRequest
            {
                Password = "readSecret123"
            });

            Assert.That(result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result;
            Assert.That(objectResult.StatusCode, Is.EqualTo(StatusCodes.Status410Gone));
        }

        [Test]
        public async Task GetContentAsync_WrongPassword_ReturnsUnauthorized()
        {
            using var context = CreateContext();
            var preview = CreateSamplePreview();
            context.Previews.Add(preview);
            await context.SaveChangesAsync();

            var controller = new PreviewsApiController(context, _passwordHasher);

            var result = await controller.GetContentAsync(preview.Token, new PreviewsApiController.PreviewContentRequest
            {
                Password = "wrongPassword"
            });

            Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        }

        [Test]
        public async Task GetContentAsync_CorrectPassword_ReturnsOkWithContent()
        {
            using var context = CreateContext();
            var preview = CreateSamplePreview();
            context.Previews.Add(preview);
            await context.SaveChangesAsync();

            var controller = new PreviewsApiController(context, _passwordHasher);

            var result = await controller.GetContentAsync(preview.Token, new PreviewsApiController.PreviewContentRequest
            {
                Password = "readSecret123"
            });

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.That(doc.RootElement.GetProperty("title").GetString(), Is.EqualTo("測試試閱標題"));
            Assert.That(doc.RootElement.GetProperty("contentHtml").GetString(), Is.EqualTo("<p>這是測試內容</p>"));
        }

        [Test]
        public async Task RevokeAsync_TokenNotFound_ReturnsNotFound()
        {
            using var context = CreateContext();
            var controller = new PreviewsApiController(context, _passwordHasher);

            var result = await controller.RevokeAsync("non-existent", new PreviewsApiController.PreviewRevokeRequest
            {
                RevokePassword = "any"
            });

            Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        }

        [Test]
        public async Task RevokeAsync_AlreadyRevoked_ReturnsBadRequest()
        {
            using var context = CreateContext();
            var preview = CreateSamplePreview(revokedAt: DateTime.UtcNow.AddDays(-1));
            context.Previews.Add(preview);
            await context.SaveChangesAsync();

            var controller = new PreviewsApiController(context, _passwordHasher);

            var result = await controller.RevokeAsync(preview.Token, new PreviewsApiController.PreviewRevokeRequest
            {
                RevokePassword = "revokeSecret456"
            });

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task RevokeAsync_AlreadyExpired_ReturnsBadRequest()
        {
            using var context = CreateContext();
            var preview = CreateSamplePreview(expiresAt: DateTime.UtcNow.AddDays(-1));
            context.Previews.Add(preview);
            await context.SaveChangesAsync();

            var controller = new PreviewsApiController(context, _passwordHasher);

            var result = await controller.RevokeAsync(preview.Token, new PreviewsApiController.PreviewRevokeRequest
            {
                RevokePassword = "revokeSecret456"
            });

            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task RevokeAsync_WrongRevokePassword_ReturnsUnauthorized()
        {
            using var context = CreateContext();
            var preview = CreateSamplePreview();
            context.Previews.Add(preview);
            await context.SaveChangesAsync();

            var controller = new PreviewsApiController(context, _passwordHasher);

            var result = await controller.RevokeAsync(preview.Token, new PreviewsApiController.PreviewRevokeRequest
            {
                RevokePassword = "wrongRevokePassword"
            });

            Assert.That(result, Is.InstanceOf<UnauthorizedObjectResult>());
        }

        [Test]
        public async Task RevokeAsync_CorrectRevokePassword_SetsRevokedAtAndReturnsOk()
        {
            using var context = CreateContext();
            var preview = CreateSamplePreview();
            context.Previews.Add(preview);
            await context.SaveChangesAsync();

            var controller = new PreviewsApiController(context, _passwordHasher);

            var result = await controller.RevokeAsync(preview.Token, new PreviewsApiController.PreviewRevokeRequest
            {
                RevokePassword = "revokeSecret456"
            });

            Assert.That(result, Is.InstanceOf<OkObjectResult>());

            using var verifyContext = CreateContext();
            var updated = await verifyContext.Previews.FirstOrDefaultAsync(p => p.Token == preview.Token);
            Assert.That(updated, Is.Not.Null);
            Assert.That(updated!.RevokedAt, Is.Not.Null);
        }

        [Test]
        public async Task GetStatusAsync_ReturnsCorrectStatus()
        {
            using var context = CreateContext();
            var preview = CreateSamplePreview();
            context.Previews.Add(preview);
            await context.SaveChangesAsync();

            var controller = new PreviewsApiController(context, _passwordHasher);

            var result = await controller.GetStatusAsync(preview.Token);

            Assert.That(result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result;
            var json = JsonSerializer.Serialize(okResult.Value);
            using var doc = JsonDocument.Parse(json);
            Assert.That(doc.RootElement.GetProperty("isRevoked").GetBoolean(), Is.False);
            Assert.That(doc.RootElement.GetProperty("isExpired").GetBoolean(), Is.False);
        }
    }
}
