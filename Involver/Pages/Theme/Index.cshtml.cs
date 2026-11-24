using DataAccess.Common;

using Involver.Models.ViewModels;

using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Involver.Pages.Theme
{
    /// <summary>
    /// 主題檢查頁：列出 site.css 的 Material Design 3 色彩角色、Bootstrap 對應與共用元件，
    /// 修改主題色後用來檢查淺色、深色模式的顯示與對比
    /// </summary>
    public class IndexModel : PageModel
    {
        /// <summary>
        /// 強調色角色（每組含底色、on 前景色、容器色與容器上的前景色）
        /// </summary>
        public IReadOnlyList<ColorRoleGroup> AccentRoles { get; } =
        [
            new("primary", "主色", "連結、勾選框、有意義的圖示；實心按鈕與選中狀態用 primary-fill", "primary"),
            new("secondary", "次要色", "接近中性的暖灰：低調的 tonal 按鈕、選單與清單的選中狀態", "secondary"),
            new("tertiary", "第三色", "和主色互補的藍綠，對應 Bootstrap 的 info", "info"),
            new("error", "錯誤", "錯誤、刪除等破壞性操作，對應 Bootstrap 的 danger", "danger"),
            new("success", "成功", "M3 自訂色，對應 Bootstrap 的 success", "success"),
            new("warning", "警告", "M3 自訂色，對應 Bootstrap 的 warning", "warning"),
        ];

        /// <summary>
        /// 表面角色（由低到高），文字一律用 on-surface 或 on-surface-variant
        /// </summary>
        public IReadOnlyList<SurfaceRole> SurfaceRoles { get; } =
        [
            new("surface", "頁面背景", "--bg-color"),
            new("surface-container-lowest", "卡片（淺色模式）、輸入框", "--card-bg-color"),
            new("surface-container-low", "卡片標頭、次要區塊", null),
            new("surface-container", "下拉選單、麵包屑、卡片（深色模式）", null),
            new("surface-container-high", "對話框、標籤底色", null),
            new("surface-container-highest", "進度條軌道", null),
        ];

        /// <summary>
        /// Bootstrap 語意色名稱，用來產生按鈕、alert 與 badge 的範例
        /// </summary>
        public IReadOnlyList<string> BootstrapVariants { get; } =
            ["primary", "secondary", "info", "success", "warning", "danger"];

        /// <summary>
        /// 分類 chip 的範例資料
        /// </summary>
        public CategoryPickerViewModel CategoryPicker { get; } = new()
        {
            Name = "ThemeCategoryIds",
            Max = 3,
            Options =
            [
                new(1, "奇幻", "fa-hat-wizard", 1, CategoryScope.Novel),
                new(2, "科幻", "fa-rocket", 2, CategoryScope.Novel),
                new(3, "懸疑", "fa-magnifying-glass", 3, CategoryScope.Novel),
                new(4, "日常", "fa-mug-hot", 4, CategoryScope.Novel),
            ],
            SelectedIds = [1, 3],
        };

        /// <summary>
        /// 顯示頁面
        /// </summary>
        public void OnGet()
        {
        }
    }

    /// <summary>
    /// 一組 M3 強調色角色
    /// </summary>
    /// <param name="Role">M3 角色名稱（CSS 變數 --md-{Role}）</param>
    /// <param name="Label">中文名稱</param>
    /// <param name="Usage">用途說明</param>
    /// <param name="BootstrapName">對應的 Bootstrap 語意色名稱</param>
    public sealed record ColorRoleGroup(string Role, string Label, string Usage, string BootstrapName);

    /// <summary>
    /// M3 表面角色
    /// </summary>
    /// <param name="Role">M3 角色名稱（CSS 變數 --md-{Role}）</param>
    /// <param name="Usage">用途說明</param>
    /// <param name="LegacyVariable">對應的舊變數名稱，沒有時為 null</param>
    public sealed record SurfaceRole(string Role, string Usage, string LegacyVariable);
}
