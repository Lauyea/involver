using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Involver.Data;
using Involver.Models;
using Involver.Models.ArticleModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Involver.Pages.Feed
{
    public class FollowArticlesModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private ApplicationDbContext Context;

        public string UserProfileID;

        public ICollection<Follow> Follows { get; set; }

        public ICollection<Article> Articles { get; set; } = new List<Article>();

        public FollowArticlesModel(ILogger<IndexModel> logger,
            ApplicationDbContext context)
        {
            _logger = logger;
            Context = context;
        }

        public async Task OnGetAsync(string id)
        {
            UserProfileID = id;
            Follows = await Context.Follows
                .Include(f => f.Profile)
                    .ThenInclude(p => p.Articles)
                .Where(f => f.FollowerID == id)
                .ToListAsync();
            foreach (Follow follow in Follows)
            {
                if (follow.Profile != null)
                {
                    foreach (Article article in follow.Profile.Articles)
                    {
                        Articles.Add(article);
                    }
                }
            }
            Articles.OrderByDescending(a => a.UpdateTime).Take(100);
        }
    }
}