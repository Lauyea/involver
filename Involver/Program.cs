using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Involver.Data;
using Involver.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Authorization;
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

//�ŧi AJAX POST �ϥΪ� Header �W��
services.AddAntiforgery(o => o.HeaderName = "X-CSRF-TOKEN");

services.AddControllers(config =>
{
    var policy = new AuthorizationPolicyBuilder()
                     .RequireAuthenticatedUser()
                     .Build();
    config.Filters.Add(new AuthorizeFilter(policy));
});

ServiceExtension.AddAuthorizationHandlers(services);

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
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    })
    .AddFacebook(facebookOptions =>
    {
        facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"];
        facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"];
    });

services.AddDistributedMemoryCache();

services.AddSession(options =>
{
    options.Cookie.Name = "_DarkMode";
    options.IdleTimeout = TimeSpan.FromDays(365);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapRazorPages();
//�[�WMapDefaultControllerRoute()
app.MapDefaultControllerRoute();
//�䴩�z�LAttribute���w����
app.MapControllers();

app.Run();
