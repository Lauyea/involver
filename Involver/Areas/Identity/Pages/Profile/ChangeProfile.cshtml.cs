using System.ComponentModel.DataAnnotations;
using System.Text.Json;

using DataAccess.Common;
using DataAccess.Data;

using Involver.Common;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Involver.Areas.Identity.Pages.Profile;

[AllowAnonymous]
public class ChangeProfileModel(
ApplicationDbContext context,
IAuthorizationService authorizationService,
UserManager<InvolverUser> userManager,
IAchievementService achievementService) : DI_BasePageModel(context, authorizationService, userManager, achievementService)
{
    [BindProperty]
    public DataAccess.Models.Profile Profile { get; set; }
    //[BindProperty]
    //public BufferedSingleFileUploadDb FileUpload { get; set; }
    //[BindProperty]
    //public BufferedSingleFileUploadDb BannerUpload { get; set; }

    public async Task<IActionResult> OnGetAsync(string id)
    {
        Profile = await Context.Profiles.Where(p => p.ProfileID == id).FirstOrDefaultAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (Profile.Introduction.Length > Parameters.ProfileIntroLength)
        {
            return Page();
        }

        DataAccess.Models.Profile profileToUpdate = await Context.Profiles.Where(p => p.ProfileID == UserManager.GetUserId(User)).FirstOrDefaultAsync();

        profileToUpdate.Introduction = Profile.Introduction;

        profileToUpdate.ImageUrl = Profile.ImageUrl;

        profileToUpdate.BannerImageUrl = Profile.BannerImageUrl;

        await Context.SaveChangesAsync();
        StatusMessage = "更改資料成功";

        var toasts = await AchievementService.BeAutobiographerAsync(profileToUpdate.ProfileID);

        if (toasts.Count > 0)
        {
            TempData["Toasts"] = JsonSerializer.Serialize(toasts, JsonConfig.CamelCase);
        }

        return RedirectToPage("./Index", "OnGet", new { id = profileToUpdate.ProfileID });
    }
}

public class BufferedSingleFileUploadDb
{
    [Display(Name = "上傳頭像圖片")]
    public IFormFile FormFile { get; set; }
}