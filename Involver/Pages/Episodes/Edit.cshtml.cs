using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Involver.Data;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Authorization.Novel;

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

            Episode = await Context.Episodes
                .Include(e => e.Novel).FirstOrDefaultAsync(m => m.EpisodeID == id);

            if (Episode == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
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
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Fetch data from DB to get OwnerID.
            Episode episode = await Context
                .Episodes
                .Include(e => e.Novel)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.EpisodeID == id);

            if (episode == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                  User, episode.Novel,
                                                  NovelOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            Episode.UpdateTime = DateTime.Now;
            Episode.Views = episode.Views;
            Episode.OwnerID = UserManager.GetUserId(User);
            Episode.HasVoting = episode.HasVoting;
            Episode.IsLast = episode.IsLast;
            Episode.NovelID = episode.NovelID;

            Context.Attach(Episode).State = EntityState.Modified;

            try
            {
                await Context.SaveChangesAsync();
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

            return RedirectToPage("/Novels/Details", "OnGet", new { id = Episode.NovelID }, "EpisodeHead");
        }

        private bool EpisodeExists(int id)
        {
            return Context.Episodes.Any(e => e.EpisodeID == id);
        }
    }
}
