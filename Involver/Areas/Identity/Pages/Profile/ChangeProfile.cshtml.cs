using System.ComponentModel.DataAnnotations;

using DataAccess.Common;
using DataAccess.Data;

using Involver.Common;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
        public DataAccess.Models.Profile Profile { get; set; }
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
            if (Profile.Introduction.Length > Parameters.ProfileIntroLength)
            {
                return Page();
            }

            DataAccess.Models.Profile profileToUpdate = await _context.Profiles.Where(p => p.ProfileID == _userManager.GetUserId(User)).FirstOrDefaultAsync();

            profileToUpdate.Introduction = Profile.Introduction;

            profileToUpdate.ImageUrl = Profile.ImageUrl;

            profileToUpdate.BannerImageUrl = Profile.BannerImageUrl;

            await _context.SaveChangesAsync();
            StatusMessage = "更改資料成功";

            var toasts = await Helpers.AchievementHelper.BeAutobiographerAsync(_context, profileToUpdate.ProfileID);

            Toasts.AddRange(toasts);

            ToastsJson = System.Text.Json.JsonSerializer.Serialize(Toasts);

            return RedirectToPage("./Index", "OnGet", new { id = profileToUpdate.ProfileID });
        }
    }

    public class BufferedSingleFileUploadDb
    {
        [Display(Name = "上傳頭像圖片")]
        public IFormFile FormFile { get; set; }
    }
}