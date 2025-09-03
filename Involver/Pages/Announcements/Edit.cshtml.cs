using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataAccess.Data;
using DataAccess.Models.AnnouncementModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Authorization.Announcement;
using Involver.Common;
using DataAccess.Common;

namespace Involver.Pages.Announcements
{
    public class EditModel : DI_BasePageModel
    {
        public EditModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        [BindProperty]
        public Announcement Announcement { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Announcement = await _context.Announcements.FirstOrDefaultAsync(m => m.AnnouncementID == id);

            if (Announcement == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                  User, Announcement,
                                                  AnnouncementOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int id)
        {
            if(Announcement.Content?.Length > Parameters.ArticleLength)
            {
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Fetch data from DB to get OwnerID.
            var announcement = await _context
                .Announcements
                .FirstOrDefaultAsync(f => f.AnnouncementID == id);

            if (announcement == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                  User, announcement,
                                                  AnnouncementOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var tempUser = await _context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Announcement.OwnerID);
            announcement.OwnerName = tempUser.UserName;
            announcement.UpdateTime = DateTime.Now;
            announcement.Content = Announcement.Content;
            announcement.Title = Announcement.Title;

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

            return RedirectToPage("./Index");
        }

        private bool AnnouncementExists(int id)
        {
            return _context.Announcements.Any(e => e.AnnouncementID == id);
        }
    }
}
