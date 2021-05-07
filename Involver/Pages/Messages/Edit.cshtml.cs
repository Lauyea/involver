using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Involver.Data;
using Involver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Authorization.Message;

namespace Involver.Pages.Messages
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
        public Message Message { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Message = await Context.Messages
                .Include(m => m.Comment).FirstOrDefaultAsync(m => m.MessageID == id);

            if (Message == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                  User, Message,
                                                  MessageOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int id, int fromID)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Fetch data from DB to get OwnerID.
            var message = await Context
                .Messages
                .Include(m => m.Comment)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.MessageID == id);

            if (message == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                  User, message,
                                                  MessageOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            Message.ProfileID = message.ProfileID;
            Message.UpdateTime = DateTime.Now;
            Message.CommentID = fromID;

            Context.Attach(Message).State = EntityState.Modified;

            try
            {
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!MessageExists(Message.MessageID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("/Comments/Details", "OnGet", new { id = fromID, pageIndex = 1 });
        }

        private bool MessageExists(int id)
        {
            return Context.Messages.Any(e => e.MessageID == id);
        }
    }
}
