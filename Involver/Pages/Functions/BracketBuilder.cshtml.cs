using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Functions
{
    /// <summary>
    /// 賽程表產生器：建立單淘汰賽程（種子排位、輪空、比分、季軍戰），並可匯出 HTML。
    /// 賽程完全在瀏覽器端計算，進度存在使用者的瀏覽器（localStorage）或匯出的 JSON 檔，不寫入資料庫。
    /// </summary>
    [AllowAnonymous]
    public class BracketBuilderModel : PageModel
    {
        /// <summary>
        /// 快速填入範例名單時可選的參賽人數。
        /// </summary>
        public IReadOnlyList<int> PresetSizes { get; } = [4, 8, 16, 32, 64];

        /// <summary>
        /// 顯示賽程表產生器頁面。
        /// </summary>
        public void OnGet()
        {
        }
    }
}
