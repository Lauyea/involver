using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Involver.Data;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Authorization.Novel;
using Microsoft.EntityFrameworkCore;
using Involver.Models;

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

        public async Task<IActionResult> OnGetAsync(string from, int? fromID)
        {
            Novel = Context.Novels.Where(n => n.NovelID == fromID).SingleOrDefault();

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
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
        public async Task<IActionResult> OnPostAsync(string from, int fromID)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await SetOtherEpisodesNotLast(fromID);

            Episode.OwnerID = UserManager.GetUserId(User);

            Novel = Context.Novels.Where(n => n.NovelID == fromID).FirstOrDefault();

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                        User, Novel,
                                                        NovelOperations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            if (from != "Novels")
            {
                ErrorMessage = "沒有指定的評論頁面";
                return Page();
            }

            Episode emptyEpisode = new Episode
            {
                Votings = new List<Voting>
                {
                    new Voting
                    {
                        Title = "anchor",
                        EpisodeID = Episode.EpisodeID,
                        OwnerID = Episode.OwnerID
                    }
                },
                Title = "temp title",
                Content = "temp content",
                Comments = new List<Comment>
                {
                    //防止Comment找不到所屬的Episode
                    new Comment
                    {
                        ProfileID = Episode.OwnerID,
                        EpisodeID = Episode.EpisodeID,
                        Block = true,
                        Content = "anchor"
                    }
                }
            };

            //Protect from overposting attacks
            if (await TryUpdateModelAsync<Episode>(
                emptyEpisode,
                "Episode",   // Prefix for form value.
                e => e.Title, e => e.Content))
            {
                emptyEpisode.UpdateTime = DateTime.Now;
                if (from == "Novels")
                {
                    emptyEpisode.NovelID = fromID;
                }
                var tempUser = await Context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == emptyEpisode.OwnerID);
                emptyEpisode.OwnerID = Episode.OwnerID;
                emptyEpisode.IsLast = true;
                emptyEpisode.Views = 0;
                emptyEpisode.HasVoting = true;
                Context.Episodes.Add(emptyEpisode);
                Novel.UpdateTime = DateTime.Now;
                Context.Attach(Novel).State = EntityState.Modified;
                await Context.SaveChangesAsync();

                if (from != null)
                {
                    return RedirectToPage("/" + from + "/Details", "OnGet", new { id = fromID }, "EpisodeHead");
                }
            }
            return Page();
        }

        private async Task SetOtherEpisodesNotLast(int fromID)
        {
            List<Episode> episodes = Context.Episodes.Where(e => e.NovelID == fromID).ToList();
            episodes.ForEach(e => { e.IsLast = false; });
            await Context.SaveChangesAsync();
        }
    }
}
