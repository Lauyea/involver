using DataAccess.Common;
using DataAccess.Data;
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
    public class FollowCreatorsModel : DI_BasePageModel
    {
        public FollowCreatorsModel(
            ApplicationDbContext context,
            IAuthorizationService authorizationService,
            UserManager<InvolverUser> userManager)
            : base(context, authorizationService, userManager)
        {
        }

        public class FollowedCreatorViewModel
        {
            public string ProfileID { get; set; }
            public string UserName { get; set; }
            public string Introduction { get; set; }
            public string ImageUrl { get; set; }
            public bool IsMonthlyInvolver { get; set; }
            public DateTime FollowTime { get; set; }
        }

        public DataAccess.Models.Profile Profile { get; set; }
        public List<FollowedCreatorViewModel> Creators { get; set; }

        private async Task<List<FollowedCreatorViewModel>> GetFollowedCreators(string id, int page = 1)
        {
            var follows = await _context.Follows
                .Include(f => f.Profile)
                .Where(f => f.FollowerID == id && f.ProfileID != null)
                .OrderByDescending(f => f.UpdateTime)
                .Skip((page - 1) * Parameters.PageSize)
                .Take(Parameters.PageSize)
                .AsNoTracking()
                .ToListAsync();

            var creatorViewModels = new List<FollowedCreatorViewModel>();

            foreach (var follow in follows)
            {
                if (follow.Profile == null) continue;

                string imageUrl = follow.Profile.ImageUrl;
                if (string.IsNullOrEmpty(imageUrl))
                {
                    var user = await _userManager.FindByIdAsync(follow.ProfileID);
                    if (user != null)
                    {
                        imageUrl = "https://www.gravatar.com/avatar/" + user.Email.ToMd5() + "?d=retro";
                    }
                }

                creatorViewModels.Add(new FollowedCreatorViewModel
                {
                    ProfileID = follow.ProfileID,
                    UserName = follow.Profile.UserName,
                    Introduction = follow.Profile.Introduction,
                    ImageUrl = imageUrl,
                    IsMonthlyInvolver = follow.ProfileMonthlyInvolver,
                    FollowTime = follow.UpdateTime
                });
            }

            return creatorViewModels;
        }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            Profile = await _context.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.ProfileID == id);

            if (Profile == null)
            {
                return NotFound();
            }

            Creators = await GetFollowedCreators(id);

            return Page();
        }

        public async Task<IActionResult> OnGetLoadMoreAsync(string id, int pageIndex)
        {
            var creators = await GetFollowedCreators(id, pageIndex);
            return Partial("_FollowCreatorListPartial", creators);
        }
    }
}
