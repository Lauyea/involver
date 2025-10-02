using DataAccess.Data;
using DataAccess.Models.NovelModel;
using Involver.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataAccess.Common;

namespace Involver.Areas.Identity.Pages.Profile
{
    [AllowAnonymous]
    public class ViewesModel : DI_BasePageModel
    {
        public ViewesModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            UserManager<InvolverUser> userManager)
            : base(context, authorizationService, userManager)
        {
        }

        public class ViewedNovelViewModel
        {
            public int NovelID { get; set; }
            public string Title { get; set; }
            public string Introduction { get; set; }
            public DateTime ViewDate { get; set; }
        }

        public DataAccess.Models.Profile Profile { get; set; }
        public List<ViewedNovelViewModel> Novels { get; set; }

        private async Task<List<ViewedNovelViewModel>> GetViewedNovels(string id, int pageIndex = 1)
        {
            var userId = _userManager.GetUserId(User);
            if (id != userId)
            {
                return new List<ViewedNovelViewModel>(); // Or handle as an error
            }

            Profile = await _context.Profiles
                .Where(p => p.ProfileID == id)
                .Include(p => p.ViewedNovels)
                .FirstOrDefaultAsync();

            var novels = Profile.ViewedNovels
                .OrderByDescending(n => n.NovelViewers.FirstOrDefault()?.ViewDate)
                .Select(n => new ViewedNovelViewModel
                {
                    NovelID = n.NovelID,
                    Title = n.Title,
                    Introduction = n.Introduction.Length < Parameters.ContentLength 
                        ? n.Introduction 
                        : n.Introduction.Substring(0, Parameters.ContentLength) + "...",
                    ViewDate = (DateTime)(n.NovelViewers.FirstOrDefault()?.ViewDate)
                })
                .Skip((pageIndex - 1) * Parameters.PageSize)
                .Take(Parameters.PageSize)
                .ToList();

            return novels;
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            Profile = await _context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);

            if (Profile == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);
            if (id != userId)
            {
                return Forbid();
            }

            Novels = await GetViewedNovels(id);

            return Page();
        }

        public async Task<IActionResult> OnGetLoadMoreAsync(string id, int pageIndex)
        {
            var novels = await GetViewedNovels(id, pageIndex);
            return Partial("_ViewedNovelListPartial", novels);
        }
    }
}
