using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;

using DataAccess.Data;

using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;

using WebPWrecover.Services;

var builder = WebApplication.CreateBuilder(args);

// Call AddOpenTelemetry() to add OpenTelemetry to your ServiceCollection.
// Call UseAzureMonitor() to fully configure OpenTelemetry.
builder.Services.AddOpenTelemetry().UseAzureMonitor();

if (builder.Environment.IsProduction())
{
    builder.Configuration.AddAzureKeyVault(
        new Uri($"https://{builder.Configuration["KeyVaultName"]}.vault.azure.net/"),
        new DefaultAzureCredential());
}

var services = builder.Services;

// Cookie Policy
services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential 
    // cookies is needed for a given request.
    options.CheckConsentNeeded = context => true;
    // requires using Microsoft.AspNetCore.Http;
    options.MinimumSameSitePolicy = SameSiteMode.None;
});

// Add services to the container.
services.AddHttpClient();

services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("ConnectionStrings"),
        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));

    // Use it when you need to analyze EF Core performance
    //options.EnableSensitiveDataLogging();
});

services.AddDefaultIdentity<InvolverUser>(
    options => options.SignIn.RequireConfirmedAccount = true)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

//SendGrid function
services.AddTransient<IEmailSender, EmailSender>();
services.Configure<AuthMessageSenderOptions>(builder.Configuration);

//services.AddRazorPages();
services.AddMvc();

// 將預設的 IHtmlGenerator 替換為自訂的 HTML5 版本
services.AddSingleton<IHtmlGenerator, Html5ValidationHtmlGenerator>();

services.AddResponseCaching();

//宣告 AJAX POST 使用的 Header 名稱
services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");

services.AddControllers(config =>
{
    var policy = new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser()
                     .Build();
    config.Filters.Add(new AuthorizeFilter(policy));
});

ServiceExtension.AddAuthorizationHandlers(services);

services.AddScoped<IAchievementService, AchievementService>();

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

    options.SlidingExpiration = true;

    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }
        context.Response.Redirect("/Identity/Account/Login");
        return Task.CompletedTask;
    };

    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        context.Response.Redirect("/Identity/Account/AccessDenied");
        return Task.CompletedTask;
    };
});

services.AddAuthentication()
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    })
    .AddFacebook(facebookOptions =>
    {
        facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"];
        facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
    });

services.AddDistributedMemoryCache();

// 不再使用 Session 去紀錄 dark mode parameter
//services.AddSession(options =>
//{
//    options.Cookie.Name = "_DarkMode";
//    options.IdleTimeout = TimeSpan.FromDays(365);
//    options.Cookie.HttpOnly = true;
//    options.Cookie.IsEssential = true;
//});

var app = builder.Build();

//using (var scope = app.Services.CreateScope())
//{
//    var serviceProvider = scope.ServiceProvider;
//    var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
//    context.Database.Migrate();
//    // requires using Microsoft.Extensions.Configuration;
//    // Set password with the Secret Manager tool.
//    // dotnet user-secrets set SeedUserPW <pw>

//    var testUserPw = builder.Configuration.GetValue<string>("SeedUserPW");

//    await SeedData.Initialize(serviceProvider, testUserPw);
//}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCookiePolicy();

app.UseRouting();

app.UseResponseCaching();

app.UseAuthentication();
app.UseAuthorization();

//app.UseSession(); // 暫時沒有用到 Session

app.MapRazorPages();
//加上MapDefaultControllerRoute()
app.MapDefaultControllerRoute();
//支援透過Attribute指定路由
app.MapControllers();

app.Run();