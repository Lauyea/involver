using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;

using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Announcements;

[AllowAnonymous]
public class DetailsModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService,
IViewCountService viewCountService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public Article Announcement { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id, int? pageIndex)
    {
        if (id == null)
        {
            return NotFound();
        }

        Announcement = await Context.Articles
            .Include(a => a.Profile)
            .FirstOrDefaultAsync(a => a.ArticleID == id);

        if (Announcement == null)
        {
            return NotFound();
        }

        // 觀看數以原子更新累加，避免併發時「讀出、改值、存回」掉計數；公告不計每日觀看數，與原本行為一致
        await viewCountService.AddArticleViewAsync(Announcement, countDaily: false);

        var profileId = UserManager.GetUserId(User);

        var toasts = await AchievementService.ReadAnnouncementAsync(profileId);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        return Page();
    }
}