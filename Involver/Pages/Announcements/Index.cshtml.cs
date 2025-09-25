using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Comment;
using Involver.Common;
using Involver.Extensions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Announcements
{
    [AllowAnonymous]
    public class IndexModel : DI_BasePageModel
    {
        public IndexModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }


        public PaginatedList<Article> Announcements { get; set; }
        public string CurrentFilter { get; set; }

        public async Task OnGetAsync(
            string currentFilter,
            string searchString,
            int? PageIndex)
        {
            if (searchString != null)
            {
                PageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            CurrentFilter = searchString;

            var announcements = from a in _context.Articles
                                where a.Type == ArticleType.Announcement
                                select a;

            //Announcements = await Context.Announcements
            //    .OrderBy(a => a.UpdateTime)
            //    .ToListAsync();

            if (!String.IsNullOrEmpty(searchString))
            {
                announcements = announcements
                    .Where(a => a.Profile.UserName.Contains(searchString) || a.Title.Contains(searchString))
                    .OrderByDescending(a => a.UpdateTime);
            }

            announcements = announcements.OrderByDescending(a => a.UpdateTime);

            await CheckMissionViewAnnouncement();

            Announcements = await PaginatedList<Article>.CreateAsync(
                announcements.AsNoTracking(), PageIndex ?? 1, Parameters.ArticlePageSize);
        }

        private async Task CheckMissionViewAnnouncement()
        {
            //Check mission:ViewAnnouncement
            string UserID = _userManager.GetUserId(User);
            if (UserID != null)
            {
                Profile userProfile = await _context.Profiles
                .Where(p => p.ProfileID == UserID)
                .Include(p => p.Missions)
                .FirstOrDefaultAsync();
                if (userProfile.Missions.ViewAnnouncement != true)
                {
                    userProfile.Missions.ViewAnnouncement = true;
                    userProfile.AwardCoins();
                    StatusMessage = "每週任務：瀏覽公告 已完成，獲得5 虛擬In幣。";
                }

                // 檢查是否完成所有任務，若完成會自動加獎勵幣
                userProfile.Missions.CheckCompletion(userProfile);

                await _context.SaveChangesAsync();
            }
        }
    }
}