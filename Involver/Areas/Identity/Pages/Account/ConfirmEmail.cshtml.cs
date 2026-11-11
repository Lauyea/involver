using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.AchievementModel;
using DataAccess.Models.StatisticalData;

using Involver.Services;


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<InvolverUser> _userManager;
        private readonly ApplicationDbContext Context;
        private readonly ICoinService _coinService;

        public ConfirmEmailModel(UserManager<InvolverUser> userManager, ApplicationDbContext context, ICoinService coinService)
        {
            _userManager = userManager;
            Context = context;
            _coinService = coinService;
        }

        [TempData]
        public string StatusMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            StatusMessage = result.Succeeded ? "感謝確認你的Email" : "Error 確認Emil發生錯誤";
            //外部登入者可以變更用戶名一次
            DataAccess.Models.Profile profile = new DataAccess.Models.Profile
            {
                ProfileID = user.Id,
                UserName = user.UserName,
                RealCoins = 0,
                VirtualCoins = 0,
                EnrollmentDate = DateTime.Now,
                LastTimeLogin = DateTime.Now,
                Professional = false,
                Prime = false,
                Banned = false,
                Missions = new Missions() { ProfileID = user.Id },
                Achievements = new List<Achievement>(),
                CanChangeUserName = user.PasswordHash == null
            };

            await using var transaction = await Context.Database.BeginTransactionAsync();
            Context.Profiles.Add(profile);
            await Context.SaveChangesAsync();

            // 註冊獎勵經過 CoinService，才會留下流水帳
            await _coinService.AddVirtualCoinsAsync(
                profile.ProfileID,
                300,
                new CoinTransactionInfo(CoinTransactionType.SignUpBonus, Note: "註冊獎勵"));
            await transaction.CommitAsync();

            return Page();
        }
    }
}