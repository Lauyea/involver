using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using Involver.Common;
using Involver.Extensions;
using Involver.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile
{
    [AllowAnonymous]
    public class InvolversModel : DI_BasePageModel
    {
        public InvolversModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            UserManager<InvolverUser> userManager)
            : base(context, authorizationService, userManager)
        {
        }

        public class InvolverViewModel
        {
            public string ProfileID { get; set; }
            public string UserName { get; set; }
            public string ImageUrl { get; set; }
            public int RecentValue { get; set; }
            public int MonthlyValue { get; set; }
            public int TotalValue { get; set; }
            public DateTime LastTime { get; set; }
        }

        public class InvolverPageViewModel
        {
            public List<InvolverViewModel> Involvers { get; set; }
            public string TimeSpan { get; set; }
        }

        public DataAccess.Models.Profile Profile { get; set; }
        public InvolverPageViewModel PageViewModel { get; set; }

        private async Task<List<InvolverViewModel>> GetInvolvers(string id, string timeSpan, int page = 1)
        {
            IQueryable<Involving> query = _context.Involvings
                .Include(i => i.Involver)
                .Where(i => i.ProfileID == id);

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

            var involvers = await query
                .Skip((page - 1) * Parameters.PageSize)
                .Take(Parameters.PageSize)
                .AsNoTracking()
                .ToListAsync();

            var involverViewModels = new List<InvolverViewModel>();
            foreach (var involving in involvers)
            {
                if (involving.Involver == null) continue;

                var vm = new InvolverViewModel
                {
                    ProfileID = involving.InvolverID,
                    UserName = involving.Involver.UserName,
                    RecentValue = involving.Value,
                    MonthlyValue = involving.MonthlyValue,
                    TotalValue = involving.TotalValue,
                    LastTime = involving.LastTime,
                    ImageUrl = involving.Involver.ImageUrl
                };

                if (string.IsNullOrEmpty(vm.ImageUrl))
                {
                    var user = await _userManager.FindByIdAsync(involving.InvolverID);
                    if (user != null) vm.ImageUrl = "https://www.gravatar.com/avatar/" + user.Email.ToMd5() + "?d=retro";
                }

                involverViewModels.Add(vm);
            }

            return involverViewModels;
        }

        public async Task<IActionResult> OnGetAsync(string id, string timeSpan)
        {
            Profile = await _context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);
            if (Profile == null)
            {
                return NotFound();
            }

            var involvers = await GetInvolvers(id, timeSpan);
            PageViewModel = new InvolverPageViewModel
            {
                Involvers = involvers,
                TimeSpan = timeSpan
            };

            return Page();
        }

        public async Task<IActionResult> OnGetLoadMoreAsync(string id, string timeSpan, int pageIndex)
        {
            var involvers = await GetInvolvers(id, timeSpan, pageIndex);
            var pageViewModel = new InvolverPageViewModel
            {
                Involvers = involvers,
                TimeSpan = timeSpan
            };
            return Partial("_InvolverListPartial", pageViewModel);
        }
    }
}
