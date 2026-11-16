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

public class BecomeProfessionalModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public Profile Profile { get; set; }
    public string ProfileID { get; set; }
    public int Involvers { get; set; }
    public bool CanBeProfessional { get; set; } = false;

    private async Task LoadAsync(string id)
    {
        Profile = await Context.Profiles
            .Where(p => p.ProfileID == id)
            .FirstOrDefaultAsync();
        if (Profile != null)
        {
            // 對此創作者本人的月贊助者：被追蹤的對象是 Follow.ProfileID。
            // （原本用 Profile.Followers，那是 FollowerID == 本人，也就是「本人追蹤別人」的方向，算反了）
            var profileInvolvers = await Context.Follows
                .CountAsync(f => f.ProfileID == id && f.ProfileMonthlyInvolver);

            // 對此創作者作品的月贊助者
            var novelInvolvers = await Context.Follows
                .CountAsync(f => f.NovelID != null
                    && f.Novel.ProfileID == id
                    && f.NovelMonthlyInvolver);

            Involvers = profileInvolvers + novelInvolvers;

            if (Involvers > 100 && Profile.MonthlyCoins > 1000)
            {
                CanBeProfessional = true;
            }
            else
            {
                CanBeProfessional = false;
            }
        }
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        ProfileID = id;
        await LoadAsync(id);

        if (Profile != null)
        {
            return Page();
        }
        else
        {
            return NotFound();
        }
    }

    public async Task<IActionResult> OnPostAsync(string id)
    {
        // 只能為自己申請成為特約作家
        if (id == null || id != UserManager.GetUserId(User))
        {
            return Forbid();
        }

        ProfileID = id;
        await LoadAsync(id);
        if (CanBeProfessional)
        {
            Profile.Professional = true;

            // 明確存檔；成就服務在成就已取得時不會呼叫 SaveChanges
            await Context.SaveChangesAsync();

            List<Toast> toasts = await AchievementService.BeProfessionalAsync(ProfileID);

            if (toasts.Count > 0)
            {
                TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
            }
        }

        return RedirectToPage("/Profile/Index", "OnGet", new { area = "Identity", id = ProfileID });
    }
}