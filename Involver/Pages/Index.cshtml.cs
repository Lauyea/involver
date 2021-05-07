using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private ApplicationDbContext Context;
        private UserManager<InvolverUser> UserManager { get; }

        public Models.Profile UserProfile;

        [TempData]
        public string StatusMessage { get; set; }

        public ICollection<Follow> Follows { get; set; }

        public IndexModel(ILogger<IndexModel> logger, 
            ApplicationDbContext context, 
            UserManager<InvolverUser> userManager)
        {
            _logger = logger;
            Context = context;
            UserManager = userManager;
        }

        public async Task OnGetAsync()
        {
            string UserId = UserManager.GetUserId(User);
            UserProfile = await Context
                                    .Profiles
                                    .Where(p => p.ProfileID == UserId)
                                    .Include(p => p.Missions)
                                    .FirstOrDefaultAsync();

            Follows = await Context.Follows
                .Include(f => f.Novel)
                    .ThenInclude(n => n.Episodes)
                 .Include(f => f.Novel)
                    .ThenInclude(n => n.Profile)
                .Where(f => f.FollowerID == UserId)
                .OrderByDescending(f => f.Novel.UpdateTime)
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
                Context.Attach(UserProfile).State = EntityState.Modified;
                await Context.SaveChangesAsync();
            }
        }
    }
}
