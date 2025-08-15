using Involver.Authorization.Message;
using Involver.Common;
using DataAccess.Data;
using DataAccess.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            Message = await _context.Messages
                .Include(m => m.Comment).FirstOrDefaultAsync(m => m.MessageID == id);

            if (Message == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
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
            var message = await _context
                .Messages
                .Include(m => m.Comment)
                .FirstOrDefaultAsync(m => m.MessageID == id);

            if (message == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                  User, message,
                                                  MessageOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            message.UpdateTime = DateTime.Now;
            message.CommentID = fromID;
            message.Content = Message.Content;

            try
            {
                await _context.SaveChangesAsync();
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

            var toasts = await Helpers.AchievementHelper.FirstTimeEditAsync(_context, Message.ProfileID);

            Toasts.AddRange(toasts);

            ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

            return RedirectToPage("/Comments/Details", "OnGet", new { id = fromID, pageIndex = 1 });
        }

        private bool MessageExists(int id)
        {
            return _context.Messages.Any(e => e.MessageID == id);
        }
    }
}
