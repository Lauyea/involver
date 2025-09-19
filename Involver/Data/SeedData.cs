using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.AchievementModel;
using DataAccess.Models.StatisticalData;

using Involver.Authorization.Announcement;
using Involver.Authorization.Article;
using Involver.Authorization.Comment;
using Involver.Authorization.Feedback;
using Involver.Authorization.Message;
using Involver.Authorization.Novel;
using Involver.Authorization.Payment;
using Involver.Authorization.Profile;
using Involver.Authorization.ProfitSharing;
using Involver.Authorization.Voting;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Involver.Data
{
    public class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, string testUserPw)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                var UserName = "admin@involver.tw";
                var adminID = await EnsureUser(serviceProvider, testUserPw, UserName);
                await EnsureProfile(context, UserName, adminID);
                await EnsureRole(serviceProvider, adminID, Feedbacks.FeedbackAdministratorsRole);
                await EnsureRole(serviceProvider, adminID, Comments.CommentAdministratorsRole);
                await EnsureRole(serviceProvider, adminID, Announcements.AnnouncementAdministratorsRole);
                await EnsureRole(serviceProvider, adminID, Articles.ArticleAdministratorsRole);
                await EnsureRole(serviceProvider, adminID, Novels.NovelAdministratorsRole);
                await EnsureRole(serviceProvider, adminID, Messages.MessageAdministratorsRole);
                await EnsureRole(serviceProvider, adminID, Votings.VotingAdministratorsRole);
                await EnsureRole(serviceProvider, adminID, Profiles.ProfileAdministratorsRole);
                await EnsureRole(serviceProvider, adminID, Payments.PaymentAdministratorsRole);
                await EnsureRole(serviceProvider, adminID, ProfitSharings.ProfitSharingAdministratorsRole);

                UserName = "manager@involver.tw";
                // allowed user can create and edit contacts that they create
                var managerID = await EnsureUser(serviceProvider, testUserPw, UserName);
                await EnsureProfile(context, UserName, managerID);
                await EnsureRole(serviceProvider, managerID, Feedbacks.FeedbackManagersRole);
                await EnsureRole(serviceProvider, managerID, Comments.CommentManagersRole);
                await EnsureRole(serviceProvider, managerID, Announcements.AnnouncementManagersRole);
                await EnsureRole(serviceProvider, managerID, Articles.ArticleManagersRole);
                await EnsureRole(serviceProvider, managerID, Novels.NovelManagersRole);
                await EnsureRole(serviceProvider, managerID, Messages.MessageManagersRole);
                await EnsureRole(serviceProvider, managerID, Votings.VotingManagersRole);
                await EnsureRole(serviceProvider, managerID, Profiles.ProfileManagersRole);
                await EnsureRole(serviceProvider, managerID, Payments.PaymentManagersRole);
                await EnsureRole(serviceProvider, managerID, ProfitSharings.ProfitSharingManagersRole);
            }
        }

        private static async Task<string> EnsureUser(IServiceProvider serviceProvider,
                                                    string testUserPw, string UserName)
        {
            var userManager = serviceProvider.GetService<UserManager<InvolverUser>>();

            var user = await userManager.FindByNameAsync(UserName);
            if (user == null)
            {
                user = new InvolverUser
                {
                    UserName = UserName,
                    Email = UserName,
                    EmailConfirmed = true,
                    Prime = true
                };
                await userManager.CreateAsync(user, testUserPw);
            }

            if (user == null)
            {
                throw new Exception("The password is probably not strong enough!");
            }

            return user.Id;
        }

        private static async Task EnsureProfile(ApplicationDbContext context, string userName, string id)
        {
            var userID = await context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == id);
            if (userID == null)
            {
                Profile profile = new Profile
                {
                    ProfileID = id,
                    UserName = userName,
                    RealCoins = 300,
                    EnrollmentDate = DateTime.Now,
                    LastTimeLogin = DateTime.Now,
                    Professional = false,
                    Prime = true,
                    Banned = false,
                    Missions = new Missions() { ProfileID = id },
                    Achievements = new List<Achievement>()
                };
                context.Profiles.Add(profile);
                await context.SaveChangesAsync();
            }
        }

        private static async Task<IdentityResult> EnsureRole(IServiceProvider serviceProvider,
                                                                      string uid, string role)
        {
            IdentityResult IR = null;
            var roleManager = serviceProvider.GetService<RoleManager<IdentityRole>>();

            if (roleManager == null)
            {
                throw new Exception("roleManager null");
            }

            if (!await roleManager.RoleExistsAsync(role))
            {
                IR = await roleManager.CreateAsync(new IdentityRole(role));
            }

            var userManager = serviceProvider.GetService<UserManager<InvolverUser>>();

            var user = await userManager.FindByIdAsync(uid);

            if (user == null)
            {
                throw new Exception("The testUserPw password was probably not strong enough!");
            }

            IR = await userManager.AddToRoleAsync(user, role);

            return IR;
        }
    }
}