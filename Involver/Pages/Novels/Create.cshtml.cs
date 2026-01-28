using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;

using Involver.Authorization.Novel;
using Involver.Common;
using Involver.Helpers;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

using Type = DataAccess.Common.Type;

namespace Involver.Pages.Novels;

public class CreateModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    public IActionResult OnGet()
    {
        //ViewData["ProfileID"] = new SelectList(Context.Profiles, "ProfileID", "ProfileID");
        return Page();
    }

    [BindProperty]
    public Novel Novel { get; set; }

    [BindProperty]
    [Display(Name = "標籤")]
    [MaxLength(50)]
    public string TagString { get; set; }

    public string ErrorMessage { get; set; }

    // Use for upload file
    //[BindProperty]
    //public BufferedSingleFileUploadDb FileUpload { get; set; }

    public List<SelectListItem> Types { get; } =
    [
        new() { Value = Type.Fantasy.ToString(), Text = "奇幻" },
        new() { Value = Type.History.ToString(), Text = "歷史" },
        new() { Value = Type.Love.ToString(), Text = "愛情" },
        new() { Value = Type.Real.ToString(), Text = "真實" },
        new() { Value = Type.Modern.ToString(), Text = "現代" },
        new() { Value = Type.Science.ToString(), Text = "科幻" },
        new() { Value = Type.Horror.ToString(), Text = "驚悚" },
        new() { Value = Type.Detective.ToString(), Text = "推理" },
    ];

    // To protect from overposting attacks, please enable the specific properties you want to bind to, for
    // more details see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await UserManager.GetUserAsync(User);
        if (user.Banned)
        {
            return Forbid();
        }

        Novel.ProfileID = UserManager.GetUserId(User);

        var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                    User, Novel,
                                                    NovelOperations.Create);
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

        foreach (var tag in tagArr)
        {
            if (tag.Length > Parameters.TagNameMaxLength)
            {
                ErrorMessage = $"設定標籤長度超過{Parameters.TagNameMaxLength}個字，請重新設定";
                return Page();
            }
        }

        List<NovelTag> novelTags = [];

        foreach (var tag in tagArr)
        {
            if(string.IsNullOrEmpty(tag))
            {
                continue;
            }
            
            var existingTag = await Context.NovelTags.FirstOrDefaultAsync(t => t.Name == tag);

            if (existingTag != null)
            {
                novelTags.Add(existingTag);
            }
            else
            {
                NovelTag newTag = new()
                {
                    Name = tag
                };
                
                novelTags.Add(newTag);
            }
        }
        #endregion

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
            n => n.Title, n => n.Introduction, n => n.Type, n => n.PrimeRead, n => n.Block, n => n.ProfileID, n => n.ImageUrl))
        {
            //if (memoryStream.Length != 0)
            //{
            //    emptyNovel.Image = memoryStream.ToArray();
            //}
            emptyNovel.UpdateTime = DateTime.Now;
            emptyNovel.CreateTime = DateTime.Now;
            //var tempUser = await Context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Novel.ProfileID);
            //emptyNovel.ProfileID = Novel.ProfileID;

            emptyNovel.NovelTags = novelTags;

            Context.Novels.Add(emptyNovel);
            await Context.SaveChangesAsync();

            var toasts = await AchievementService.NovelCountAsync(Novel.ProfileID);

            if (novelTags.Count > 0)
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

internal class TagifyTag
{
    [JsonPropertyName("value")]
    public string Value { get; set; }
}