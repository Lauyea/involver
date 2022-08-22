using Involver.Authorization.Novel;
using Involver.Common;
using Involver.Data;
using Involver.Helpers;
using Involver.Models.ArticleModel;
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

            Novel = await _context.Novels
                .Include(n => n.Profile).FirstOrDefaultAsync(m => m.NovelID == id);

            if (Novel == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
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

            Novel = await _context.Novels.FindAsync(id);

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                 User, Novel,
                                                 NovelOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var comments = from c in _context.Comments
                           where c.NovelID == id
                           select c;

            var episodes = from e in _context.Episodes
                           where e.NovelID == id
                           select e;
            var commentsInEpisodes = from c in _context.Comments
                                     where c.Episode.NovelID == id
                                     select c;
            var votes = from v in _context.Votes
                        where v.NormalOption.Voting.Episode.NovelID == id
                        select v;
            var involvings = from i in _context.Involvings
                             where i.NovelID == id
                             select i;
            var follows = from f in _context.Follows
                          where f.NovelID == id
                          select f;

            if (Novel != null)
            {
                _context.Follows.RemoveRange(follows);
                _context.Involvings.RemoveRange(involvings);
                _context.Votes.RemoveRange(votes);
                _context.Comments.RemoveRange(comments);
                _context.Comments.RemoveRange(commentsInEpisodes);
                _context.Episodes.RemoveRange(episodes);
                _context.Novels.Remove(Novel);
                await _context.SaveChangesAsync();
            }

            var toasts = await AchievementHelper.FirstTimeDeleteAsync(_context, Novel.ProfileID);

            Toasts.AddRange(toasts);

            ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

            return RedirectToPage("./Index");
        }
    }
}
