using System.Text;

namespace Involver.Helpers;

/// <summary>
/// 產生給試算表開啟的 CSV
/// </summary>
public static class CsvHelper
{
    /// <summary>
    /// CSV 欄位跳脫；開頭是公式字元時加上單引號，避免在試算表裡被當成公式執行
    /// </summary>
    /// <param name="value">欄位值</param>
    /// <returns>跳脫後的欄位</returns>
    public static string Escape(string value)
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

    /// <summary>
    /// 轉成 UTF-8（含 BOM）位元組，Excel 才不會把中文顯示成亂碼
    /// </summary>
    /// <param name="csv">CSV 內容</param>
    /// <returns>檔案內容</returns>
    public static byte[] ToUtf8WithBom(StringBuilder csv)
    {
        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }
}
