using DataAccess.Data;
using DataAccess.Models;

using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.StatisticalData;

[AllowAnonymous]
public class IndexModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public List<Profile> Profiles { get; set; }

    public IActionResult OnGet()
    {
        Profiles = Context.Profiles
            .OrderByDescending(p => p.EnrollmentDate)
            .AsNoTracking()
            .ToList();
        return Page();
    }
}