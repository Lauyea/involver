using Involver.Authorization.Article;
using Involver.Common;
using Involver.Data;
using Involver.Models.ArticleModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Articles
{
    public class EditModel : DI_BasePageModel
    {
        public EditModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        [BindProperty]
        public Article Article { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Article = await Context.Articles.FirstOrDefaultAsync(m => m.ArticleID == id);

            if (Article == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                  User, Article,
                                                  ArticleOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync(int id)
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Fetch data from DB to get OwnerID.
            Article articleToUpdate = await Context
                .Articles.AsNoTracking()
                .FirstOrDefaultAsync(a => a.ArticleID == id);

            if (articleToUpdate == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                  User, articleToUpdate,
                                                  ArticleOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            if (await TryUpdateModelAsync<Article>(
                articleToUpdate,
                "article",
                a => a.Title, a => a.Content, a => a.Block))
            {
                articleToUpdate.UpdateTime = DateTime.Now;

                Context.Attach(articleToUpdate).State = EntityState.Modified;

                try
                {
                    await Context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ArticleExists(Article.ArticleID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToPage("./Index");
            }


            //Article.OwnerID = articleToUpdate.OwnerID;
            //var tempUser = await Context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Article.OwnerID);
            //Article.OwnerName = tempUser.UserName;
            //Article.UpdateTime = DateTime.Now;
            //Article.Views = articleToUpdate.Views;
            //Article.Block = articleToUpdate.Block;
            //Article.TotalIncome = articleToUpdate.TotalIncome;
            //Article.MonthlyIncome = articleToUpdate.MonthlyIncome;

            //Context.Attach(Article).State = EntityState.Modified;

            return Page();
        }

        private bool ArticleExists(int id)
        {
            return Context.Articles.Any(e => e.ArticleID == id);
        }
    }
}
