using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.AchievementModel;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Common;
using Involver.Helpers;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Involver.Pages.Articles;

public class EditModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    [BindProperty]
    public Article Article { get; set; }

    [BindProperty]
    [Display(Name = "標籤")]
    [MaxLength(200)]
    public string TagString { get; set; }

    public string ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Article = await Context.Articles.Include(a => a.ArticleTags).FirstOrDefaultAsync(a => a.ArticleID == id);

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

        var tags = Article.ArticleTags.Select(t => new { value = t.Name });
        TagString = JsonSerializer.Serialize(tags);

        return Page();
    }



    // To protect from overposting attacks, please enable the specific properties you want to bind to, for
    // more details see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (Article.Content?.Length > Parameters.ArticleLength)
        {
            return Page();
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Fetch data from DB to get OwnerID.
        Article articleToUpdate = await Context
            .Articles
            .Include(a => a.ArticleTags)
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
            Article = articleToUpdate; // Re-assign for the view
            return Page();
        }

        foreach (var tag in tagArr)
        {
            if (tag.Length > Parameters.TagNameMaxLength)
            {
                ErrorMessage = $"設定標籤長度超過{Parameters.TagNameMaxLength}個字，請重新設定";
                Article = articleToUpdate; // Re-assign for the view
                return Page();
            }
        }

        var updatedTags = new List<ArticleTag>();
        if (!tagArr.IsNullOrEmpty())
        {
            foreach (var tagName in tagArr)
            {
                if (string.IsNullOrEmpty(tagName))
                {
                    continue;
                }

                var existingTag = await Context.ArticleTags.FirstOrDefaultAsync(t => t.Name.ToUpper() == tagName.ToUpper());

                if (existingTag != null)
                {
                    updatedTags.Add(existingTag);
                }
                else
                {
                    var newTag = new ArticleTag { Name = tagName };
                    Context.ArticleTags.Add(newTag); // Add to context to be saved
                    updatedTags.Add(newTag);
                }
            }
        }
        #endregion

        if (await TryUpdateModelAsync<Article>(
            articleToUpdate,
            "article",
            a => a.Title, a => a.Content, a => a.Block, articleTags => articleTags.ImageUrl))
        {
            articleToUpdate.UpdateTime = DateTime.Now;

            articleToUpdate.ArticleTags = updatedTags;

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

            var toasts = await AchievementService.FirstTimeEditAsync(Article.ProfileID);

            if (updatedTags.Count > 0)
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

    private bool ArticleExists(int id)
    {
        return Context.Articles.Any(e => e.ArticleID == id);
    }
}