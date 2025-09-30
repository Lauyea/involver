using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;

using Involver.Authorization.Novel;
using Involver.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Episodes
{
    public class CreateModel : DI_BasePageModel
    {
        public CreateModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            UserManager<InvolverUser> userManager)
            : base(context, authorizationService, userManager)
        {
        }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            Novel = _context.Novels.Where(n => n.NovelID == id).SingleOrDefault();

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                        User, Novel,
                                                        NovelOperations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            return Page();
        }

        [BindProperty]
        public Episode Episode { get; set; }

        public string ErrorMessage { get; set; }
        public Novel Novel { get; set; }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int id)
        {
            Novel = await _context.Novels.Where(n => n.NovelID == id).FirstOrDefaultAsync();

            if (Episode.Content?.Length > Parameters.ArticleLength)
            {
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            await SetOtherEpisodesNotLast(id);

            Episode.OwnerID = _userManager.GetUserId(User);

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                        User, Novel,
                                                        NovelOperations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            Episode emptyEpisode = new Episode
            {
                Title = "temp title",
                Content = "temp content"
            };

            //Protect from overposting attacks
            if (await TryUpdateModelAsync<Episode>(
                emptyEpisode,
                "Episode",   // Prefix for form value.
                e => e.Title, e => e.Content))
            {
                emptyEpisode.UpdateTime = DateTime.Now;
                emptyEpisode.NovelID = id;
                var tempUser = await _context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == emptyEpisode.OwnerID);
                emptyEpisode.OwnerID = Episode.OwnerID;
                emptyEpisode.IsLast = true;
                emptyEpisode.Views = 0;
                emptyEpisode.HasVoting = true;
                _context.Episodes.Add(emptyEpisode);
                Novel.UpdateTime = DateTime.Now;
                await _context.SaveChangesAsync();

                var toasts = await Helpers.AchievementHelper.EpisodeCountAsync(_context, Novel.ProfileID);

                Toasts.AddRange(toasts);

                ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

                return RedirectToPage("/Novels/Details", "OnGet", new { id = id }, "EpisodeHead");
            }
            return Page();
        }

        private async Task SetOtherEpisodesNotLast(int fromID)
        {
            List<Episode> episodes = _context.Episodes.Where(e => e.NovelID == fromID).ToList();
            episodes.ForEach(e => { e.IsLast = false; });
            await _context.SaveChangesAsync();
        }
    }
}