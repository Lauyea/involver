using Involver.Authorization.Novel;
using Involver.Common;
using Involver.Data;
using Involver.Models;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Involver.Pages.Novels
{
    public class CreateModel : DI_BasePageModel
    {

        public CreateModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        public IActionResult OnGet()
        {
        //ViewData["ProfileID"] = new SelectList(Context.Profiles, "ProfileID", "ProfileID");
            return Page();
        }

        [BindProperty]
        public Novel Novel { get; set; }

        [BindProperty]
        public BufferedSingleFileUploadDb FileUpload { get; set; }

        public List<SelectListItem> Types { get; } = new List<SelectListItem>
        {
            new SelectListItem { Value = Models.NovelModel.Type.Fantasy.ToString(), Text = "奇幻" },
            new SelectListItem { Value = Models.NovelModel.Type.History.ToString(), Text = "歷史" },
            new SelectListItem { Value = Models.NovelModel.Type.Love.ToString(), Text = "愛情" },
            new SelectListItem { Value = Models.NovelModel.Type.Real.ToString(), Text = "真實" },
            new SelectListItem { Value = Models.NovelModel.Type.Modern.ToString(), Text = "現代" },
            new SelectListItem { Value = Models.NovelModel.Type.Science.ToString(), Text = "科幻" },
            new SelectListItem { Value = Models.NovelModel.Type.Horror.ToString(), Text = "驚悚" },
            new SelectListItem { Value = Models.NovelModel.Type.Detective.ToString(), Text = "推理" },
        };

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user.Banned)
            {
                return Forbid();
            }

            Novel.ProfileID = _userManager.GetUserId(User);

            var isAuthorized = await _authorizationService.AuthorizeAsync(
                                                        User, Novel,
                                                        NovelOperations.Create);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }

            Novel emptyNovel =
                new Novel
                {
                    Title = "temp title",
                    Introduction = "temp introduction",
                    ProfileID = Novel.ProfileID,
                    Comments = new List<Comment>
                {
                    //防止Comment找不到所屬的Feedback
                    new Comment
                    {
                        ProfileID = Novel.ProfileID,
                        NovelID = Novel.NovelID,
                        Block = true,
                        Content = "anchor"
                    }
                },
                //    Episodes = new List<Episode>
                //{
                //    new Episode
                //    {
                //        Title = "anchor",
                //        NovelID = Novel.NovelID,
                //        Content = "anchor"
                //    }
                //}
                };

            using (var memoryStream = new MemoryStream())
            {
                if (FileUpload.FormFile != null)
                {
                    await FileUpload.FormFile.CopyToAsync(memoryStream);
                }
                
                // Upload the file if less than 260 KB
                if (memoryStream.Length < 262144)
                {
                    //Protect from overposting attacks
                    if (await TryUpdateModelAsync<Novel>(
                        emptyNovel,
                        "Novel",   // Prefix for form value.
                        n => n.Title, n => n.Introduction, n => n.Type, n => n.PrimeRead, n => n.Block, n => n.ProfileID))
                    {
                        if(memoryStream.Length != 0)
                        {
                            emptyNovel.Image = memoryStream.ToArray();
                        }
                        emptyNovel.UpdateTime = DateTime.Now;
                        emptyNovel.CreateTime = DateTime.Now;
                        //var tempUser = await Context.Profiles.FirstOrDefaultAsync(u => u.ProfileID == Novel.ProfileID);
                        //emptyNovel.ProfileID = Novel.ProfileID;
                        _context.Novels.Add(emptyNovel);
                        await _context.SaveChangesAsync();

                        return RedirectToPage("./Index");
                    }
                }
                else
                {
                    ModelState.AddModelError("FileUpload", "這個檔案太大了");
                }
            }

            return Page();
        }
    }
}
