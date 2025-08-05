using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System;
using Microsoft.AspNetCore.Hosting;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Involver.Pages.Functions
{
    public class CreateCoverModel : PageModel
    {
        private readonly IWebHostEnvironment _environment;

        public CreateCoverModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [BindProperty]
        [Required(ErrorMessage = "�п�ܭn�W�Ǫ��I���Ϥ��C")]
        [Display(Name = "�I���Ϥ�")]
        public IFormFile Upload { get; set; }

        [BindProperty]
        [Display(Name = "�~�P��r")]
        public string BrandText { get; set; } = "involver";

        [BindProperty]
        [Required(ErrorMessage = "�п�J�D���D�C")]
        [Display(Name = "�D���D")]
        public string Title { get; set; }

        [BindProperty]
        [Display(Name = "�Ƽ��D")]
        public string SubTitle { get; set; }

        public void OnGet()
        {
            // �����즸���J�ɰ��檺�{���X
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            // 1. �B�z�W�Ǫ��Ϥ��A���x�s����A���Ȧs��
            var tempFileName = $"{Guid.NewGuid()}{Path.GetExtension(Upload.FileName)}";
            var tempBgImagePath = Path.Combine(_environment.WebRootPath, "temp", tempFileName);

            // �T�O�Ȧs��Ƨ��s�b
            Directory.CreateDirectory(Path.GetDirectoryName(tempBgImagePath));

            using (var stream = new FileStream(tempBgImagePath, FileMode.Create))
            {
                await Upload.CopyToAsync(stream);
            }

            // 2. �ǳ� CoverMaker �һݪ��Ѽ�
            var fontPath = Path.Combine(_environment.WebRootPath, "fonts", "NotoSansTC-Bold.ttf");
            if (!System.IO.File.Exists(fontPath))
            {
                ModelState.AddModelError(string.Empty, "���~�G���A���W�䤣��r���ɮ� NotoSansTC-Bold.ttf�C");
                return Page();
            }

            var processedSubTitle = (SubTitle ?? string.Empty).Replace("\\n", Environment.NewLine);
            var outputFileName = $"cover_{DateTime.Now:yyyyMMddHHmmss}.png";
            var outputFilePath = Path.Combine(_environment.WebRootPath, "temp", outputFileName);

            try
            {
                // 3. ����ʭ��s�@
                var maker = new CoverMaker(fontPath, fontPath); // �D���D�M�Ƽ��D�ϥάۦP�r��
                maker.Generate(tempBgImagePath, BrandText, Title, processedSubTitle, outputFilePath);

                // 4. �N���ͪ�����Ū���� byte array�A�ǳƦ^�ǵ��ϥΪ�
                var fileBytes = await System.IO.File.ReadAllBytesAsync(outputFilePath);

                // 5. �M�z���A���W���Ȧs�ɮ�
                System.IO.File.Delete(tempBgImagePath);
                System.IO.File.Delete(outputFilePath);

                // 6. �N�ɮצ^�ǵ��s�����U��
                return File(fileBytes, "image/png", outputFileName);
            }
            catch (Exception ex)
            {
                // �o�Ϳ��~�ɡA�]�n�M�z�Ȧs��
                if (System.IO.File.Exists(tempBgImagePath)) System.IO.File.Delete(tempBgImagePath);
                if (System.IO.File.Exists(outputFilePath)) System.IO.File.Delete(outputFilePath);

                ModelState.AddModelError(string.Empty, $"�ʭ��s�@���ѡG{ex.Message}");
                return Page();
            }
        }
    }
}