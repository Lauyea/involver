using Involver.Authorization.Article;
using Involver.Common;
using Involver.Data;
using Involver.Models.ArticleModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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

        [BindProperty]
        [Display(Name = "標籤")]
        [MaxLength(50)]
        public string TagString { get; set; }

        public string ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Article = await _context.Articles.Include(a => a.ArticleTags).FirstOrDefaultAsync(a => a.ArticleID == id);

            if (Article == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                  User, Article,
                                                  ArticleOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            string temp = string.Empty;

            foreach(var tag in Article.ArticleTags)
            {
                temp = tag.Name + ",";
                TagString += temp;
            }

            //移除最後一個頓號
            if(TagString != null)
            {
                TagString = TagString.Remove(TagString.Length - 1);
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
            Article articleToUpdate = await _context
                .Articles
                .Include(a => a.ArticleTags)
                .FirstOrDefaultAsync(a => a.ArticleID == id);

            if (articleToUpdate == null)
            {
                return NotFound();
            }

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                  User, articleToUpdate,
                                                  ArticleOperations.Update);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            //設定Tags
            var tagArr = TagString.Split(",").Select(t => t.Trim()).ToArray();

            if (tagArr.Length > 3)
            {
                ErrorMessage = "設定標籤超過三個，請重新設定";
                return Page();
            }

            foreach (var tag in tagArr)
            {
                if (tag.Length > 15)
                {
                    ErrorMessage = "設定標籤長度超過15個字，請重新設定";
                    return Page();
                }
            }

            List<ArticleTag> articleTags = new();

            foreach (var tag in tagArr)
            {
                var existingTag = await _context.ArticleTags.Where(t => t.Name == tag).FirstOrDefaultAsync();

                if (existingTag != null)
                {
                    articleTags.Add(existingTag);
                }
                else
                {
                    ArticleTag newTag = new ArticleTag
                    {
                        Name = tag
                    };

                    articleTags.Add(newTag);
                }
            }

            if (await TryUpdateModelAsync<Article>(
                articleToUpdate,
                "article",
                a => a.Title, a => a.Content, a => a.Block))
            {
                articleToUpdate.UpdateTime = DateTime.Now;

                articleToUpdate.ArticleTags = articleTags;

                _context.Attach(articleToUpdate).State = EntityState.Modified;

                try
                {
                    await _context.SaveChangesAsync();
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
            return _context.Articles.Any(e => e.ArticleID == id);
        }
    }
}
