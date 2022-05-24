﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Involver.Data;
using Involver.Models.ArticleModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Common;

namespace Involver.Pages.Articles
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

        public PaginatedList<Article> Articles { get; set; }

        public string CurrentFilter { get; set; }

        public async Task OnGetAsync(
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
            }
            CurrentFilter = searchString;

            var articles = Context.Articles
                .Include(a => a.Comments)
                .Include(a => a.Profile)
                .AsQueryable();
            //var articles = from a in Context.Articles
            //               select a;
            articles = articles.OrderByDescending(a => a.UpdateTime);

            if (!String.IsNullOrEmpty(searchString))
            {
                articles = articles
                    .Where(a => a.Profile.UserName.Contains(searchString) || a.Title.Contains(searchString));
            }


            var isAuthorized = User.IsInRole(Authorization.Feedback.Feedbacks.FeedbackManagersRole) ||
                           User.IsInRole(Authorization.Feedback.Feedbacks.FeedbackAdministratorsRole);

            var currentUserId = UserManager.GetUserId(User);

            // Only approved contacts are shown UNLESS you're authorized to see them
            // or you are the owner.
            if (!isAuthorized)
            {
                articles = articles.Where(a => a.Block == false
                                            || a.ProfileID == currentUserId);
            }

            int PageSize = 5;
            Articles = await PaginatedList<Article>.CreateAsync(
                articles.AsNoTracking(), PageIndex ?? 1, PageSize);
        }
    }
}
