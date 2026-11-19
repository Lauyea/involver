using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

using Involver.Authorization.Admin;
using Involver.Authorization.Novel;
using Involver.Authorization.Payment;
using Involver.Authorization.Profile;
using Involver.Authorization.ProfitSharing;
using Involver.Views.Shared.Components.AdminSidebar;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

using NUnit.Framework;

namespace InvolverTest.Authorization
{
    /// <summary>
    /// Issue #237：管理員後台的權限 policy
    /// </summary>
    [TestFixture]
    public class AdminPoliciesTests
    {
        private ServiceProvider _provider = null!;
        private IAuthorizationService _authorizationService = null!;

        [SetUp]
        public void SetUp()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddAuthorization(AdminPolicies.Configure);
            _provider = services.BuildServiceProvider();
            _authorizationService = _provider.GetRequiredService<IAuthorizationService>();
        }

        [TearDown]
        public void TearDown()
        {
            _provider?.Dispose();
        }

        private static ClaimsPrincipal UserInRoles(params string[] roles) =>
            new(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), "test"));

        private async Task<bool> IsAuthorizedAsync(ClaimsPrincipal user, string policy) =>
            (await _authorizationService.AuthorizeAsync(user, policy)).Succeeded;

        [Test]
        public async Task Access_WhenAnonymous_IsDenied()
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());

            Assert.That(await IsAuthorizedAsync(anonymous, AdminPolicies.Access), Is.False);
        }

        [Test]
        public async Task Access_WhenUserHasNoManagementRole_IsDenied()
        {
            var user = UserInRoles("SomeOtherRole");

            Assert.That(await IsAuthorizedAsync(user, AdminPolicies.Access), Is.False);
        }

        [Test]
        public async Task Access_WithAnyManagementRole_IsAllowed()
        {
            foreach (var role in AdminPolicies.AllRoles)
            {
                Assert.That(await IsAuthorizedAsync(UserInRoles(role), AdminPolicies.Access), Is.True, role);
            }
        }

        [Test]
        public async Task Payments_WithNovelManagerRole_IsDenied()
        {
            var user = UserInRoles(Novels.NovelManagersRole);

            Assert.Multiple(async () =>
            {
                Assert.That(await IsAuthorizedAsync(user, AdminPolicies.Payments), Is.False);
                Assert.That(await IsAuthorizedAsync(user, AdminPolicies.Tags), Is.True);
                Assert.That(await IsAuthorizedAsync(user, AdminPolicies.Content), Is.True);
            });
        }

        [Test]
        public async Task Payments_WithPaymentManagerRole_IsAllowedButUsersIsDenied()
        {
            var user = UserInRoles(Payments.PaymentManagersRole);

            Assert.Multiple(async () =>
            {
                Assert.That(await IsAuthorizedAsync(user, AdminPolicies.Payments), Is.True);
                Assert.That(await IsAuthorizedAsync(user, AdminPolicies.Users), Is.False);
                Assert.That(await IsAuthorizedAsync(user, AdminPolicies.Payouts), Is.False);
            });
        }

        [Test]
        public async Task Payouts_WithProfitSharingManagerRole_IsAllowed()
        {
            var user = UserInRoles(ProfitSharings.ProfitSharingManagersRole);

            Assert.That(await IsAuthorizedAsync(user, AdminPolicies.Payouts), Is.True);
        }

        [Test]
        public async Task RoleAssignment_WithProfileManagerRole_IsDenied()
        {
            var user = UserInRoles(Profiles.ProfileManagersRole);

            Assert.Multiple(async () =>
            {
                Assert.That(await IsAuthorizedAsync(user, AdminPolicies.Users), Is.True);
                Assert.That(await IsAuthorizedAsync(user, AdminPolicies.RoleAssignment), Is.False);
            });
        }

        [Test]
        public async Task RoleAssignment_WithProfileAdministratorRole_IsAllowed()
        {
            var user = UserInRoles(Profiles.ProfileAdministratorsRole);

            Assert.That(await IsAuthorizedAsync(user, AdminPolicies.RoleAssignment), Is.True);
        }

        [Test]
        public async Task Administrators_WithManagerRoleOnly_IsDenied()
        {
            var user = UserInRoles(Novels.NovelManagersRole, Payments.PaymentManagersRole);

            Assert.That(await IsAuthorizedAsync(user, AdminPolicies.Administrators), Is.False);
        }

        [Test]
        public async Task Administrators_WithAnyAdministratorRole_IsAllowed()
        {
            foreach (var role in AdminPolicies.AdministratorRoles)
            {
                Assert.That(await IsAuthorizedAsync(UserInRoles(role), AdminPolicies.Administrators), Is.True, role);
            }
        }

        [Test]
        public void AdministratorRoles_WithAllRoles_ContainsOnlyAdministratorRoles()
        {
            Assert.That(AdminPolicies.AdministratorRoles, Has.Length.EqualTo(AdminPolicies.AllRoles.Length / 2));
            Assert.That(AdminPolicies.AdministratorRoles, Has.All.EndsWith("Administrators"));
        }

        [Test]
        public void Items_EverySidebarItem_UsesRegisteredPolicy()
        {
            var options = new AuthorizationOptions();
            AdminPolicies.Configure(options);

            foreach (var item in AdminSidebarViewComponent.Items)
            {
                Assert.That(options.GetPolicy(item.Policy), Is.Not.Null, item.Page);
            }
        }

        [TestCase("/Users/Index", "/Users/Details", ExpectedResult = true)]
        [TestCase("/Payments/Index", "/Payments/Details", ExpectedResult = true)]
        [TestCase("/Tags/Index", "/Tags/Aliases", ExpectedResult = false)]
        [TestCase("/Tags/Aliases", "/Tags/Aliases", ExpectedResult = true)]
        [TestCase("/People/Index", "/People/AuditLog", ExpectedResult = false)]
        [TestCase("/Index", "/Users/Index", ExpectedResult = false)]
        public bool IsActive_WithCurrentPage_MatchesItemOrItsDetailPages(string itemPage, string currentPage)
        {
            return AdminSidebarViewComponent.IsActive(itemPage, currentPage);
        }
    }
}
