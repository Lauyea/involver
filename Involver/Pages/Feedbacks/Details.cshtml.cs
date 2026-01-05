using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Common;
using Involver.Services;
using Involver.Services.NotificationSetterService;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Feedbacks;

[AllowAnonymous]
public class DetailsModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
INotificationSetter notificationSetter,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public Article Feedback { get; set; }
    public string UserID { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id, int? pageIndex)
    {
        if (id == null)
        {
            return NotFound();
        }

        Feedback = await Context.Articles
            .Include(f => f.Profile)
            .FirstOrDefaultAsync(f => f.ArticleID == id);

        if (Feedback == null)
        {
            return NotFound();
        }

        //Comments = Context.Comments.Where(c => c.FeedbackID == id).OrderByDescending(c => c.UpdateTime).ToList();

        var isAuthorized = User.IsInRole(Authorization.Article.Articles.ArticleManagersRole) ||
                       User.IsInRole(Authorization.Article.Articles.ArticleAdministratorsRole);

        UserID = UserManager.GetUserId(User);

        if (!isAuthorized
            && UserID != Feedback.ProfileID
            && Feedback.Block)
        {
            return Forbid();
        }

        var toasts = await AchievementService.ReadFeedbackAsync(UserID);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, bool block)
    {
        var feedback = await Context.Articles.FirstOrDefaultAsync(
                                                  m => m.ArticleID == id);

        if (feedback == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(User, feedback,
                                    ArticleOperations.Block);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }
        feedback.Block = block;
        Context.Articles.Update(feedback);
        await Context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostAcceptAsync(int id)
    {
        var feedback = await Context.Articles
            .FirstOrDefaultAsync(m => m.ArticleID == id);

        Profile profile = await Context.Profiles.FirstOrDefaultAsync(p => p.ProfileID == feedback.ProfileID);

        if (feedback == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(User, feedback,
                                    ArticleOperations.Block);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }
        feedback.Accept = true;
        Context.Articles.Update(feedback);
        profile.VirtualCoins += 50;//回報錯誤與採納意見獲得虛擬In幣50枚
        await Context.SaveChangesAsync();

        var toasts = await AchievementService.AcceptCountAsync(feedback.ProfileID);

        // Set notification
        var url = $"{Request.Scheme}://{Request.Host}/Feedbacks/Details?id={id}";

        await notificationSetter.ForFeedbackAcceptAsync(feedback.Title, feedback.ProfileID, url, toasts);

        return RedirectToPage("./Index");
    }
}