using DataAccess.Data;
using DataAccess.Models;

using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Involvings;

[AllowAnonymous]
public class ArticleInvolversModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public ICollection<Involving> Involvings { get; set; }

    public int ArticleID { get; set; }

    public string TimeSpan { get; set; }

    private async Task LoadAsync(int id)
    {
        if (TimeSpan == "TotalTime")
        {
            Involvings = await Context.Involvings
            .Include(i => i.Involver)
            .Where(i => i.ArticleID == id)
            .OrderByDescending(i => i.TotalValue)
            .Take(100)
            .ToListAsync();
        }
        else if (TimeSpan == "Monthly")
        {
            Involvings = await Context.Involvings
            .Include(i => i.Involver)
            .Where(i => i.ArticleID == id)
            .OrderByDescending(i => i.MonthlyValue)
            .Take(100)
            .ToListAsync();
        }
        else
        {
            Involvings = await Context.Involvings
            .Include(i => i.Involver)
            .Where(i => i.ArticleID == id)
            .OrderByDescending(i => i.LastTime)
            .Take(100)
            .ToListAsync();
        }
    }

    public async Task<IActionResult> OnGetAsync(int id, string TimeSpan)
    {
        ArticleID = id;
        this.TimeSpan = TimeSpan;
        await LoadAsync(id);
        if (Involvings != null)
        {
            return Page();
        }
        else
        {
            return NotFound();
        }
    }
}