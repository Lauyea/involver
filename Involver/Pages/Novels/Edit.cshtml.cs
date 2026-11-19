using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.NovelModel;

using Involver.Authorization.Novel;
using Involver.Common;
using Involver.Models.ViewModels;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;


namespace Involver.Pages.Novels;

public class EditModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService,
ITagService tagService,
ICategoryService categoryService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    [BindProperty]
    public Novel Novel { get; set; }

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
    /// 分類選擇（小說至少選一個）
    /// </summary>
    public CategoryPickerViewModel CategoryPicker => new()
    {
        Options = Categories,
        SelectedIds = CategoryIds,
        Required = true,
    };

    /// <summary>
    /// 標籤輸入框
    /// </summary>
    public TagInputViewModel TagInput => new()
    {
        Value = TagString,
        Target = TagTarget.Novel,
        Level = TagAuthorLevel,
    };


    public async Task<IActionResult> OnGetAsync(int? id, CancellationToken cancellationToken)
    {
        if (id == null)
        {
            return NotFound();
        }

        Novel = await Context.Novels
            .Include(n => n.Profile)
            .Include(n => n.Tags)
            .FirstOrDefaultAsync(m => m.NovelID == id, cancellationToken);

        if (Novel == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                              User, Novel,
                                              NovelOperations.Update);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        // 分類在分類選擇裡顯示，標籤輸入框只放一般標籤
        var tags = Novel.Tags.Where(t => !t.IsCategoryFor(CategoryScope.Novel)).Select(t => new { value = t.Name });
        CategoryIds = Novel.Tags.Where(t => t.IsCategoryFor(CategoryScope.Novel)).Select(t => t.TagId).ToList();
        TagString = JsonSerializer.Serialize(tags);
        TagAuthorLevel = await tagService.GetAuthorLevelAsync(User, UserManager.GetUserId(User), cancellationToken);
        Categories = await categoryService.GetCategoriesAsync(TagTarget.Novel, cancellationToken);

        return Page();
    }



    // To protect from overposting attacks, please enable the specific properties you want to bind to, for
    // more details see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        var userId = UserManager.GetUserId(User);
        TagAuthorLevel = await tagService.GetAuthorLevelAsync(User, userId, cancellationToken);
        Categories = await categoryService.GetCategoriesAsync(TagTarget.Novel, cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Fetch data from DB to get OwnerID.
        Novel novelToUpdate = await Context
            .Novels
            .Include(n => n.Tags)
            .FirstOrDefaultAsync(n => n.NovelID == id, cancellationToken);

        if (novelToUpdate == null)
        {
            return NotFound();
        }

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                              User, novelToUpdate,
                                              NovelOperations.Update);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        var tagResult = await tagService.ResolveTagsAsync(
            TagString,
            TagTarget.Novel,
            userId,
            TagAuthorLevel,
            novelToUpdate.Tags.Select(t => t.TagId).ToList(),
            cancellationToken);
        if (!tagResult.Succeeded)
        {
            ErrorMessage = tagResult.ErrorMessage;
            Novel = novelToUpdate; // Re-assign for the view
            return Page();
        }

        var categoryResult = await categoryService.SelectAsync(CategoryIds, tagResult.Tags, TagTarget.Novel, required: true, cancellationToken);
        if (!categoryResult.Succeeded)
        {
            ErrorMessage = categoryResult.ErrorMessage;
            Novel = novelToUpdate; // Re-assign for the view
            return Page();
        }

        // 一般標籤裡的分類已併入分類
        var updatedTags = tagResult.Tags.Where(t => !t.IsCategoryFor(CategoryScope.Novel)).Concat(categoryResult.Categories).ToList();

        //Protect from overposting attacks
        if (await TryUpdateModelAsync<Novel>(
            novelToUpdate,
            "Novel",   // Prefix for form value.
            n => n.Introduction, n => n.PrimeRead, n => n.End, n => n.ImageUrl))
        {
            //if (memoryStream.Length != 0)
            //{
            //    novelToUpdate.Image = memoryStream.ToArray();
            //}
            novelToUpdate.UpdateTime = DateTime.Now;

            novelToUpdate.Tags = updatedTags;

            try
            {
                await Context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!NovelExists(Novel.NovelID))
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

    private bool NovelExists(int id)
    {
        return Context.Novels.Any(e => e.NovelID == id);
    }
}