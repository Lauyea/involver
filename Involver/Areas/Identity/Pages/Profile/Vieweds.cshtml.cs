using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.NovelModel;

using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile;

[AllowAnonymous]
public class ViewesModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
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
        var userId = UserManager.GetUserId(User);
        if (id != userId)
        {
            return new List<ViewedNovelViewModel>(); // Or handle as an error
        }

        // 在資料庫端排序、分頁，只取列表需要的欄位
        var rows = await Context.Set<NovelViewer>()
            .Where(nv => nv.ProfileID == id)
            .OrderByDescending(nv => nv.ViewDate)
            .Skip((pageIndex - 1) * Parameters.PageSize)
            .Take(Parameters.PageSize)
            .Select(nv => new
            {
                nv.NovelID,
                nv.Novel.Title,
                nv.Novel.Introduction,
                nv.ViewDate,
            })
            .AsNoTracking()
            .ToListAsync();

        var novels = rows
            .Select(r => new ViewedNovelViewModel
            {
                NovelID = r.NovelID,
                Title = r.Title,
                Introduction = r.Introduction.Length < Parameters.ContentLength
                    ? r.Introduction
                    : r.Introduction.Substring(0, Parameters.ContentLength) + "...",
                ViewDate = r.ViewDate,
            })
            .ToList();

        return novels;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        Profile = await Context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);

        if (Profile == null)
        {
            return NotFound();
        }

        var userId = UserManager.GetUserId(User);
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
