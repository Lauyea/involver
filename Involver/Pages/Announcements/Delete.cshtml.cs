using DataAccess.Data;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Announcements;

public class DeleteModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    [BindProperty]
    public Article Announcement { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Announcement = await Context.Articles.Include(a => a.Profile).FirstOrDefaultAsync(a => a.ArticleID == id);

        if (Announcement == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                             User, Announcement,
                                             ArticleOperations.Delete);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        return Page();
    }

    /// <summary>
    /// 刪除公告（軟刪除）
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

        Announcement = await Context.Articles.FindAsync(id);

        if (Announcement == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                             User, Announcement,
                                             ArticleOperations.Delete);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        Announcement.IsDeleted = true;
        await Context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}