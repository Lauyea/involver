using System.Text.Json;

using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;

using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Involvings;

[AllowAnonymous]
public class InvolveArticleModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService,
ICoinService coinService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    [BindProperty]
    public Involving Involving { get; set; }
    public Article Article { get; set; }
    public Profile Involver { get; set; }
    public int ArticleID { get; set; }
    public string UserID { get; set; }

    private async Task LoadAsync(int id)
    {
        UserID = UserManager.GetUserId(User);
        if (UserID != null)
        {
            Involver = await Context.Profiles.FindAsync(UserID);
        }
        Article = await Context.Articles
            .Include(a => a.Profile)
            .Where(a => a.ArticleID == id)
            .FirstOrDefaultAsync();
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        ArticleID = id;
        await LoadAsync(id);

        if (UserID == null)
        {
            return Challenge();
        }

        if (Article != null)
        {
            return Page();
        }
        else
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        ArticleID = id;
        await LoadAsync(ArticleID);

        if (UserID == null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Article == null)
        {
            return NotFound();
        }

        await using var transaction = await Context.Database.BeginTransactionAsync();

        if (!await coinService.TrySpendAsync(UserID, Involving.Value))
        {
            ModelState.AddModelError(Involving.Value.ToString(), "帳戶餘額不足");
            return Page();
        }
        await coinService.AddCreatorIncomeAsync(Article.ProfileID, Involving.Value * 0.5m);//文章直接贊助，作者得50%分潤
        await coinService.AddArticleCoinsAsync(ArticleID, Involving.Value);

        Involving ExistingInvolving = await Context.Involvings
            .Where(i => i.ArticleID == ArticleID)
            .Where(i => i.InvolverID == UserID)
            .FirstOrDefaultAsync();

        if (ExistingInvolving != null)
        {
            ExistingInvolving.Value = Involving.Value;
            ExistingInvolving.MonthlyValue += Involving.Value;
            ExistingInvolving.TotalValue += Involving.Value;
            ExistingInvolving.LastTime = DateTime.Now;
        }
        else
        {
            Involving newInvolving = new Involving()
            {
                Value = Involving.Value,
                MonthlyValue = Involving.Value,
                TotalValue = Involving.Value,
                LastTime = DateTime.Now,
                InvolverID = UserID,
                ArticleID = ArticleID
            };
            Context.Involvings.Add(newInvolving);
        }
        await Context.SaveChangesAsync();
        await transaction.CommitAsync();

        // In 幣是直接以 SQL 更新，重新載入才會拿到最新的已使用 In 幣供成就計算
        await Context.Entry(Involver).ReloadAsync();

        var toasts = await AchievementService.UseCoinsCountAsync(Involver.ProfileID, Involver.UsedCoins);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        return RedirectToPage("/Articles/Details", "OnGet", new { id = Article.ArticleID });
    }
}