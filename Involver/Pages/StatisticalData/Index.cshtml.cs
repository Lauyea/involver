using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Involver.Data;
using Involver.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.StatisticalData
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

        public List<Profile> Profiles { get; set; }

        public IActionResult OnGet()
        {
            Profiles = Context.Profiles
                .OrderByDescending(p => p.EnrollmentDate)
                .AsNoTracking()
                .ToList();
            return Page();
        }
    }
}
