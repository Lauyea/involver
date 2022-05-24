using Involver.Authorization.Novel;
using Involver.Common;
using Involver.Data;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Novels
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
        public Novel Novel { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Novel = await Context.Novels
                .Include(n => n.Profile).FirstOrDefaultAsync(m => m.NovelID == id);

            if (Novel == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                 User, Novel,
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

            Novel = await Context.Novels.FindAsync(id);

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                 User, Novel,
                                                 NovelOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var comments = from c in Context.Comments
                           where c.NovelID == id
                           select c;

            var episodes = from e in Context.Episodes
                           where e.NovelID == id
                           select e;
            var commentsInEpisodes = from c in Context.Comments
                                     where c.Episode.NovelID == id
                                     select c;
            var votes = from v in Context.Votes
                        where v.NormalOption.Voting.Episode.NovelID == id
                        select v;
            var involvings = from i in Context.Involvings
                             where i.NovelID == id
                             select i;
            var follows = from f in Context.Follows
                          where f.NovelID == id
                          select f;

            if (Novel != null)
            {
                Context.Follows.RemoveRange(follows);
                Context.Involvings.RemoveRange(involvings);
                Context.Votes.RemoveRange(votes);
                Context.Comments.RemoveRange(comments);
                Context.Comments.RemoveRange(commentsInEpisodes);
                Context.Episodes.RemoveRange(episodes);
                Context.Novels.Remove(Novel);
                await Context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
