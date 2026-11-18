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

using Type = DataAccess.Common.Type;

namespace Involver.Pages.Novels;

public class EditModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService,
ITagService tagService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
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
    /// 標籤輸入框
    /// </summary>
    public TagInputViewModel TagInput => new()
    {
        Value = TagString,
        Target = TagTarget.Novel,
        Level = TagAuthorLevel,
    };

    public List<SelectListItem> Types { get; } = new List<SelectListItem>
    {
        new SelectListItem { Value = Type.Fantasy.ToString(), Text = "奇幻" },
        new SelectListItem { Value = Type.History.ToString(), Text = "歷史" },
        new SelectListItem { Value = Type.Love.ToString(), Text = "愛情" },
        new SelectListItem { Value = Type.Real.ToString(), Text = "真實" },
        new SelectListItem { Value = Type.Modern.ToString(), Text = "現代" },
        new SelectListItem { Value = Type.Science.ToString(), Text = "科幻" },
        new SelectListItem { Value = Type.Horror.ToString(), Text = "驚悚" },
        new SelectListItem { Value = Type.Detective.ToString(), Text = "推理" },
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

        var tags = Novel.Tags.Select(t => new { value = t.Name });
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

        var updatedTags = tagResult.Tags;

        //Protect from overposting attacks
        if (await TryUpdateModelAsync<Novel>(
            novelToUpdate,
            "Novel",   // Prefix for form value.
            n => n.Introduction, n => n.Type, n => n.PrimeRead, n => n.End, n => n.ImageUrl))
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

            var toasts = await AchievementService.FirstTimeEditAsync(Novel.ProfileID);

            if (updatedTags.Count > 0)
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

    private bool NovelExists(int id)
    {
        return Context.Novels.Any(e => e.NovelID == id);
    }
}