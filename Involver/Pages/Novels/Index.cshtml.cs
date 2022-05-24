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
using Involver.Common;

namespace Involver.Pages.Novels
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
        public string IncoinSort { get; set; }
        public string DateSort { get; set; }
        public string ViewSort { get; set; }
        public string CurrentFilter { get; set; }
        public string CurrentSort { get; set; }
        public PaginatedList<Novel> Novels { get; set; }

        public async Task OnGetAsync(
            string currentType,
            string searchType,
            string sortOrder,
            string currentFilter,
            string searchString, 
            int? PageIndex)
        {
            CurrentSort = sortOrder;
            DateSort = String.IsNullOrEmpty(sortOrder) ? "Date" : "";

            IncoinSort = sortOrder == "Incoin" ? "incoin_desc" : "Incoin";
            ViewSort = sortOrder == "View" ? "view_desc" : "View";

            if (searchType != null || searchString != null)
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

            IQueryable<Novel> NovelsIQ = from n in Context.Novels.Include("Profile")
                                         select n;

            if (!String.IsNullOrEmpty(searchString))
            {
                NovelsIQ = NovelsIQ.Where(n => n.Profile.UserName.Contains(searchString)
                                       || n.Title.Contains(searchString));
            }

            if (!String.IsNullOrEmpty(searchType))
            {
                switch (searchType)
                {
                    case "Fantasy":
                        NovelsIQ = NovelsIQ.Where(n => n.Type == Models.NovelModel.Type.Fantasy);
                        break;
                    case "History":
                        NovelsIQ = NovelsIQ.Where(n => n.Type == Models.NovelModel.Type.History);
                        break;
                    case "Love":
                        NovelsIQ = NovelsIQ.Where(n => n.Type == Models.NovelModel.Type.Love);
                        break;
                    case "Real":
                        NovelsIQ = NovelsIQ.Where(n => n.Type == Models.NovelModel.Type.Real);
                        break;
                    case "Modern":
                        NovelsIQ = NovelsIQ.Where(n => n.Type == Models.NovelModel.Type.Modern);
                        break;
                    case "Science":
                        NovelsIQ = NovelsIQ.Where(n => n.Type == Models.NovelModel.Type.Science);
                        break;
                    case "Horror":
                        NovelsIQ = NovelsIQ.Where(n => n.Type == Models.NovelModel.Type.Horror);
                        break;
                    case "Detective":
                        NovelsIQ = NovelsIQ.Where(n => n.Type == Models.NovelModel.Type.Detective);
                        break;
                    default:
                        break;
                }
            }

            switch (sortOrder)
            {
                case "Date":
                    NovelsIQ = NovelsIQ.OrderBy(s => s.UpdateTime);
                    break;
                case "incoin_desc":
                    NovelsIQ = NovelsIQ.OrderByDescending(s => s.MonthlyCoins);
                    break;
                case "Incoin":
                    NovelsIQ = NovelsIQ.OrderBy(s => s.MonthlyCoins);
                    break;
                case "view_desc":
                    NovelsIQ = NovelsIQ.OrderByDescending(s => s.Views);
                    break;
                case "View":
                    NovelsIQ = NovelsIQ.OrderBy(s => s.Views);
                    break;
                default:
                    NovelsIQ = NovelsIQ.OrderByDescending(s => s.UpdateTime);
                    break;
            }

            var isAuthorized = User.IsInRole(Authorization.Novel.Novels.NovelManagersRole) ||
                           User.IsInRole(Authorization.Novel.Novels.NovelAdministratorsRole);

            var currentUserId = UserManager.GetUserId(User);

            // Only approved contacts are shown UNLESS you're authorized to see them
            // or you are the owner.
            if (!isAuthorized)
            {
                NovelsIQ = NovelsIQ.Where(a => a.Block == false
                                            || a.ProfileID == currentUserId);
            }

            
            Novels = await PaginatedList<Novel>.CreateAsync(
                NovelsIQ.AsNoTracking(), PageIndex ?? 1, Parameters.ArticlePageSize);
        }
    }
}
