using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Involver.Data;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Models;
using Involver.Authorization.Novel;
using NuGet.Frameworks;

namespace Involver.Pages.Novels
{
    [AllowAnonymous]
    public class DetailsModel : DI_BasePageModel
    {
        public DetailsModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public Novel Novel { get; set; }
        public Profile Profile { get; set; }
        public PaginatedList<Comment> Comments { get; set; }
        public PaginatedList<Episode> Episodes { get; set; }
        public List<Novel> RecommendNovels { get; set; }
        public bool Followed { get; set; } = false;

        public async Task<IActionResult> OnGetAsync(int? id, int? pageIndex, int? pageIndexEpisode)
        {
            if (id == null)
            {
                return NotFound();
            }

            Novel = await Context.Novels
                .Include(n => n.Profile)
                .Include(n => n.Follows)
                .FirstOrDefaultAsync(m => m.NovelID == id);

            if (Novel == null)
            {
                return NotFound();
            }

            Profile = Novel.Profile;

            var SetCommentsTask = SetComments(id, pageIndex);

            IQueryable<Episode> episodes = from e in Context.Episodes
                                           select e;
            episodes = episodes
                .Where(e => e.NovelID == id)
                .OrderByDescending(e => e.EpisodeID);

            int pageSize = 10;
            var EpisodesTask = PaginatedList<Episode>.CreateAsync(
                episodes, pageIndexEpisode ?? 1, pageSize);

            await Task.WhenAll(SetCommentsTask, EpisodesTask).ConfigureAwait(false);

            //Check authorization
            var isAuthorized = User.IsInRole(Authorization.Novel.Novels.NovelManagersRole) ||
                           User.IsInRole(Authorization.Novel.Novels.NovelAdministratorsRole);

            var currentUserId = UserManager.GetUserId(User);

            if (!isAuthorized
                && currentUserId != Novel.ProfileID
                && Novel.Block)
            {
                return Forbid();
            }

            RecommendNovels = Context.Novels
                .Where(n => n.Type == Novel.Type)
                .Where(n => n.Block == false)
                .OrderByDescending(n => n.MonthlyCoins)
                .Take(5)
                .OrderByDescending(n => n.UpdateTime)
                .AsNoTracking()
                .ToList();
            string UserID = UserManager.GetUserId(User);
            Follow ExistingFollow = Novel.Follows
                .Where(f => f.FollowerID == UserID)
                .FirstOrDefault();
            if(ExistingFollow != null)
            {
                Followed = true;
            }
            else
            {
                Followed = false;
            }

            //Add views
            Novel.Views++;
            Context.Attach(Novel).State = EntityState.Modified;

            try
            {
                await Context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NovelExists(Novel.NovelID))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return Page();
        }

        private async Task SetComments(int? id, int? pageIndex)
        {
            IQueryable<Comment> comments = from c in Context.Comments
                                           select c;
            comments = comments
                .Include(c => c.Agrees)
                .Include(c => c.Messages)
                .Include(c => c.Profile)
                .Include(c => c.Dices)
                .Include(c => c.Novel)
                    .ThenInclude(n => n.Involvers)
                .Where(c => c.NovelID == id)
                .OrderByDescending(c => c.CommentID);

            int pageSize = 5;
            Comments = await PaginatedList<Comment>.CreateAsync(
                comments, pageIndex ?? 1, pageSize);
        }

        private bool NovelExists(int id)
        {
            return Context.Novels.Any(e => e.NovelID == id);
        }

        public async Task<IActionResult> OnPostAsync(int id, bool block)
        {
            var novel = await Context.Novels.FirstOrDefaultAsync(
                                                      m => m.NovelID == id);

            if (novel == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(User, novel,
                                        NovelOperations.Block);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }
            novel.Block = block;
            Context.Novels.Update(novel);
            await Context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPostAgreeAsync(int id, int fromID, int pageIndex)
        {
            string OwenrID = UserManager.GetUserId(User);

            if (OwenrID == null)
            {
                return Challenge();
            }

            Agree ExistingAgree = await Context.Agrees
                .Where(a => a.CommentID == id)
                .Where(a => a.ProfileID == OwenrID)
                .FirstOrDefaultAsync();

            if(ExistingAgree == null)
            {
                Agree agree = new Agree
                {
                    CommentID =id,
                    ProfileID = OwenrID,
                    UpdateTime = DateTime.Now
                };
                Context.Agrees.Add(agree);

                //Check mission:BeAgreed //CheckMissionBeAgreed
                Comment comment = await Context.Comments
                    .Where(c => c.CommentID == id)
                    .Include(c => c.Profile)
                    .FirstOrDefaultAsync();
                string UserID = comment.ProfileID;
                Profile Commenter = await Context.Profiles
                    .Where(p => p.ProfileID == UserID)
                    .Include(p => p.Missions)
                    .FirstOrDefaultAsync();
                if (Commenter.Missions.BeAgreed != true)
                {
                    Commenter.Missions.BeAgreed = true;
                    Commenter.VirtualCoins += 5;
                    Context.Attach(Commenter).State = EntityState.Modified;
                }
                //Check other missions
                Missions missions = Commenter.Missions;
                if (missions.WatchArticle
                    && missions.Vote
                    && missions.LeaveComment
                    && missions.ViewAnnouncement
                    && missions.ShareCreation
                    && missions.BeAgreed)
                {
                    Commenter.Missions.CompleteOtherMissions = true;
                    Context.Attach(Commenter).State = EntityState.Modified;
                }

                await Context.SaveChangesAsync();
            }
            else
            {
                Context.Agrees.Remove(ExistingAgree);
                await Context.SaveChangesAsync();
            }

            return RedirectToPage("./Details", "OnGet", new { id = fromID, pageIndex }, id.ToString());
        }

        public async Task<IActionResult> OnPostAgreeCommentAsync(int id)
        {
            string OwenrID = UserManager.GetUserId(User);

            if (OwenrID == null)
            {
                return Challenge();
            }

            Agree ExistingAgree = await Context.Agrees
                .Where(a => a.CommentID == id)
                .Where(a => a.ProfileID == OwenrID)
                .FirstOrDefaultAsync();

            Comment comment = null;

            if (ExistingAgree == null)
            {
                Agree agree = new Agree
                {
                    CommentID = id,
                    ProfileID = OwenrID,
                    UpdateTime = DateTime.Now
                };
                Context.Agrees.Add(agree);

                //Check mission:BeAgreed //CheckMissionBeAgreed
                comment = await Context.Comments
                    .Where(c => c.CommentID == id)
                    .Include(c => c.Profile)
                    .Include(c => c.Agrees)
                    .FirstOrDefaultAsync();
                string UserID = comment.ProfileID;
                Profile Commenter = await Context.Profiles
                    .Where(p => p.ProfileID == UserID)
                    .Include(p => p.Missions)
                    .FirstOrDefaultAsync();
                if (Commenter.Missions.BeAgreed != true)
                {
                    Commenter.Missions.BeAgreed = true;
                    Commenter.VirtualCoins += 5;
                    Context.Attach(Commenter).State = EntityState.Modified;
                }
                //Check other missions
                Missions missions = Commenter.Missions;
                if (missions.WatchArticle
                    && missions.Vote
                    && missions.LeaveComment
                    && missions.ViewAnnouncement
                    && missions.ShareCreation
                    && missions.BeAgreed)
                {
                    Commenter.Missions.CompleteOtherMissions = true;
                    Context.Attach(Commenter).State = EntityState.Modified;
                }

                await Context.SaveChangesAsync();
            }
            else
            {
                comment = await Context.Comments
                    .Where(c => c.CommentID == id)
                    .Include(c => c.Agrees)
                    .FirstOrDefaultAsync();
                Context.Agrees.Remove(ExistingAgree);
                await Context.SaveChangesAsync();
            }

            if(comment != null)
            {
                return Content(comment.Agrees.Count().ToString());
            }
            else
            {
                return BadRequest();
            }
        }

        public async Task<IActionResult> OnPostFollowAsync(int id)
        {
            Novel novel = await Context.Novels
                .Include(n => n.Follows)
                .Where(n => n.NovelID == id)
                .FirstOrDefaultAsync();

            if (novel == null)
            {
                return Page();
            }
            string UserID = UserManager.GetUserId(User);
            Follow follow = novel.Follows.Where(f => f.FollowerID == UserID).FirstOrDefault();

            if (follow == null)
            {
                Follow newFollow = new Follow
                {
                    FollowerID = UserID,
                    NovelID = id,
                    UpdateTime = DateTime.Now,
                    NovelMonthlyInvolver = false,
                    ProfileMonthlyInvolver = false
                };
                Context.Follows.Add(newFollow);
                await Context.SaveChangesAsync();
            }
            else
            {
                Context.Follows.Remove(follow);
                await Context.SaveChangesAsync();
            }

            return RedirectToPage("./Details", "OnGet", new { id });
        }
    }
}
