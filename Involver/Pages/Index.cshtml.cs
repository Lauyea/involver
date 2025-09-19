using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using DataAccess.Data;
using DataAccess.Models;

using Involver.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Involver.Pages
{
    [AllowAnonymous]
    public class IndexModel : DI_BasePageModel
    {
        //private readonly ILogger<IndexModel> _logger;

        public Profile UserProfile;

        public ICollection<Follow> Follows { get; set; }

        //public IndexModel(ILogger<IndexModel> logger, 
        //    ApplicationDb_context _context, 
        //    UserManager<InvolverUser> userManager)
        //{
        //    _logger = logger;
        //    _context = _context;
        //    UserManager = userManager;
        //}

        public IndexModel(
        ApplicationDbContext _context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(_context, authorizationService, userManager)
        {
        }

        public async Task<IActionResult> OnGetAsync()
        {
            string userId = _userManager.GetUserId(User);

            //if(userId == null)
            //{
            //    return RedirectToPage("/Feed/TrendingCreations", "OnGet");
            //}

            UserProfile = await _context
                                    .Profiles
                                    .Where(p => p.ProfileID == userId)
                                    .Include(p => p.Missions)
                                    .FirstOrDefaultAsync();

            if (UserProfile == null)
            {
                return Page();
            }

            Follows = await _context.Follows
                .Include(f => f.Novel)
                    .ThenInclude(n => n.Episodes)
                 .Include(f => f.Novel)
                    .ThenInclude(n => n.Profile)
                .Where(f => f.FollowerID == userId)
                .OrderByDescending(f => f.Novel.UpdateTime)
                .Take(10)
                .ToListAsync();

            if (UserProfile != null)
            {
                if (!UserProfile.Missions.DailyLogin)
                {
                    UserProfile.LastTimeLogin = DateTime.Now;//登入時間
                    UserProfile.Missions.DailyLogin = true;//每日登入任務
                    UserProfile.VirtualCoins += 5;
                    StatusMessage = "每日登入 已完成，獲得5 虛擬In幣。";
                }
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(ToastsJson))
            {
                Toasts = JsonSerializer.Deserialize<List<Toast>>(ToastsJson);
            }

            var toasts = await Helpers.AchievementHelper.GetCoinsCountAsync(_context, userId, UserProfile.VirtualCoins + UserProfile.RealCoins);

            Toasts.AddRange(toasts);

            toasts = await Helpers.AchievementHelper.CheckGradeAsync(_context, UserProfile.ProfileID, UserProfile.EnrollmentDate);

            Toasts.AddRange(toasts);

            return Page();
        }
    }
}