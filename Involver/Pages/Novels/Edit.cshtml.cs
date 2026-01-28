using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models.NovelModel;

using Involver.Authorization.Novel;
using Involver.Common;
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
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    [BindProperty]
    public Novel Novel { get; set; }

    [BindProperty]
    [Display(Name = "標籤")]
    [MaxLength(200)]
    public string TagString { get; set; }

    public string ErrorMessage { get; set; }

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

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        Novel = await Context.Novels
            .Include(n => n.Profile)
            .Include(n => n.NovelTags)
            .FirstOrDefaultAsync(m => m.NovelID == id);

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

        var tags = Novel.NovelTags.Select(t => new { value = t.Name });
        TagString = JsonSerializer.Serialize(tags);

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
        Novel novelToUpdate = await Context
            .Novels
            .Include(n => n.NovelTags)
            .FirstOrDefaultAsync(n => n.NovelID == id);

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
            Novel = novelToUpdate; // Re-assign for the view
            return Page();
        }

        foreach (var tag in tagArr)
        {
            if (tag.Length > Parameters.TagNameMaxLength)
            {
                ErrorMessage = $"設定標籤長度超過{Parameters.TagNameMaxLength}個字，請重新設定";
                Novel = novelToUpdate; // Re-assign for the view
                return Page();
            }
        }

        var updatedTags = new List<NovelTag>();
        foreach (var tagName in tagArr)
        {
            if (string.IsNullOrEmpty(tagName))
            {
                continue;
            }

            var existingTag = await Context.NovelTags.FirstOrDefaultAsync(t => t.Name.ToUpper() == tagName.ToUpper());

            if (existingTag != null)
            {
                updatedTags.Add(existingTag);
            }
            else
            {
                var newTag = new NovelTag { Name = tagName };
                Context.NovelTags.Add(newTag); // Add to context to be saved
                updatedTags.Add(newTag);
            }
        }
        #endregion

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

            novelToUpdate.NovelTags = updatedTags;

            try
            {
                await Context.SaveChangesAsync();
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