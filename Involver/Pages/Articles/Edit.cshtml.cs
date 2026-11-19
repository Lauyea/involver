using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.AchievementModel;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Common;
using Involver.Models.ViewModels;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Articles;

public class EditModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService,
ITagService tagService,
ICategoryService categoryService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    [BindProperty]
    public Article Article { get; set; }

    /// <summary>
    /// Tagify 送出的標籤（JSON）
    /// </summary>
    [BindProperty]
    [Display(Name = "標籤")]
    [MaxLength(2000)]
    public string TagString { get; set; }

    public string ErrorMessage { get; set; }

    /// <summary>
    /// 使用者在標籤上的權限等級，決定標籤輸入框能不能直接新增標籤
    /// </summary>
    public TagAuthorLevel TagAuthorLevel { get; set; }

    /// <summary>
    /// 勾選的分類標籤 ID
    /// </summary>
    [BindProperty]
    public List<int> CategoryIds { get; set; } = [];

    /// <summary>
    /// 可選的分類
    /// </summary>
    public IReadOnlyList<CategoryOption> Categories { get; private set; } = [];

    /// <summary>
    /// 分類選擇（文章可以不選）
    /// </summary>
    public CategoryPickerViewModel CategoryPicker => new()
    {
        Options = Categories,
        SelectedIds = CategoryIds,
        Required = false,
    };

    /// <summary>
    /// 標籤輸入框
    /// </summary>
    public TagInputViewModel TagInput => new()
    {
        Value = TagString,
        Target = TagTarget.Article,
        Level = TagAuthorLevel,
    };

    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken cancellationToken)
    {
        if (id == null)
        {
            return NotFound();
        }

        Article = await Context.Articles
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.ArticleID == id, cancellationToken);

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

        // 分類在分類選擇裡顯示，標籤輸入框只放一般標籤
        var tags = Article.Tags.Where(t => !t.IsCategoryFor(CategoryScope.Article)).Select(t => new { value = t.Name });
        CategoryIds = Article.Tags.Where(t => t.IsCategoryFor(CategoryScope.Article)).Select(t => t.TagId).ToList();
        TagString = JsonSerializer.Serialize(tags);
        TagAuthorLevel = await tagService.GetAuthorLevelAsync(User, UserManager.GetUserId(User), cancellationToken);
        Categories = await categoryService.GetCategoriesAsync(TagTarget.Article, cancellationToken);

        return Page();
    }



    // To protect from overposting attacks, please enable the specific properties you want to bind to, for
    // more details see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        var userId = UserManager.GetUserId(User);
        TagAuthorLevel = await tagService.GetAuthorLevelAsync(User, userId, cancellationToken);
        Categories = await categoryService.GetCategoriesAsync(TagTarget.Article, cancellationToken);

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
            .Include(a => a.Tags)
            .FirstOrDefaultAsync(a => a.ArticleID == id, cancellationToken);

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

        var tagResult = await tagService.ResolveTagsAsync(
            TagString,
            TagTarget.Article,
            userId,
            TagAuthorLevel,
            articleToUpdate.Tags.Select(t => t.TagId).ToList(),
            cancellationToken);
        if (!tagResult.Succeeded)
        {
            ErrorMessage = tagResult.ErrorMessage;
            Article = articleToUpdate; // Re-assign for the view
            return Page();
        }

        var categoryResult = await categoryService.SelectAsync(CategoryIds, tagResult.Tags, TagTarget.Article, required: false, cancellationToken);
        if (!categoryResult.Succeeded)
        {
            ErrorMessage = categoryResult.ErrorMessage;
            Article = articleToUpdate; // Re-assign for the view
            return Page();
        }

        // 一般標籤裡的分類已併入分類
        var updatedTags = tagResult.Tags.Where(t => !t.IsCategoryFor(CategoryScope.Article)).Concat(categoryResult.Categories).ToList();

        if (await TryUpdateModelAsync<Article>(
            articleToUpdate,
            "article",
            a => a.Title, a => a.Content, a => a.Block, articleTags => articleTags.ImageUrl))
        {
            articleToUpdate.UpdateTime = DateTime.Now;

            articleToUpdate.Tags = updatedTags;

            try
            {
                await Context.SaveChangesAsync(cancellationToken);
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

            await tagService.PromotePendingTagsAsync(updatedTags.Select(t => t.TagId), cancellationToken);

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