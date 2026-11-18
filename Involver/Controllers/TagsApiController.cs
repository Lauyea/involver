using System.Globalization;
using System.Text;

using DataAccess.Common;

using Involver.Authorization.Tag;
using Involver.Services;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Involver.Controllers;

/// <summary>
/// 標籤 API
/// </summary>
[Route("api/v1/tags")]
[ApiController]
public class TagsApiController(ITagService tagService) : ControllerBase
{
    /// <summary>
    /// 變更標籤狀態的請求
    /// </summary>
    public class UpdateTagStatusRequest
    {
        /// <summary>
        /// 新狀態
        /// </summary>
        public TagStatus Status { get; set; }
    }

    /// <summary>
    /// 取得建議清單（Tagify whitelist）。只列正式標籤；該作品類型用過的優先，依使用次數排序。
    /// </summary>
    /// <param name="type">作品類型：novel 或 article</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>Tagify 需要的 { "value": "標籤" } 格式清單</returns>
    [HttpGet]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, NoStore = false, VaryByQueryKeys = ["type"])]
    public async Task<IActionResult> GetTagsAsync([FromQuery] string type, CancellationToken cancellationToken)
    {
        if (!TryParseTarget(type, out var target))
        {
            return BadRequest(new { message = "type 必須是 novel 或 article。" });
        }

        var tags = await tagService.GetSuggestionsAsync(target, cancellationToken);

        // 只回傳 value：Tagify 會把 whitelist 物件的其他欄位一起寫進表單的值
        return Ok(tags.Select(t => new { value = t.Name }));
    }

    /// <summary>
    /// 取得與輸入名稱相似的現有標籤（含待審標籤）。
    /// duplicate 有值時代表只差結尾數字或符號，不能新增，必須改用它。
    /// </summary>
    /// <param name="name">作者輸入的名稱</param>
    /// <param name="type">作品類型：novel 或 article</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>相同、重複與相似的現有標籤</returns>
    [HttpGet("similar")]
    [Authorize]
    public async Task<IActionResult> GetSimilarTagsAsync([FromQuery] string name, [FromQuery] string type, CancellationToken cancellationToken)
    {
        if (!TryParseTarget(type, out var target))
        {
            return BadRequest(new { message = "type 必須是 novel 或 article。" });
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(new { message = "請輸入標籤名稱。" });
        }

        var result = await tagService.FindSimilarAsync(name, target, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// 現有標籤的重複報表（CSV），列出可能重複的組合給管理員確認後再合併
    /// </summary>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>UTF-8（含 BOM）CSV 檔</returns>
    [HttpGet("reports/duplicates")]
    [Authorize]
    public async Task<IActionResult> GetDuplicateReportAsync(CancellationToken cancellationToken)
    {
        if (!TagManagers.IsTagManager(User))
        {
            return Forbid();
        }

        var rows = await tagService.GetDuplicateReportAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("類型,相似度,標籤A ID,標籤A,標籤A 狀態,標籤A 小說數,標籤A 文章數,標籤B ID,標籤B,標籤B 狀態,標籤B 小說數,標籤B 文章數,備註");
        foreach (var row in rows)
        {
            csv.AppendJoin(',',
                row.Kind,
                row.Score.ToString("0.###", CultureInfo.InvariantCulture),
                row.TagA.TagId,
                EscapeCsv(row.TagA.Name),
                row.TagA.Status,
                row.TagA.NovelCount,
                row.TagA.ArticleCount,
                row.TagB?.TagId.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                EscapeCsv(row.TagB?.Name),
                row.TagB?.Status.ToString() ?? string.Empty,
                row.TagB?.NovelCount.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                row.TagB?.ArticleCount.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                EscapeCsv(row.Note));
            csv.AppendLine();
        }

        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"tag-duplicates-{DateTime.Now:yyyyMMdd}.csv");
    }

    /// <summary>
    /// 變更標籤狀態（管理員核准待審標籤）
    /// </summary>
    /// <param name="id">標籤 ID</param>
    /// <param name="request">新狀態</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>204；找不到標籤時 404</returns>
    [HttpPut("{id:int}/status")]
    [Authorize]
    public async Task<IActionResult> UpdateTagStatusAsync(int id, [FromBody] UpdateTagStatusRequest request, CancellationToken cancellationToken)
    {
        if (!TagManagers.IsTagManager(User))
        {
            return Forbid();
        }

        if (request == null || !Enum.IsDefined(request.Status))
        {
            return BadRequest(new { message = "狀態不正確。" });
        }

        var updated = await tagService.UpdateStatusAsync(id, request.Status, cancellationToken);
        return updated ? NoContent() : NotFound();
    }

    private static bool TryParseTarget(string type, out TagTarget target)
    {
        target = TagTarget.Novel;
        if (string.Equals(type, "novel", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(type, "article", StringComparison.OrdinalIgnoreCase))
        {
            target = TagTarget.Article;
            return true;
        }

        return false;
    }

    /// <summary>
    /// CSV 欄位跳脫；開頭是公式字元時加上單引號，避免在試算表裡被當成公式執行
    /// </summary>
    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        if ("=+-@\t\r".Contains(value[0]))
        {
            value = "'" + value;
        }

        if (value.IndexOfAny([',', '"', '\n', '\r']) >= 0)
        {
            value = $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
