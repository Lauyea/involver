using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Involver.Data;
using Involver.Models.ArticleModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Authorization.Article;
using Involver.Models;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Articles
{
    public class CreateModel : DI_BasePageModel
    {

        public CreateModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public Article Article { get; set; }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            //if(Article.Title == null || Article.Content == null)
            //{
            //    return Page();
            //}

            var user = await UserManager.GetUserAsync(User);
            if (user.Banned)
            {
                return Forbid();
            }

            Article.ProfileID = UserManager.GetUserId(User);

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                        User, Article,
                                                        ArticleOperations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            Article emptyArticle =
                new Article
                {
                    Title = "temp",
                    Content = "temp content post here.",
                    ProfileID = Article.ProfileID,
                    Comments = new List<Comment>
                {
                    //防止Comment找不到所屬的Article
                    new Comment
                    {
                        ProfileID = Article.ProfileID,
                        ArticleID = Article.ArticleID,
                        Block = true,
                        Content = "anchor"
                    }
                }
                };

            //Protect from overposting attacks
            if (await TryUpdateModelAsync<Article>(
                emptyArticle,
                "Article",   // Prefix for form value.
                f => f.Title, f => f.Content))
            {
                emptyArticle.UpdateTime = DateTime.Now;
                var tempUser = await Context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Article.ProfileID);
                emptyArticle.ProfileID = Article.ProfileID;
                emptyArticle.Views = 0;
                emptyArticle.Block = false;
                emptyArticle.TotalCoins = 0;
                emptyArticle.MonthlyCoins = 0;
                Context.Articles.Add(emptyArticle);
                await Context.SaveChangesAsync();

                return RedirectToPage("./Index");
            }

            return Page();
        }
    }
}
