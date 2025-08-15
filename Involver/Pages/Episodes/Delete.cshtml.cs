using Involver.Authorization.Novel;
using Involver.Common;
using DataAccess.Data;
using Involver.Helpers;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;
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

            Episode = await _context.Episodes
                .Include(e => e.Novel).FirstOrDefaultAsync(m => m.EpisodeID == id);

            if (Episode == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
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

            Episode = await _context.Episodes
                .Include(e => e.Novel).FirstOrDefaultAsync(m => m.EpisodeID == id);

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                 User, Episode.Novel,
                                                 NovelOperations.Delete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            var comments = from c in _context.Comments
                           where c.EpisodeID == id
                           select c;
            var votes = from v in _context.Votes
                        where v.NormalOption.Voting.EpisodeID == id
                        select v;

            if (Episode != null)
            {
                _context.Votes.RemoveRange(votes);
                _context.Comments.RemoveRange(comments);
                _context.Episodes.Remove(Episode);
                await _context.SaveChangesAsync();
            }

            var toasts = await AchievementHelper.FirstTimeDeleteAsync(_context, Episode.OwnerID);

            Toasts.AddRange(toasts);

            ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

            //return RedirectToPage("./Index");
            return RedirectToPage("/Novels/Details", "OnGet", new { id = Episode.NovelID });
        }
    }
}
