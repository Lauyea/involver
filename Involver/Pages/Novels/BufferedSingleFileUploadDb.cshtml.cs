using Involver.Common;
using Involver.Data;
using Involver.Models.NovelModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Involver.Pages.Novels
{
    //後來沒用到這個
    public class BufferedSingleFileUploadDbModel : DI_BasePageModel
    {
        public BufferedSingleFileUploadDbModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager)
        : base(context, authorizationService, userManager)
        {
        }

        [BindProperty]
        public Novel Novel { get; set; }

        [BindProperty]
        public BufferedSingleFileUploadDb FileUpload { get; set; }


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
           ViewData["ProfileID"] = new SelectList(Context.Profiles, "ProfileID", "ProfileID");
            return Page();
        }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            using (var memoryStream = new MemoryStream())
            {
                await FileUpload.FormFile.CopyToAsync(memoryStream);

                // Upload the file if less than 260 KB
                if (memoryStream.Length < 262144)
                {
                    Novel.Image = memoryStream.ToArray();

                    Context.Attach(Novel).State = EntityState.Modified;

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
                }
                else
                {
                    ModelState.AddModelError("File", "The file is too large.");
                }
            }

            return RedirectToPage("./Index");
        }

        private bool NovelExists(int id)
        {
            return Context.Novels.Any(e => e.NovelID == id);
        }
    }

    public class BufferedSingleFileUploadDb
    {
        [Display(Name = "上傳圖片")]
        public IFormFile FormFile { get; set; }
    }
}
