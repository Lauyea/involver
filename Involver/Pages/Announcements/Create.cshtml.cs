using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Involver.Data;
using Involver.Models.AnnouncementModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Authorization.Announcement;
using Involver.Models;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Announcements
{
    public class CreateModel : DI_BasePageModel
    {

        public CreateModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public IActionResult OnGet()
        {
            var isAuthorized = User.IsInRole(Authorization.Announcement.Announcements.AnnouncementManagersRole) ||
                           User.IsInRole(Authorization.Announcement.Announcements.AnnouncementAdministratorsRole);

            if (!isAuthorized)
            {
                return Forbid();
            }
            return Page();
        }

        [BindProperty]
        public Announcement Announcement { get; set; }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            Announcement.OwnerID = UserManager.GetUserId(User);

            var isAuthorized = User.IsInRole(Authorization.Announcement.Announcements.AnnouncementManagersRole) ||
                           User.IsInRole(Authorization.Announcement.Announcements.AnnouncementAdministratorsRole);

            if (!isAuthorized)
            {
                return Forbid();
            }


            Announcement emptyAnnouncement =
                new Announcement
                {
                    Title = "temp title",
                    Content = "temp content",
                    Comments = new List<Comment>
                {
                    //防止Comment找不到所屬的Announcement
                    new Comment
                    {
                        ProfileID = Announcement.OwnerID,
                        AnnouncementID = Announcement.AnnouncementID,
                        Block = true,
                        Content = "anchor"
                    }
                }
                };

            //Protect from overposting attacks
            if (await TryUpdateModelAsync<Announcement>(
                emptyAnnouncement,
                "announcement",   // Prefix for form value.
                f => f.Title, f => f.Content))
            {
                emptyAnnouncement.UpdateTime = DateTime.Now;
                var tempUser = await Context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Announcement.OwnerID);
                emptyAnnouncement.OwnerID = Announcement.OwnerID;
                emptyAnnouncement.OwnerName = tempUser.UserName;
                emptyAnnouncement.Views = 0;
                Context.Announcements.Add(emptyAnnouncement);
                await Context.SaveChangesAsync();

                return RedirectToPage("./Index");
            }

            //Context.Announcements.Add(Announcement);
            //await Context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
