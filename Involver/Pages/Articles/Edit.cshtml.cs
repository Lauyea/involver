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
ITagService tagService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
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

        var tags = Article.Tags.Select(t => new { value = t.Name });
        TagString = JsonSerializer.Serialize(tags);
        TagAuthorLevel = await tagService.GetAuthorLevelAsync(User, UserManager.GetUserId(User), cancellationToken);

        return Page();
    }



    // To protect from overposting attacks, please enable the specific properties you want to bind to, for
    // more details see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        var userId = UserManager.GetUserId(User);
        TagAuthorLevel = await tagService.GetAuthorLevelAsync(User, userId, cancellationToken);

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

        var updatedTags = tagResult.Tags;

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

            // 成就屬於執行操作的人：管理員編輯/刪除別人的內容時，不可替作者跳成就，否則 Toast 會顯示在管理員畫面上
            var actorId = UserManager.GetUserId(User);

            var toasts = await AchievementService.FirstTimeEditAsync(actorId);

            if (updatedTags.Count > 0)
            {
                toasts.AddRange(await AchievementService.FirstTimeUseTagsAsync(actorId));
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