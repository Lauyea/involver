using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Involver.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Involver.Authorization.Feedback;
using Involver.Authorization.Comment;
using Involver.Authorization.Announcement;
using Involver.Authorization.Article;
using Involver.Authorization.Novel;
using Involver.Authorization.Message;
using Involver.Authorization.Voting;
using Involver.Authorization.Profile;
using Microsoft.AspNetCore.Identity.UI.Services;
using WebPWrecover.Services;
using Involver.Authorization.Payment;
using Involver.Authorization.ProfitSharing;

namespace Involver
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpClient();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("InvolverConnection")));
            services.AddDefaultIdentity<InvolverUser>(
                options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();

            //SendGrid function
            services.AddTransient<IEmailSender, EmailSender>();
            services.Configure<AuthMessageSenderOptions>(Configuration);

            //services.AddRazorPages();
            services.AddMvc();

            //宣告 AJAX POST 使用的 Header 名稱
            services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");

            services.AddControllers(config =>
            {
                var policy = new AuthorizationPolicyBuilder()
                                 .RequireAuthenticatedUser()
                                 .Build();
                config.Filters.Add(new AuthorizeFilter(policy));
            });

            AddAuthorizationHandlers(services);

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings.
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;

                // Lockout settings.
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings.
                options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = true;
            });

            services.ConfigureApplicationCookie(options =>
            {
                // Cookie settings
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromDays(7);

                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.SlidingExpiration = true;
            });

            services.AddAuthentication()
                .AddGoogle(options =>
                {
                    IConfigurationSection googleAuthNSection =
                        Configuration.GetSection("Authentication:Google");

                    options.ClientId = googleAuthNSection["ClientId"];
                    options.ClientSecret = googleAuthNSection["ClientSecret"];
                })
                .AddFacebook(facebookOptions =>
                {
                    facebookOptions.AppId = Configuration["Authentication:Facebook:AppId"];
                    facebookOptions.AppSecret = Configuration["Authentication:Facebook:AppSecret"];
                });
            services.AddApplicationInsightsTelemetry(Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]);
        }

        private static void AddAuthorizationHandlers(IServiceCollection services)
        {
            //Feedback Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  FeedbackIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  FeedbackAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  FeedbackManagerAuthorizationHandler>();

            //Comment Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  CommentIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  CommentAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  CommentManagerAuthorizationHandler>();

            //Announcement Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  AnnouncementIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  AnnouncementAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  AnnouncementManagerAuthorizationHandler>();

            //Article Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  ArticleIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  ArticleAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  ArticleManagerAuthorizationHandler>();

            //Novel Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  NovelIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  NovelAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  NovelManagerAuthorizationHandler>();

            //Message Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  MessageIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  MessageAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  MessageManagerAuthorizationHandler>();

            //Voting Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  VotingIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  VotingAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  VotingManagerAuthorizationHandler>();

            //Profile Authorization Handlers
            services.AddScoped<IAuthorizationHandler,
                                  ProfileIsOwnerAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  ProfileAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  ProfileManagerAuthorizationHandler>();

            //Payment Authorization Handlers

            services.AddSingleton<IAuthorizationHandler,
                                  PaymentAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  PaymentManagerAuthorizationHandler>();

            //ProfitSharing Authorization Handlers

            services.AddSingleton<IAuthorizationHandler,
                                  ProfitSharingAdministratorsAuthorizationHandler>();

            services.AddSingleton<IAuthorizationHandler,
                                  ProfitSharingManagerAuthorizationHandler>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                //加上MapDefaultControllerRoute()
                endpoints.MapDefaultControllerRoute();
                //支援透過Attribute指定路由
                endpoints.MapControllers();
            });
        }
    }
}
