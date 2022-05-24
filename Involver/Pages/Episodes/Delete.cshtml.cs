using Involver.Authorization.Novel;
using Involver.Common;
using Involver.Data;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Episodes
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
        public Episode Episode { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Episode = await Context.Episodes
                .Include(e => e.Novel).FirstOrDefaultAsync(m => m.EpisodeID == id);

            if (Episode == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                 User, Episode.Novel,
                                                 NovelOperations.Delete);
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

            Episode = await Context.Episodes
                .Include(e => e.Novel).FirstOrDefaultAsync(m => m.EpisodeID == id);

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                 User, Episode.Novel,
                                                 NovelOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var comments = from c in Context.Comments
                           where c.EpisodeID == id
                           select c;
            var votes = from v in Context.Votes
                        where v.NormalOption.Voting.EpisodeID == id
                        select v;

            if (Episode != null)
            {
                Context.Votes.RemoveRange(votes);
                Context.Comments.RemoveRange(comments);
                Context.Episodes.Remove(Episode);
                await Context.SaveChangesAsync();
            }

            //return RedirectToPage("./Index");
            return RedirectToPage("/Novels/Details", "OnGet", new { id = Episode.NovelID });
        }
    }
}
