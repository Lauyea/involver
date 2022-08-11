using Microsoft.AspNetCore.Mvc;
using Involver.Data;
using Involver.Models.ArticleModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Involver.Authorization.Article;
using Involver.Models;
using Microsoft.EntityFrameworkCore;
using Involver.Common;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

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
            if (!string.IsNullOrEmpty(ToastsJson))
            {
                Toasts = JsonSerializer.Deserialize<List<Toast>>(ToastsJson);
            }

            return Page();
        }

        [BindProperty]
        public Article Article { get; set; }

        [BindProperty]
        [Display(Name = "標籤")]
        [MaxLength(50)]
        public string TagString { get; set; }

        public string ErrorMessage { get; set; }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if(Article.Content.Length > Parameters.ArticleLength)
            {
                return Page();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            //if(Article.Title == null || Article.Content == null)
            //{
            //    return Page();
            //}

            var user = await _userManager.GetUserAsync(User);
            if (user.Banned)
            {
                return Forbid();
            }

            Article.ProfileID = _userManager.GetUserId(User);

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                        User, Article,
                                                        ArticleOperations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            #region 設定Tags
            var tagArr = TagString.Split(",").Select(t => t.Trim()).ToArray();

            if(tagArr.Length > Parameters.TagSize)
            {
                ErrorMessage = $"設定標籤超過{Parameters.TagSize}個，請重新設定";
                return Page();
            }

            foreach(var tag in tagArr)
            {
                if(tag.Length > Parameters.TagNameMaxLength)
                {
                    ErrorMessage = $"設定標籤長度超過{Parameters.TagNameMaxLength}個字，請重新設定";
                    return Page();
                }
            }

            List<ArticleTag> articleTags = new();

            foreach(var tag in tagArr)
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
            #endregion

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
                var tempUser = await _context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Article.ProfileID);
                emptyArticle.ProfileID = Article.ProfileID;
                emptyArticle.Views = 0;
                emptyArticle.Block = false;
                emptyArticle.TotalCoins = 0;
                emptyArticle.MonthlyCoins = 0;

                emptyArticle.ArticleTags = articleTags;

                _context.Articles.Add(emptyArticle);
                await _context.SaveChangesAsync();

                if (articleTags.Count > 0)
                {
                    var toasts = await Helpers.AchievementHelper.FirstTimeUseTagsAsync(_context, Article.ProfileID);

                    Toasts.AddRange(toasts);
                }

                ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

                return RedirectToPage("./Index");
            }

            return Page();
        }
    }
}
