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
using Involver.Authorization.Announcement;

namespace Involver.Pages.Announcements
{
    public class DeleteModel : DI_BasePageModel
    {

        public DeleteModel(
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

            Announcement = await Context.Announcements.FirstOrDefaultAsync(a => a.AnnouncementID == id);

            if (Announcement == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                 User, Announcement,
                                                 AnnouncementOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Announcement = await Context.Announcements.FindAsync(id);

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                 User, Announcement,
                                                 AnnouncementOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }


            var comments = from c in Context.Comments
                           where c.AnnouncementID == id
                           select c;

            if (Announcement != null)
            {
                Context.Comments.RemoveRange(comments);
                Context.Announcements.Remove(Announcement);
                await Context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
