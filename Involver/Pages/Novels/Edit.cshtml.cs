using Involver.Authorization.Novel;
using Involver.Common;
using Involver.Data;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Involver.Pages.Novels
{
    public class EditModel : DI_BasePageModel
    {
        public EditModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        [BindProperty]
        public Novel Novel { get; set; }

        public string ErrorMessage { get; set; }

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

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Novel = await Context.Novels
                .Include(n => n.Profile).FirstOrDefaultAsync(m => m.NovelID == id);

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
                .Novels.AsNoTracking()
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
                        novelToUpdate,
                        "Novel",   // Prefix for form value.
                        n => n.Introduction, n => n.Type, n => n.PrimeRead, n => n.End))
                    {
                        if (memoryStream.Length != 0)
                        {
                            novelToUpdate.Image = memoryStream.ToArray();
                        }
                        novelToUpdate.UpdateTime = DateTime.Now;

                        Context.Attach(novelToUpdate).State = EntityState.Modified;

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

                        return RedirectToPage("./Index");
                    }
                }
                else
                {
                    ModelState.AddModelError("FileUpload", "圖片檔案必須小於260KB");
                    ErrorMessage = "圖片檔案必須小於260KB";
                }
            }

            return Page();
        }

        private bool NovelExists(int id)
        {
            return Context.Novels.Any(e => e.NovelID == id);
        }
    }
}
