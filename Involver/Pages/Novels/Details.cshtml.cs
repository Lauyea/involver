using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DataAccess.Data;
using DataAccess.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using DataAccess.Models;
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
                .Include(n => n.Views)
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

            await AddViewRecordAsync();

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

        /// <summary>
        /// AddViewer
        /// </summary>
        /// <returns></returns>
        /// TODO: 之後可能要與 AddView 合併
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

        private async Task AddViewRecordAsync()
        {
            var sessionCookie = Request.Cookies["ViewSession"];
            var sessionId = "";

            if (sessionCookie == null)
            {
                sessionId = Guid.NewGuid().ToString();
                var cookieOptions = new CookieOptions
                {
                    Expires = DateTime.Now.AddDays(1),
                    IsEssential = true
                };
                Response.Cookies.Append("ViewSession", sessionId, cookieOptions);
            }
            else
            {
                sessionId = sessionCookie;
            }

            var userId = _userManager.GetUserId(User);

            // Check if a view has been recorded recently (e.g., in the last hour) to prevent spamming views by refreshing.
            // This timespan can be adjusted.
            var recentViewExists = await _context.Views
                .Where(v => v.NovelId == Novel.NovelID)
                .Where(v => (v.UserId != null && v.UserId == userId) || (v.SessionId == sessionId))
                .Where(v => v.CreateTime > DateTime.Now.AddHours(-1))
                .AnyAsync();

            if (recentViewExists)
            {
                return; // A recent view was found, so don't record another one.
            }

            var view = new DataAccess.Models.View
            {
                NovelId = Novel.NovelID,
                UserId = userId,
                SessionId = sessionId,
                CreateTime = DateTime.Now
            };

            _context.Views.Add(view);

            Novel.TotalViews++;
            Novel.DailyView++;
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
