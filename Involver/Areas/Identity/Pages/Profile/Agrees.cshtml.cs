using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;

using Involver.Common;
using Involver.Extensions;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile;

[AllowAnonymous]
public class AgreesModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public class AgreeViewModel
    {
        public string Content { get; set; }
        public string PageName { get; set; }
        public string RouteId { get; set; }
        public string Fragment { get; set; }
        public string DestinationTitle { get; set; }
        public DateTime UpdateTime { get; set; }
    }

    public DataAccess.Models.Profile Profile { get; set; }
    public List<AgreeViewModel> Agrees { get; set; }

    private async Task<List<AgreeViewModel>> GetAgrees(string id, int pageIndex = 1)
    {
        var agrees = await Context.Agrees
            .Include(a => a.Comment).ThenInclude(c => c.Novel)
            .Include(a => a.Comment).ThenInclude(c => c.Episode)
            .Include(a => a.Comment).ThenInclude(c => c.Article)
            .Include(a => a.Message).ThenInclude(m => m.Comment)
            .Where(a => a.ProfileID == id)
            .OrderByDescending(a => a.UpdateTime)
            .Skip((pageIndex - 1) * Parameters.PageSize)
            .Take(Parameters.PageSize)
            .AsNoTracking()
            .ToListAsync();

        var agreeViewModels = new List<AgreeViewModel>();

        foreach (var agree in agrees)
        {
            var vm = new AgreeViewModel
            {
                UpdateTime = agree.UpdateTime
            };

            if (agree.Comment != null)
            {
                vm.Content = agree.Comment.Content.StripHTML().Length < Parameters.ContentLength
                    ? agree.Comment.Content.StripHTML()
                    : agree.Comment.Content.StripHTML().Substring(0, Parameters.ContentLength) + "...";

                if (agree.Comment.NovelID != null)
                {
                    vm.PageName = "/Novels/Details";
                    vm.RouteId = agree.Comment.NovelID.ToString();
                    vm.DestinationTitle = agree.Comment.Novel.Title;
                }
                else if (agree.Comment.EpisodeID != null)
                {
                    vm.PageName = "/Episodes/Details";
                    vm.RouteId = agree.Comment.EpisodeID.ToString();
                    vm.DestinationTitle = agree.Comment.Episode.Title;
                }
                else if (agree.Comment.ArticleID != null)
                {
                    vm.PageName = "/Articles/Details";
                    vm.RouteId = agree.Comment.ArticleID.ToString();
                    vm.DestinationTitle = agree.Comment.Article.Title;
                }
                vm.Fragment = "CommentHead";
            }
            else if (agree.Message != null)
            {
                vm.Content = agree.Message.Content.Length < Parameters.ContentLength
                    ? agree.Message.Content
                    : agree.Message.Content.Substring(0, Parameters.ContentLength) + "...";
                vm.PageName = "/Comments/Details";
                vm.RouteId = agree.Message.CommentID.ToString();
                vm.DestinationTitle = agree.Message.Comment.Content.StripHTML().Length < Parameters.SmallContentLength
                    ? agree.Message.Comment.Content.StripHTML()
                    : agree.Message.Comment.Content.StripHTML().Substring(0, Parameters.SmallContentLength) + "...";
            }

            agreeViewModels.Add(vm);
        }

        return agreeViewModels;
    }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        Profile = await Context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);

        if (Profile == null)
        {
            return NotFound();
        }

        Agrees = await GetAgrees(id);

        return Page();
    }

    public async Task<IActionResult> OnGetLoadMoreAsync(string id, int pageIndex)
    {
        var agrees = await GetAgrees(id, pageIndex);
        return Partial("_AgreeListPartial", agrees);
    }
}
