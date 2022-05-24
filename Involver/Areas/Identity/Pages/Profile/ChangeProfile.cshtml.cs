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
        [BindProperty]
        public BufferedSingleFileUploadDb FileUpload { get; set; }
        [BindProperty]
        public BufferedSingleFileUploadDb BannerUpload { get; set; }

        public async Task<IActionResult> OnGetAsync(string id)
        {
            Profile = await Context.Profiles.Where(p => p.ProfileID == id).FirstOrDefaultAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            Models.Profile ProfileToUpdate = await Context.Profiles.Where(p => p.ProfileID == UserManager.GetUserId(User)).FirstOrDefaultAsync();
            ProfileToUpdate.Introduction = Profile.Introduction;

            using (var memoryStream = new MemoryStream())
            {
                if (FileUpload.FormFile != null)
                {
                    await FileUpload.FormFile.CopyToAsync(memoryStream);
                }

                // Upload the file if less than 260 KB
                if (memoryStream.Length < 262144)
                {
                    if (memoryStream.Length != 0)
                    {
                        ProfileToUpdate.Image = memoryStream.ToArray();
                    }
                }
                else
                {
                    ModelState.AddModelError("FileUpload", "圖片檔案必須小於260KB");
                    return Page();
                }
            }

            using (var memoryStream = new MemoryStream())
            {
                if (BannerUpload.FormFile != null)
                {
                    await BannerUpload.FormFile.CopyToAsync(memoryStream);
                }

                // Upload the file if less than 260 KB
                if (memoryStream.Length < 262144)
                {
                    if (memoryStream.Length != 0)
                    {
                        ProfileToUpdate.BannerImage = memoryStream.ToArray();
                    }
                }
                else
                {
                    ModelState.AddModelError("BannerUpload", "圖片檔案必須小於260KB");
                    return Page();
                }
            }

            Context.Attach(ProfileToUpdate).State = EntityState.Modified;
            await Context.SaveChangesAsync();
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
