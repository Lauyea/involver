using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;

using Involver.Common;
using Involver.Extensions;
using Involver.Helpers;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile;

[AllowAnonymous]
public class InvolvingModel(
    ApplicationDbContext context,
    IAuthorizationService authorizationService,
    UserManager<InvolverUser> userManager,
    IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public class InvolvingViewModel
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public string ImageUrl { get; set; }
        public string LinkPage { get; set; }
        public string LinkID { get; set; }
        public int RecentValue { get; set; }
        public int MonthlyValue { get; set; }
        public int TotalValue { get; set; }
        public DateTime LastTime { get; set; }
    }

    public class InvolvingPageViewModel
    {
        public List<InvolvingViewModel> Involvings { get; set; }
        public string TimeSpan { get; set; }
    }

    public DataAccess.Models.Profile Profile { get; set; }
    public InvolvingPageViewModel PageViewModel { get; set; }

    private async Task<List<InvolvingViewModel>> GetInvolvings(string id, string timeSpan, int page = 1)
    {
        IQueryable<Involving> query = Context.Involvings
            .Include(i => i.Profile)
            .Include(i => i.Novel)
            .Include(i => i.Article)
            .Where(i => i.InvolverID == id);

        if (timeSpan == "TotalTime")
        {
            query = query.OrderByDescending(i => i.TotalValue);
        }
        else if (timeSpan == "Monthly")
        {
            query = query.OrderByDescending(i => i.MonthlyValue);
        }
        else
        {
            query = query.OrderByDescending(i => i.LastTime);
        }

        var involvings = await query
            .Skip((page - 1) * Parameters.PageSize)
            .Take(Parameters.PageSize)
            .AsNoTracking()
            .ToListAsync();

        var involvingViewModels = new List<InvolvingViewModel>();
        foreach (var involving in involvings)
        {
            var vm = new InvolvingViewModel
            {
                RecentValue = involving.Value,
                MonthlyValue = involving.MonthlyValue,
                TotalValue = involving.TotalValue,
                LastTime = involving.LastTime
            };

            if (involving.ProfileID != null && involving.Profile != null)
            {
                vm.Type = "創作者";
                vm.Title = involving.Profile.UserName;
                vm.LinkPage = "./Index";
                vm.LinkID = involving.ProfileID;
                vm.ImageUrl = involving.Profile.ImageUrl;
                if (string.IsNullOrEmpty(vm.ImageUrl))
                {
                    var user = await UserManager.FindByIdAsync(involving.ProfileID);
                    if (user != null) vm.ImageUrl = "https://www.gravatar.com/avatar/" + user.Email.ToMd5() + "?d=retro";
                }
            }
            else if (involving.NovelID != null && involving.Novel != null)
            {
                vm.Type = "創作";
                vm.Title = involving.Novel.Title;
                vm.LinkPage = "/Novels/Details";
                vm.LinkID = involving.NovelID.ToString();
            }
            else if (involving.ArticleID != null && involving.Article != null)
            {
                vm.Type = "文章";
                vm.Title = involving.Article.Title;
                vm.LinkPage = "/Articles/Details";
                vm.LinkID = involving.ArticleID.ToString();
            }

            involvingViewModels.Add(vm);
        }

        return involvingViewModels;
    }

    public async Task<IActionResult> OnGetAsync(string id, string timeSpan)
    {
        Profile = await Context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);
        if (Profile == null)
        {
            return NotFound();
        }

        var involvings = await GetInvolvings(id, timeSpan);
        PageViewModel = new InvolvingPageViewModel
        {
            Involvings = involvings,
            TimeSpan = timeSpan
        };

        return Page();
    }

    public async Task<IActionResult> OnGetLoadMoreAsync(string id, string timeSpan, int pageIndex)
    {
        var involvings = await GetInvolvings(id, timeSpan, pageIndex);
        var pageViewModel = new InvolvingPageViewModel
        {
            Involvings = involvings,
            TimeSpan = timeSpan
        };
        return Partial("_InvolvingListPartial", pageViewModel);
    }
}
