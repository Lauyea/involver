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

using Involver.Extensions;

namespace Involver.Pages.Novels;

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
        Categories = await categoryService.GetCategoriesAsync(TagTarget.Novel, cancellationToken);
        return Page();
    }

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

    // Use for upload file
    //[BindProperty]
    //public BufferedSingleFileUploadDb FileUpload { get; set; }


    // To protect from overposting attacks, please enable the specific properties you want to bind to, for
    // more details see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var userId = UserManager.GetUserId(User);
        TagAuthorLevel = await tagService.GetAuthorLevelAsync(User, userId, cancellationToken);
        Categories = await categoryService.GetCategoriesAsync(TagTarget.Novel, cancellationToken);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (await Context.IsBannedAsync(userId, cancellationToken))
        {
            return Forbid();
        }

        Novel.ProfileID = userId;

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                    User, Novel,
                                                    NovelOperations.Create);
        if (!isAuthorized.Succeeded)
        {
            return Forbid();
        }

        var tagResult = await tagService.ResolveTagsAsync(
            TagString, TagTarget.Novel, userId, TagAuthorLevel, cancellationToken: cancellationToken);
        if (!tagResult.Succeeded)
        {
            ErrorMessage = tagResult.ErrorMessage;
            return Page();
        }

        var categoryResult = await categoryService.SelectAsync(CategoryIds, tagResult.Tags, TagTarget.Novel, required: true, cancellationToken);
        if (!categoryResult.Succeeded)
        {
            ErrorMessage = categoryResult.ErrorMessage;
            return Page();
        }

        // 一般標籤裡的分類已併入分類
        var allTags = tagResult.Tags.Where(t => !t.IsCategoryFor(CategoryScope.Novel)).Concat(categoryResult.Categories).ToList();

        Novel emptyNovel =
            new()
            {
                Title = "temp title",
                Introduction = "temp introduction",
                ProfileID = Novel.ProfileID
            };

        // Code below use for upload file
        //using (var memoryStream = new MemoryStream())
        //{
        //    if (FileUpload.FormFile != null)
        //    {
        //        await FileUpload.FormFile.CopyToAsync(memoryStream);
        //    }

        //    // Upload the file if less than 260 KB
        //    if (memoryStream.Length < 262144)
        //    {

        //    }
        //    else
        //    {
        //        ModelState.AddModelError("FileUpload", "這個檔案太大了");
        //    }
        //}

        //Protect from overposting attacks
        if (await TryUpdateModelAsync<Novel>(
            emptyNovel,
            "Novel",   // Prefix for form value.
            n => n.Title, n => n.Introduction, n => n.PrimeRead, n => n.Block, n => n.ProfileID, n => n.ImageUrl))
        {
            //if (memoryStream.Length != 0)
            //{
            //    emptyNovel.Image = memoryStream.ToArray();
            //}
            emptyNovel.UpdateTime = DateTime.Now;
            emptyNovel.CreateTime = DateTime.Now;
            //var tempUser = await Context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Novel.ProfileID);
            //emptyNovel.ProfileID = Novel.ProfileID;

            emptyNovel.Tags = allTags;

            Context.Novels.Add(emptyNovel);
            await Context.SaveChangesAsync(cancellationToken);

            await tagService.PromotePendingTagsAsync(allTags.Select(t => t.TagId), cancellationToken);

            var toasts = await AchievementService.NovelCountAsync(Novel.ProfileID);

            if (tagResult.Tags.Count > 0)
            {
                toasts.AddRange(await AchievementService.FirstTimeUseTagsAsync(Novel.ProfileID));
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