using System;
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
    public class DetailsModel : DI_BasePageModel
    {
        public DetailsModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public PaginatedList<Comment> Comments { get; set; }
        public Announcement Announcement { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, int? pageIndex)
        {
            if (id == null)
            {
                return NotFound();
            }

            Announcement = await Context.Announcements
                .FirstOrDefaultAsync(m => m.AnnouncementID == id);
            await SetComments(id, pageIndex);

            if (Announcement == null)
            {
                return NotFound();
            }

            Announcement.Views++;
            Context.Attach(Announcement).State = EntityState.Modified;

            try
            {
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AnnouncementExists(Announcement.AnnouncementID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Page();
        }

        private async Task SetComments(int? id, int? pageIndex)
        {
            IQueryable<Comment> comments = from c in Context.Comments
                                           select c;
            comments = comments
                .Include(c => c.Agrees)
                .Include(c => c.Messages)
                .Include(c => c.Profile)
                .Include(c => c.Dices)
                .Where(c => c.AnnouncementID == id)
                .OrderBy(c => c.CommentID);

            int pageSize = 5;
            Comments = await PaginatedList<Comment>.CreateAsync(
                comments, pageIndex ?? 1, pageSize);
        }

        private bool AnnouncementExists(int id)
        {
            return Context.Announcements.Any(e => e.AnnouncementID == id);
        }

        public async Task<IActionResult> OnPostAgreeCommentAsync(int id)
        {
            string OwenrID = UserManager.GetUserId(User);

            if (OwenrID == null)
            {
                return Challenge();
            }

            Agree ExistingAgree = await Context.Agrees
                .Where(a => a.CommentID == id)
                .Where(a => a.ProfileID == OwenrID)
                .FirstOrDefaultAsync();

            Comment comment = null;

            if (ExistingAgree == null)
            {
                Agree agree = new Agree
                {
                    CommentID = id,
                    ProfileID = OwenrID,
                    UpdateTime = DateTime.Now
                };
                Context.Agrees.Add(agree);

                //Check mission:BeAgreed //CheckMissionBeAgreed
                comment = await Context.Comments
                    .Where(c => c.CommentID == id)
                    .Include(c => c.Profile)
                    .Include(c => c.Agrees)
                    .FirstOrDefaultAsync();
                string UserID = comment.ProfileID;
                Profile Commenter = await Context.Profiles
                    .Where(p => p.ProfileID == UserID)
                    .Include(p => p.Missions)
                    .FirstOrDefaultAsync();
                if (Commenter.Missions.BeAgreed != true)
                {
                    Commenter.Missions.BeAgreed = true;
                    Commenter.VirtualCoins += 5;
                    Context.Attach(Commenter).State = EntityState.Modified;
                }
                //Check other missions
                Missions missions = Commenter.Missions;
                if (missions.WatchArticle
                    && missions.Vote
                    && missions.LeaveComment
                    && missions.ViewAnnouncement
                    && missions.ShareCreation
                    && missions.BeAgreed)
                {
                    Commenter.Missions.CompleteOtherMissions = true;
                    Context.Attach(Commenter).State = EntityState.Modified;
                }

                await Context.SaveChangesAsync();
            }
            else
            {
                comment = await Context.Comments
                    .Where(c => c.CommentID == id)
                    .Include(c => c.Agrees)
                    .FirstOrDefaultAsync();
                Context.Agrees.Remove(ExistingAgree);
                await Context.SaveChangesAsync();
            }

            if (comment != null)
            {
                return Content(comment.Agrees.Count().ToString());
            }
            else
            {
                return BadRequest();
            }
        }
    }
}
