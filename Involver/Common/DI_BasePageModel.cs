using DataAccess.Data;

using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Common;

public class DI_BasePageModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService) : PageModel()
{
    protected ApplicationDbContext Context { get; } = context;
    protected IAuthorizationService AuthorizationService { get; } = authorizationService;
    protected UserManager<InvolverUser> UserManager { get; } = userManager;

    protected IAchievementService AchievementService { get; } = achievementService;

    [TempData]
    public string StatusMessage { get; set; }
}