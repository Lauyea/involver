using System.Text.Json;

using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;

using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Involvings;

[AllowAnonymous]
public class InvolveNovelModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService,
ICoinService coinService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    [BindProperty]
    public Involving Involving { get; set; }
    public Novel Novel { get; set; }
    public int NovelID { get; set; }
    public string UserID { get; set; }

    public Profile Involver { get; set; }

    private async Task LoadAsync(int id)
    {
        UserID = UserManager.GetUserId(User);
        Novel = await Context.Novels
            .Include(n => n.Profile)
            .Where(n => n.NovelID == id)
            .FirstOrDefaultAsync();
        Involver = await Context.Profiles
            .Where(p => p.ProfileID == UserID)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// In 幣是直接以 SQL 更新，追蹤中的實體仍是舊值，重新載入後頁面與成就計算才會拿到最新的餘額
    /// </summary>
    private async Task ReloadAfterCoinsChangedAsync()
    {
        await Context.Entry(Involver).ReloadAsync();
        await Context.Entry(Novel).ReloadAsync();
    }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        NovelID = id;
        await LoadAsync(id);
        if (UserID == null)
        {
            return Challenge();
        }

        if (Novel != null)
        {
            return Page();
        }
        else
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostSingleInvolveAsync(int id)
    {
        NovelID = id;
        await LoadAsync(NovelID);

        if (UserID == null)
        {
            return Challenge();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (Novel == null)
        {
            return NotFound();
        }

        await using var transaction = await Context.Database.BeginTransactionAsync();

        if (!await coinService.TrySpendAsync(UserID, Involving.Value))
        {
            ModelState.AddModelError(Involving.Value.ToString(), "帳戶餘額不足");
            return Page();
        }
        await coinService.AddCreatorIncomeAsync(Novel.ProfileID, Involving.Value * 0.5m);//創作直接贊助，作者得50%分潤
        await coinService.AddNovelCoinsAsync(NovelID, Involving.Value);

        Involving ExistingInvolving = await Context.Involvings
            .Where(i => i.NovelID == NovelID)
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
                NovelID = NovelID
            };
            Context.Involvings.Add(newInvolving);
        }
        await Context.SaveChangesAsync();
        await transaction.CommitAsync();
        await ReloadAfterCoinsChangedAsync();
        StatusMessage = "Involve成功，總共" + Involving.Value + " In幣，感謝以實體行動鼓勵創作";

        var toasts = await AchievementService.UseCoinsCountAsync(Involver.ProfileID, Involver.UsedCoins);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostMonthlyInvolveAsync(int id)
    {
        NovelID = id;
        int InvolveValue = 150;
        await LoadAsync(id);
        if (UserID == null)
        {
            return Challenge();
        }

        if (Novel == null)
        {
            return NotFound();
        }

        await using var transaction = await Context.Database.BeginTransactionAsync();

        // 先扣款：扣款會鎖住付款人的資料列直到交易結束，之後讀到的 Follow 才不會因重複送出而重複扣款
        if (!await coinService.TrySpendAsync(UserID, InvolveValue))
        {
            StatusMessage = "Error: 帳戶餘額不足";
            return Page();
        }

        Follow existingFollow = await Context.Follows
            .Where(f => f.NovelID == NovelID)
            .Where(f => f.FollowerID == UserID)
            .FirstOrDefaultAsync();

        if (existingFollow == null)
        {
            Follow newFollow = new Follow
            {
                FollowerID = UserID,
                NovelMonthlyInvolver = true,
                UpdateTime = DateTime.Now,
                NovelID = NovelID
            };
            Context.Follows.Add(newFollow);
        }
        else
        {
            if (existingFollow.NovelMonthlyInvolver)
            {
                // 交易沒有提交，剛才的扣款會一併還原
                StatusMessage = "已經Involve過了，如要繼續Involve，可以使用直接Involve的功能";
                return Page();
            }
            existingFollow.NovelMonthlyInvolver = true;
            existingFollow.UpdateTime = DateTime.Now;
        }

        await coinService.AddCreatorIncomeAsync(Novel.ProfileID, InvolveValue * 0.6m);//創作每月Involve，作者得60%分潤
        await coinService.AddNovelCoinsAsync(NovelID, InvolveValue);

        Involving ExistingInvolving = await Context.Involvings
            .Where(i => i.NovelID == NovelID)
            .Where(i => i.InvolverID == UserID)
            .FirstOrDefaultAsync();

        if (ExistingInvolving != null)
        {
            ExistingInvolving.Value = InvolveValue;
            ExistingInvolving.MonthlyValue += InvolveValue;
            ExistingInvolving.TotalValue += InvolveValue;
            ExistingInvolving.LastTime = DateTime.Now;
        }
        else
        {
            Involving newInvolving = new Involving()
            {
                Value = InvolveValue,
                MonthlyValue = InvolveValue,
                TotalValue = InvolveValue,
                LastTime = DateTime.Now,
                InvolverID = UserID,
                NovelID = NovelID
            };
            Context.Involvings.Add(newInvolving);
        }

        await Context.SaveChangesAsync();
        await transaction.CommitAsync();
        await ReloadAfterCoinsChangedAsync();

        StatusMessage = "每月Involve成功，感謝以實體行動鼓勵創作";

        var toasts = await AchievementService.UseCoinsCountAsync(Involver.ProfileID, Involver.UsedCoins);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        return Page();
    }

    public async Task<IActionResult> OnPostUnInvolveAsync(int id)
    {
        NovelID = id;
        await LoadAsync(id);
        if (UserID == null)
        {
            return Challenge();
        }

        Follow follow = await Context.Follows
            .Where(f => f.NovelID == NovelID)
            .Where(f => f.FollowerID == UserID)
            .FirstOrDefaultAsync();

        if (follow == null)
        {
            StatusMessage = "Error: 無效操作";
            return Page();
        }
        else
        {
            if (!follow.NovelMonthlyInvolver)
            {
                StatusMessage = "Error: 您沒有每月Involve這部創作";
                return Page();
            }
            follow.NovelMonthlyInvolver = false;
        }
        await Context.SaveChangesAsync();
        StatusMessage = "UnInvolve成功";
        return Page();
    }
}