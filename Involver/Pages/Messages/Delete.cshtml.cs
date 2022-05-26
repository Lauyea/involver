using Involver.Authorization.Message;
using Involver.Common;
using Involver.Data;
using Involver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Messages
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
                                                 MessageOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(int? id, int? fromID)
        {
            if (id == null)
            {
                return NotFound();
            }

            Message = await _context.Messages.FindAsync(id);

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                 User, Message,
                                                 MessageOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            if (Message != null)
            {
                _context.Messages.Remove(Message);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("/Comments/Details", "OnGet", new { id = fromID, pageIndex = 1 });
        }
    }
}
