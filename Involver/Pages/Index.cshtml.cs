using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Involver.Common;
using Involver.Data;
using Involver.Models;
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

        public Models.Profile UserProfile;

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
            string UserId = _userManager.GetUserId(User);

            if(UserId == null)
            {
                return RedirectToPage("/Feed/TrendingCreations", "OnGet");
            }

            UserProfile = await _context
                                    .Profiles
                                    .Where(p => p.ProfileID == UserId)
                                    .Include(p => p.Missions)
                                    .FirstOrDefaultAsync();

            Follows = await _context.Follows
                .Include(f => f.Novel)
                    .ThenInclude(n => n.Episodes)
                 .Include(f => f.Novel)
                    .ThenInclude(n => n.Profile)
                .Where(f => f.FollowerID == UserId)
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
                _context.Attach(UserProfile).State = EntityState.Modified;
                await _context.SaveChangesAsync();
            }

            if (!string.IsNullOrEmpty(ToastsJson))
            {
                Toasts = JsonSerializer.Deserialize<List<Toast>>(ToastsJson);
            }

            return Page();
        }
    }
}
