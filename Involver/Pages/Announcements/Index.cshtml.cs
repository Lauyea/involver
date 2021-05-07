﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Involver.Data;
using Involver.Models.AnnouncementModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Models;

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


        public PaginatedList<Announcement> Announcements { get; set; }
        public string CurrentFilter { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

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

            var announcements = from a in Context.Announcements
                                select a;

            //Announcements = await Context.Announcements
            //    .OrderBy(a => a.UpdateTime)
            //    .ToListAsync();

            if (!String.IsNullOrEmpty(searchString))
            {
                announcements = announcements
                    .Where(a => a.OwnerName.Contains(searchString) || a.Title.Contains(searchString))
                    .OrderByDescending(a => a.UpdateTime);
            }

            announcements = announcements.OrderByDescending(a => a.UpdateTime);

            await CheckMissionViewAnnouncement();

            int PageSize = 5;
            Announcements = await PaginatedList<Announcement>.CreateAsync(
                announcements.AsNoTracking(), PageIndex ?? 1, PageSize);
        }

        private async Task CheckMissionViewAnnouncement()
        {
            //Check mission:ViewAnnouncement
            string UserID = UserManager.GetUserId(User);
            if (UserID != null)
            {
                Profile userProfile = await Context.Profiles
                .Where(p => p.ProfileID == UserID)
                .Include(p => p.Missions)
                .FirstOrDefaultAsync();
                if (userProfile.Missions.ViewAnnouncement != true)
                {
                    userProfile.Missions.ViewAnnouncement = true;
                    userProfile.VirtualCoins += 5;
                    Context.Attach(userProfile).State = EntityState.Modified;
                    StatusMessage = "每週任務：瀏覽公告 已完成，獲得5 虛擬In幣。";
                }
                //Check other missions
                Missions missions = userProfile.Missions;
                if (missions.WatchArticle
                    && missions.Vote
                    && missions.LeaveComment
                    && missions.ViewAnnouncement
                    && missions.ShareCreation
                    && missions.BeAgreed)
                {
                    userProfile.Missions.CompleteOtherMissions = true;
                    Context.Attach(userProfile).State = EntityState.Modified;
                }
                await Context.SaveChangesAsync();
            }
        }
    }
}
