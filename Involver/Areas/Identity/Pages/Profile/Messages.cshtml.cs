using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using Involver.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile
{
    [AllowAnonymous]
    public class MessagesModel : DI_BasePageModel
    {
        public MessagesModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            UserManager<InvolverUser> userManager)
            : base(context, authorizationService, userManager)
        {
        }

        public DataAccess.Models.Profile Profile { get; set; }
        public List<Message> Messages { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            Profile = await _context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);

            if (Profile == null)
            {
                return NotFound();
            }

            Messages = await _context.Messages
                .Include(m => m.Comment)
                .Where(m => m.ProfileID == id)
                .OrderByDescending(m => m.UpdateTime)
                .Take(Parameters.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnGetLoadMoreAsync(string id, int pageIndex)
        {
            var messages = await _context.Messages
                .Include(m => m.Comment)
                .Where(m => m.ProfileID == id)
                .OrderByDescending(m => m.UpdateTime)
                .Skip((pageIndex - 1) * Parameters.PageSize)
                .Take(Parameters.PageSize)
                .AsNoTracking()
                .ToListAsync();

            return Partial("_MessageListPartial", messages);
        }
    }
}
