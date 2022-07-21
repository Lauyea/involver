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
using Involver.Common;

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

            Announcement = await _context.Announcements
                .FirstOrDefaultAsync(m => m.AnnouncementID == id);
            await SetComments(id, pageIndex);

            if (Announcement == null)
            {
                return NotFound();
            }

            Announcement.Views++;
            _context.Attach(Announcement).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
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
            IQueryable<Comment> comments = from c in _context.Comments
                                           select c;
            comments = comments
                .Include(c => c.Agrees)
                .Include(c => c.Messages.OrderByDescending(m => m.UpdateTime).Take(5))
                    .ThenInclude(c => c.Profile)
                .Include(c => c.Profile)
                .Include(c => c.Dices)
                .Where(c => c.AnnouncementID == id)
                .OrderBy(c => c.CommentID);

            Comments = await PaginatedList<Comment>.CreateAsync(
                comments, pageIndex ?? 1, Parameters.CommetPageSize);
        }

        private bool AnnouncementExists(int id)
        {
            return _context.Announcements.Any(e => e.AnnouncementID == id);
        }
    }
}
