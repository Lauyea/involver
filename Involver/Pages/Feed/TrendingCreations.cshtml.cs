using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Involver.Data;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Involver.Pages.Feed
{
    [AllowAnonymous]
    public class TrendingCreationsModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private ApplicationDbContext Context;

        public string UserProfileID;

        public ICollection<Novel> Novels { get; set; }

        public TrendingCreationsModel(ILogger<IndexModel> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            Context = context;
        }
        public async Task OnGetAsync(string id)
        {
            UserProfileID = id;
            Novels = await Context.Novels
                .Include(n => n.Profile)
                .OrderByDescending(n => n.MonthlyCoins)
                .Take(10)
                .ToListAsync();
        }
    }
}