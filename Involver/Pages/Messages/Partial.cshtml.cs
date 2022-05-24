using Involver.Common;
using Involver.Data;
using Involver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Messages
{
    [AllowAnonymous]
    public class PartialModel : DI_BasePageModel
    {
        public PartialModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            UserManager<InvolverUser> userManager)
            : base(context, authorizationService, userManager)
        {
        }

        public PaginatedList<Message> Messages { get; set; }

        public async Task<IActionResult> OnGetAsync(int? commentId, int? pageIndex)
        {
            IQueryable<Message> messages = from m in Context.Messages
                                           select m;
            messages = messages
                .Include(m => m.Comment)
                    .ThenInclude(c => c.Episode)
                        .ThenInclude(e => e.Novel)
                            .ThenInclude(n => n.Involvers)
                .Include(m => m.Agrees)
                .Include(m => m.Profile)
                .Where(m => m.CommentID == commentId)
                .OrderBy(m => m.CommentID);

            Messages = await PaginatedList<Message>.CreateAsync(
                messages, pageIndex ?? 1, Parameters.MessagePageSize);

            return Page();
        }
    }
}
