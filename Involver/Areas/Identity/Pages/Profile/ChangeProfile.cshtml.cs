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

        [TempData]
        public string StatusMessage { get; set; }

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
            Models.Profile ProfileToUpdate = await _context.Profiles.Where(p => p.ProfileID == _userManager.GetUserId(User)).FirstOrDefaultAsync();

            ProfileToUpdate.Introduction = Profile.Introduction;

            ProfileToUpdate.ImageUrl = Profile.ImageUrl;

            ProfileToUpdate.BannerImageUrl = Profile.BannerImageUrl;

            _context.Attach(ProfileToUpdate).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            StatusMessage = "更改資料成功";
            return RedirectToPage("./Index", "OnGet", new { id = ProfileToUpdate.ProfileID });
        }
    }

    public class BufferedSingleFileUploadDb
    {
        [Display(Name = "上傳頭像圖片")]
        public IFormFile FormFile { get; set; }
    }
}
