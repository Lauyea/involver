using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Functions
{
    [AllowAnonymous]
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
            // 使用系統臨時目錄以避免在受限的 WebRoot/容器檔案系統寫入失敗
            var tempDir = Path.Combine(Path.GetTempPath(), "InvolverTemp");
            Directory.CreateDirectory(tempDir);

            var tempFileName = $"{Guid.NewGuid()}{Path.GetExtension(Upload.FileName)}";
            var tempBgImagePath = Path.Combine(tempDir, tempFileName);

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
            var outputFilePath = Path.Combine(tempDir, outputFileName);

            try
            {
                // 3. 執行封面製作
                using var maker = new CoverMaker(fontPath, fontPath); // 主標題和副標題使用相同字體
                maker.Generate(tempBgImagePath, BrandText, Title, processedSubTitle, outputFilePath);

                // 4. 將產生的圖檔讀取為 byte array，準備回傳給使用者作為檔案下載
                var fileBytes = await System.IO.File.ReadAllBytesAsync(outputFilePath);

                // 5. 清理伺服器上的暫存檔案
                if (System.IO.File.Exists(tempBgImagePath)) System.IO.File.Delete(tempBgImagePath);
                if (System.IO.File.Exists(outputFilePath)) System.IO.File.Delete(outputFilePath);

                // 直接回傳檔案供下載
                return File(fileBytes, "image/png", outputFileName);
            }
            catch (Exception ex)
            {
                // 發生錯誤時，也要清理暫存檔
                if (System.IO.File.Exists(tempBgImagePath)) System.IO.File.Delete(tempBgImagePath);
                if (System.IO.File.Exists(outputFilePath)) System.IO.File.Delete(outputFilePath);

                // 回傳 400 與錯誤訊息
                return BadRequest($"產生封面時發生錯誤: {ex.Message}");
            }
        }
    }
}