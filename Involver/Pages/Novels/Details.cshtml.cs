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
using Involver.Common;

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
        public Profile Writer { get; set; }
        public PaginatedList<Comment> Comments { get; set; }
        public PaginatedList<Episode> Episodes { get; set; }
        public List<Novel> RecommendNovels { get; set; }
        public bool Followed { get; set; } = false;

        public string UserId { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id, int? pageIndex, int? pageIndexEpisode)
        {
            if (id == null)
            {
                return NotFound();
            }

            Novel = await _context.Novels
                .Include(n => n.Profile)
                .Include(n => n.Follows)
                .Include(n => n.NovelTags)
                .Include(n => n.Viewers)
                .Include(n => n.ViewIps)
                .FirstOrDefaultAsync(m => m.NovelID == id);

            if (Novel == null)
            {
                return NotFound();
            }

            //Check authorization
            var isAuthorized = User.IsInRole(Authorization.Novel.Novels.NovelManagersRole) 
                || User.IsInRole(Authorization.Novel.Novels.NovelAdministratorsRole);

            UserId = _userManager.GetUserId(User);

            if (!isAuthorized
                && UserId != Novel.ProfileID
                && Novel.Block)
            {
                return Forbid();
            }

            Writer = Novel.Profile;

            await SetComments(id, pageIndex);

            IQueryable<Episode> episodes = from e in _context.Episodes
                                           select e;
            episodes = episodes
                .Where(e => e.NovelID == id)
                .OrderByDescending(e => e.EpisodeID);


            Episodes = await PaginatedList<Episode>.CreateAsync(
                episodes, pageIndexEpisode ?? 1, Parameters.EpisodePageSize);

            var tagArr = Novel.NovelTags.ToArray();

            var recommendNovels = _context.Novels
                .Where(n => n.Type == Novel.Type)
                .Where(n => n.Block == false);

            // There is most 3 tags right now
            if (tagArr.Count() > 2)
            {
                recommendNovels = recommendNovels
                    .Where(n => n.NovelTags.Contains(tagArr[0])
                    || n.NovelTags.Contains(tagArr[1])
                    || n.NovelTags.Contains(tagArr[2]));
            }
            else if (tagArr.Count() > 1)
            {
                recommendNovels = recommendNovels
                    .Where(n => n.NovelTags.Contains(tagArr[0])
                    || n.NovelTags.Contains(tagArr[1]));
            }
            else if (tagArr.Count() > 0)
            {
                recommendNovels = recommendNovels
                    .Where(n => n.NovelTags.Contains(tagArr[0]));
            }


            recommendNovels = recommendNovels.OrderByDescending(n => n.MonthlyCoins)
                .Take(5)
                .OrderByDescending(n => n.UpdateTime)
                .AsNoTracking();

            RecommendNovels = recommendNovels.ToList();

            Follow ExistingFollow = Novel.Follows
                .Where(f => f.FollowerID == UserId)
                .FirstOrDefault();

            if (ExistingFollow != null)
            {
                Followed = true;
            }
            else
            {
                Followed = false;
            }

            AddViewsByIp();

            if (UserId != null)
            {
                await AddViewer();
            }

            try
            {
                await _context.SaveChangesAsync();
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

            if (!string.IsNullOrEmpty(ToastsJson))
            {
                Toasts = System.Text.Json.JsonSerializer.Deserialize<List<Toast>>(ToastsJson);
            }

            var toasts = await Helpers.AchievementHelper.ReadNovelAsync(_context, UserId);

            Toasts.AddRange(toasts);

            return Page();
        }

        private async Task AddViewer()
        {
            var userAsViewer = Novel.Viewers.Where(v => v.ProfileID == UserId).FirstOrDefault();

            if (userAsViewer != null)
            {
                var novelViewer = userAsViewer.NovelViewers.Where(v => v.ProfileID == UserId && v.NovelID == Novel.NovelID).FirstOrDefault();

                novelViewer.ViewDate = DateTime.Now;
            }
            else
            {
                var userProfile = await _context.Profiles.Where(p => p.ProfileID == UserId).FirstOrDefaultAsync();

                Novel.Viewers.Add(userProfile);
            }
        }

        private void AddViewsByIp()
        {
            var ip = Request.HttpContext.Connection.RemoteIpAddress.ToString();

            var ipRecord = Novel.ViewIps.Where(i => i.Ip == ip).FirstOrDefault();

            if (ipRecord != null)
            {
                return;
            }

            var newIp = new ViewIp()
            {
                Ip = ip
            };

            Novel.Views++;
            Novel.DailyView++;
            Novel.ViewIps.Add(newIp);
        }

        private async Task SetComments(int? id, int? pageIndex)
        {
            IQueryable<Comment> comments = from c in _context.Comments
                                           select c;
            comments = comments
                .Include(c => c.Agrees)
                .Include(c => c.Messages.OrderByDescending(m => m.UpdateTime).Take(5))
                    .ThenInclude(c => c.Profile)
                .Include(c => c.Profile)
                .Include(c => c.Dices)
                .Include(c => c.Novel)
                    .ThenInclude(n => n.Involvers)
                .Where(c => c.NovelID == id)
                .OrderByDescending(c => c.CommentID);

            
            Comments = await PaginatedList<Comment>.CreateAsync(
                comments, pageIndex ?? 1, Parameters.CommetPageSize);
        }

        private bool NovelExists(int id)
        {
            return _context.Novels.Any(e => e.NovelID == id);
        }

        public async Task<IActionResult> OnPostAsync(int id, bool block)
        {
            var novel = await _context.Novels.FirstOrDefaultAsync(
                                                      m => m.NovelID == id);

            if (novel == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(User, novel,
                                        NovelOperations.Block);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }
            novel.Block = block;
            _context.Novels.Update(novel);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
