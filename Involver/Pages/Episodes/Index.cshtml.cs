﻿using System;
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

namespace Involver.Pages.Episodes
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

        public IList<Episode> Episode { get;set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var isAuthorized = User.IsInRole(Authorization.Comment.Comments.CommentManagersRole) ||
                          User.IsInRole(Authorization.Comment.Comments.CommentAdministratorsRole);

            if (!isAuthorized)
            {
                return Forbid();
            }

            Episode = await Context.Episodes
                .Include(e => e.Novel).ToListAsync();

            return Page();
        }
    }
}
