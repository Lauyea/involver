using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Involver.Data;
using Involver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Involver.Pages.Comments
{
    public class IndexModel : DI_BasePageModel
    {
        public IndexModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public PaginatedList<Comment> Comments { get;set; }

        public string CurrentFilter { get; set; }

        public async Task<IActionResult> OnGetAsync(
            string currentFilter,
            string searchString,
            int? PageIndex)
        {
            var isAuthorized = User.IsInRole(Authorization.Comment.Comments.CommentManagersRole) ||
                           User.IsInRole(Authorization.Comment.Comments.CommentAdministratorsRole);

            if (!isAuthorized)
            {
                return Forbid();
            }

            if (searchString != null)
            {
                PageIndex = 1;
            }
            else
            {
                searchString = currentFilter;
            }
            CurrentFilter = searchString;

            //IQueryable<Comment> comments = from c in Context.Comments
            //                               select c;
            IQueryable<Comment> comments = Context.Comments
            .Include(c => c.Announcement)
            .Include(c => c.Episode)
            .Include(c => c.Feedback)
            .Include(c => c.Novel)
            .Include(c => c.Profile)
            .Include(c => c.Article)
            .OrderBy(c => c.UpdateTime).AsQueryable();

            if (!String.IsNullOrEmpty(searchString))
            {
                comments = comments.Where(c => c.Content.Contains(searchString)
                                       || c.Profile.UserName.Contains(searchString));
            }

            int PageSize = 5;
            Comments = await PaginatedList<Comment>.CreateAsync(
                comments.AsNoTracking(), PageIndex ?? 1, PageSize);

            return Page();
        }
    }
}
