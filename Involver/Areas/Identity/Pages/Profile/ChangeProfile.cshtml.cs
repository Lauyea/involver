using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Involver.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Involver.Common;

namespace Involver.Areas.Identity.Pages.Profile
{
    [AllowAnonymous]
    public class ChangeProfileModel : DI_BasePageModel
    {
        public ChangeProfileModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        [BindProperty]
        public Models.Profile Profile { get; set; }
        //[BindProperty]
        //public BufferedSingleFileUploadDb FileUpload { get; set; }
        //[BindProperty]
        //public BufferedSingleFileUploadDb BannerUpload { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            Profile = await _context.Profiles.Where(p => p.ProfileID == id).FirstOrDefaultAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if(Profile.Introduction.Length > Parameters.ProfileIntroLength)
            {
                return Page();
            }

            Models.Profile ProfileToUpdate = await _context.Profiles.Where(p => p.ProfileID == _userManager.GetUserId(User)).FirstOrDefaultAsync();

            ProfileToUpdate.Introduction = Profile.Introduction;

            ProfileToUpdate.ImageUrl = Profile.ImageUrl;

            ProfileToUpdate.BannerImageUrl = Profile.BannerImageUrl;

            await _context.SaveChangesAsync();
            StatusMessage = "更改資料成功";

            var toasts = await Helpers.AchievementHelper.BeAutobiographerAsync(_context, ProfileToUpdate.ProfileID);

            Toasts.AddRange(toasts);

            ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

            return RedirectToPage("./Index", "OnGet", new { id = ProfileToUpdate.ProfileID });
        }
    }

    public class BufferedSingleFileUploadDb
    {
        [Display(Name = "上傳頭像圖片")]
        public IFormFile FormFile { get; set; }
    }
}
