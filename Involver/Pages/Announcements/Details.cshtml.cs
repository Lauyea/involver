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
    }
}
