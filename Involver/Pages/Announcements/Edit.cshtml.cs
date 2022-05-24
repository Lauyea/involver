using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Involver.Data;
using Involver.Models.AnnouncementModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Authorization.Announcement;
using Involver.Common;

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

            Announcement = await Context.Announcements.FirstOrDefaultAsync(m => m.AnnouncementID == id);

            if (Announcement == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
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
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Fetch data from DB to get OwnerID.
            var announcement = await Context
                .Announcements.AsNoTracking()
                .FirstOrDefaultAsync(f => f.AnnouncementID == id);

            if (announcement == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                  User, announcement,
                                                  AnnouncementOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            Announcement.OwnerID = announcement.OwnerID;
            var tempUser = await Context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Announcement.OwnerID);
            Announcement.OwnerName = tempUser.UserName;
            Announcement.UpdateTime = DateTime.Now;
            Announcement.Views = announcement.Views;

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

            return RedirectToPage("./Index");
        }

        private bool AnnouncementExists(int id)
        {
            return Context.Announcements.Any(e => e.AnnouncementID == id);
        }
    }
}
