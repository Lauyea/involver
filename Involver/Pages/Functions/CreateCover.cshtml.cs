using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

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
        [Required(ErrorMessage = "請選擇要上傳的背景圖片。")]
        [Display(Name = "背景圖片")]
        public IFormFile Upload { get; set; }

        [BindProperty]
        [Display(Name = "系列名稱")]
        public string BrandText { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "請輸入主標題。")]
        [Display(Name = "主標題")]
        public string Title { get; set; }

        [BindProperty]
        [Display(Name = "副標題")]
        public string SubTitle { get; set; }

        public void OnGet()
        {
            // 頁面初次載入時執行的程式碼
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                // This is not ideal for AJAX, but as a fallback.
                return Page();
            }

            // 1. 處理上傳的圖片，並儲存到伺服器暫存區
            var tempFileName = $"{Guid.NewGuid()}{Path.GetExtension(Upload.FileName)}";
            var tempBgImagePath = Path.Combine(_environment.WebRootPath, "temp", tempFileName);

            // 確保暫存資料夾存在
            Directory.CreateDirectory(Path.GetDirectoryName(tempBgImagePath));

            using (var stream = new FileStream(tempBgImagePath, FileMode.Create))
            {
                await Upload.CopyToAsync(stream);
            }

            // 2. 準備 CoverMaker 所需的參數
            var fontPath = Path.Combine(_environment.WebRootPath, "fonts", "NotoSansTC-Bold.ttf");
            if (!System.IO.File.Exists(fontPath))
            {
                return new JsonResult(new { success = false, error = "字型檔案遺失。" });
            }

            var processedSubTitle = (SubTitle ?? string.Empty).Replace("\\n", Environment.NewLine);
            var outputFileName = $"cover_{DateTime.Now:yyyyMMddHHmmss}.png";
            var outputFilePath = Path.Combine(_environment.WebRootPath, "temp", outputFileName);

            try
            {
                // 3. 執行封面製作
                var maker = new CoverMaker(fontPath, fontPath); // 主標題和副標題使用相同字體
                maker.Generate(tempBgImagePath, BrandText, Title, processedSubTitle, outputFilePath);

                // 4. 將產生的圖檔讀取為 byte array，準備回傳給使用者
                var fileBytes = await System.IO.File.ReadAllBytesAsync(outputFilePath);
                var fileContents = Convert.ToBase64String(fileBytes);

                // 5. 清理伺服器上的暫存檔案
                System.IO.File.Delete(tempBgImagePath);
                System.IO.File.Delete(outputFilePath);

                return new JsonResult(new { success = true, fileName = outputFileName, fileContents });
            }
            catch (Exception ex)
            {
                // 發生錯誤時，也要清理暫存檔
                if (System.IO.File.Exists(tempBgImagePath)) System.IO.File.Delete(tempBgImagePath);
                if (System.IO.File.Exists(outputFilePath)) System.IO.File.Delete(outputFilePath);

                return new JsonResult(new { success = false, error = $"產生封面時發生錯誤: {ex.Message}" });
            }
        }
    }
}