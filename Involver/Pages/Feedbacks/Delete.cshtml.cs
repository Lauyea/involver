using System.Text.Json;

using DataAccess.Data;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Common;
using Involver.Helpers;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Feedbacks;

public class DeleteModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    [BindProperty]
    public Article Feedback { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Feedback = await Context.Articles.Include(f=> f.Profile).FirstOrDefaultAsync(m => m.ArticleID == id);

        if (Feedback == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                             User, Feedback,
                                             ArticleOperations.Delete);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        return Page();
    }

    /// <summary>
    /// 刪除意見回饋（軟刪除）
    /// </summary>
    /// <remarks>
    /// 與文章一致改用 IsDeleted。硬刪除會因為留言的按讚、觀看紀錄、Involve 等
    /// NO ACTION 外鍵而失敗，且觀看與贊助紀錄不應連帶消失。
    /// </remarks>
    /// <param name="id">ArticleID</param>
    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Feedback = await Context.Articles.FindAsync(id);

        if (Feedback == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                             User, Feedback,
                                             ArticleOperations.Delete);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        Feedback.IsDeleted = true;
        await Context.SaveChangesAsync();

        // 成就屬於執行操作的人：管理員編輯/刪除別人的內容時，不可替作者跳成就，否則 Toast 會顯示在管理員畫面上
        var actorId = UserManager.GetUserId(User);

        var toasts = await AchievementService.FirstTimeDeleteAsync(actorId);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        return RedirectToPage("./Index");
    }
}