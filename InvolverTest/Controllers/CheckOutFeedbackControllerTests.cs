using System.Collections.Generic;
using System.Threading.Tasks;
using DataAccess.Data;
using Involver.Controllers;
using Involver.Pages;
using Involver.Services;
using Involver.Services.ECPay;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NUnit.Framework;

namespace InvolverTest.Controllers
{
    [TestFixture]
    public class CheckOutFeedbackControllerTests
    {
        private SqliteConnection _connection = null!;
        private ApplicationDbContext _context = null!;

        [SetUp]
        public void SetUp()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _context = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(_connection)
                .Options);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();
            _connection?.Dispose();
        }

        private static IConfiguration CreateConfiguration(string? ecpayEnabled)
        {
            var settings = new Dictionary<string, string?>();
            if (ecpayEnabled != null)
            {
                settings["ECPay:Enabled"] = ecpayEnabled;
            }

            return new ConfigurationBuilder()
                .AddInMemoryCollection(settings)
                .Build();
        }

        [TestCase(null)]
        [TestCase("false")]
        public async Task PostCheckOutFeedbackAsync_WhenECPayDisabled_RejectsWithoutAddingCoins(string? ecpayEnabled)
        {
            var coinService = Substitute.For<ICoinService>();
            var controller = new CheckOutFeedbackController(
                _context,
                NullLogger<IndexModel>.Instance,
                CreateConfiguration(ecpayEnabled),
                coinService);

            var result = await controller.PostCheckOutFeedbackAsync(new Feedback());

            Assert.That(result.Content, Is.EqualTo("0|ECPay disabled"));
            await coinService.DidNotReceiveWithAnyArgs().AddRealCoinsAsync(default!, default, default!);
        }
    }
}
