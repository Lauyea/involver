using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Involver.Pages.Articles;

public class CreateModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public IActionResult OnGet()
    {
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
        if (Article.Content?.Length > Parameters.ArticleLength)
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

        #region 設定Tags
        var tagArr = Array.Empty<string>();

        if (!string.IsNullOrEmpty(TagString))
        {
            try
            {
                var tagObjects = JsonSerializer.Deserialize<List<TagifyTag>>(TagString);
                if (tagObjects != null)
                {
                    tagArr = tagObjects.Select(t => t.Value.Trim()).ToArray();
                }
            }
            catch (JsonException)
            {
                // Handle cases where the input is not valid JSON, could be a single tag or comma separated tags
                tagArr = TagString.Split(",").Select(t => t.Trim()).ToArray();
            }
        }

        if (tagArr.Length > Parameters.TagSize)
        {
            ErrorMessage = $"設定標籤超過{Parameters.TagSize}個，請重新設定";
            return Page();
        }

        List<ArticleTag> articleTags = [];

        if (!tagArr.IsNullOrEmpty())
        {
            foreach (var tag in tagArr)
            {
                if (tag.Length > Parameters.TagNameMaxLength)
                {
                    ErrorMessage = $"設定標籤長度超過{Parameters.TagNameMaxLength}個字，請重新設定";
                    return Page();
                }
            }

            foreach (var tag in tagArr)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    continue;
                }

                var existingTag = await Context.ArticleTags.FirstOrDefaultAsync(t => t.Name == tag);

                if (existingTag != null)
                {
                    articleTags.Add(existingTag);
                }
                else
                {
                    ArticleTag newTag = new()
                    {
                        Name = tag
                    };
                    
                    articleTags.Add(newTag);
                }
            }
        }
        
        #endregion

        Article emptyArticle =
            new Article
            {
                Title = "temp",
                Content = "temp content post here.",
                ProfileID = Article.ProfileID
            };

        //Protect from overposting attacks
        if (await TryUpdateModelAsync<Article>(
            emptyArticle,
            "Article",   // Prefix for form value.
            a => a.Title, a => a.Content, a => a.ImageUrl))
        {
            emptyArticle.UpdateTime = DateTime.Now;
            var tempUser = await Context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Article.ProfileID);
            emptyArticle.ProfileID = Article.ProfileID;
            emptyArticle.TotalViews = 0;
            emptyArticle.Block = false;
            emptyArticle.TotalCoins = 0;
            emptyArticle.MonthlyCoins = 0;

            emptyArticle.ArticleTags = articleTags;

            Context.Articles.Add(emptyArticle);
            await Context.SaveChangesAsync();

            var toasts = await AchievementService.ArticleCountAsync(Article.ProfileID);

            if (articleTags.Count > 0)
            {
                toasts.AddRange(await AchievementService.FirstTimeUseTagsAsync(Article.ProfileID));
            }

            if (toasts.Count > 0)
            {
                TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
            }

            return RedirectToPage("./Index");
        }

        return Page();
    }
}

internal class TagifyTag
{
    [JsonPropertyName("value")]
    public string Value { get; set; }
}