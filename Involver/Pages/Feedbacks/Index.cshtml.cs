using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using DataAccess.Data;
using DataAccess.Models.FeedbackModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Common;

namespace Involver.Pages.Feedbacks
{
    [AllowAnonymous]
    public class IndexModel : DI_BasePageModel
    {
        public IndexModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public string SearchType { get; set; }

        public string CurrentType { get; set; }

        public string CurrentFilter { get; set; }

        public PaginatedList<Feedback> Feedbacks { get; set; }

        public async Task OnGetAsync(
            string currentType, 
            string searchType, 
            string currentFilter, 
            string searchString, 
            int? PageIndex)
        {
            if (searchString != null)
            {
                PageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
                searchType = currentType;
            }
            CurrentFilter = searchString;
            CurrentType = searchType;

            var feedbacks = from f in _context.Feedbacks
                            select f;

            if (!String.IsNullOrEmpty(searchString))
            {
                if(searchType == "OwnerName")
                {
                    feedbacks = feedbacks.Where(f => f.OwnerName.Contains(searchString));
                }
                else
                {
                    feedbacks = feedbacks.Where(f => f.Title.Contains(searchString));
                }
            }

            feedbacks = feedbacks.OrderByDescending(f => f.UpdateTime);

            var isAuthorized = User.IsInRole(Authorization.Feedback.Feedbacks.FeedbackManagersRole) ||
                           User.IsInRole(Authorization.Feedback.Feedbacks.FeedbackAdministratorsRole);

            var currentUserId = _userManager.GetUserId(User);

            // Only approved contacts are shown UNLESS you're authorized to see them
            // or you are the owner.
            if (!isAuthorized)
            {
                feedbacks = feedbacks.Where(f => f.Block == false
                                            || f.OwnerID == currentUserId);
            }

            
            Feedbacks = await PaginatedList<Feedback>.CreateAsync(
                feedbacks.AsNoTracking(), PageIndex ?? 1, Parameters.ArticlePageSize);

            if (!string.IsNullOrEmpty(ToastsJson))
            {
                Toasts = System.Text.Json.JsonSerializer.Deserialize<List<Toast>>(ToastsJson);
            }
        }
    }
}
