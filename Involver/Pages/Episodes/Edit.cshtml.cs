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
                                                  NovelOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int id)
        {
            if(Episode.Content.Length > Parameters.ArticleLength)
            {
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Fetch data from DB to get OwnerID.
            Episode episode = await _context
                .Episodes
                .Include(e => e.Novel)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.EpisodeID == id);

            if (episode == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                  User, episode.Novel,
                                                  NovelOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            Episode.UpdateTime = DateTime.Now;
            Episode.Views = episode.Views;
            Episode.OwnerID = _userManager.GetUserId(User);
            Episode.HasVoting = episode.HasVoting;
            Episode.IsLast = episode.IsLast;
            Episode.NovelID = episode.NovelID;

            _context.Attach(Episode).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!EpisodeExists(Episode.EpisodeID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            var toasts = await Helpers.AchievementHelper.FirstTimeEditAsync(_context, Episode.OwnerID);

            Toasts.AddRange(toasts);

            ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

            return RedirectToPage("/Novels/Details", "OnGet", new { id = Episode.NovelID }, "EpisodeHead");
        }

        private bool EpisodeExists(int id)
        {
            return _context.Episodes.Any(e => e.EpisodeID == id);
        }
    }
}
