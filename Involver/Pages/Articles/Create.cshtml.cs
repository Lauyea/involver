using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.ArticleModel;

using Involver.Authorization.Article;
using Involver.Common;
using Involver.Extensions;
using Involver.Models.ViewModels;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Articles;

public class CreateModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService,
ITagService tagService,
ICategoryService categoryService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        TagAuthorLevel = await tagService.GetAuthorLevelAsync(User, UserManager.GetUserId(User), cancellationToken);
        Categories = await categoryService.GetCategoriesAsync(TagTarget.Article, cancellationToken);
        return Page();
    }

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

    // To protect from overposting attacks, please enable the specific properties you want to bind to, for
    // more details see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
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

        if (await Context.IsBannedAsync(userId, cancellationToken))
        {
            return Forbid();
        }

        Article.ProfileID = userId;

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                    User, Article,
                                                    ArticleOperations.Create);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        var tagResult = await tagService.ResolveTagsAsync(
            TagString, TagTarget.Article, userId, TagAuthorLevel, cancellationToken: cancellationToken);
        if (!tagResult.Succeeded)
        {
            ErrorMessage = tagResult.ErrorMessage;
            return Page();
        }

        var categoryResult = await categoryService.SelectAsync(CategoryIds, tagResult.Tags, TagTarget.Article, required: false, cancellationToken);
        if (!categoryResult.Succeeded)
        {
            ErrorMessage = categoryResult.ErrorMessage;
            return Page();
        }

        // 一般標籤裡的分類已併入分類
        var allTags = tagResult.Tags.Where(t => !t.IsCategoryFor(CategoryScope.Article)).Concat(categoryResult.Categories).ToList();

        Article emptyArticle =
            new()
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

            emptyArticle.Tags = allTags;

            Context.Articles.Add(emptyArticle);
            await Context.SaveChangesAsync(cancellationToken);

            await tagService.PromotePendingTagsAsync(allTags.Select(t => t.TagId), cancellationToken);

            var toasts = await AchievementService.ArticleCountAsync(Article.ProfileID);

            if (tagResult.Tags.Count > 0)
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