using System.Globalization;
using System.Text;

namespace Involver.Helpers;

/// <summary>
/// 標籤名稱的格式統一。
/// 產出的 NormalizedName 存在 Tags 表並設 Unique 索引，所有標籤比對都用這個值。
/// </summary>
/// <remarks>
/// migration（UnifyTags）裡有一份 SQL 版的近似實作，用來把舊資料寫入 NormalizedName；
/// 修改這裡的規則時，要一併確認 migration 與 DataAccess/Scripts/Issue234-PreMigrationCheck.sql。
/// </remarks>
public static class TagNameNormalizer
{
    /// <summary>
    /// 統一標籤格式：去頭尾空白 → NFKC（全形轉半形）→ 英文轉小寫 → 「臺」轉「台」→ 去掉中間空白和標點
    /// </summary>
    /// <param name="name">作者輸入的標籤名稱</param>
    /// <returns>統一格式後的名稱；輸入是 null、空白或只有標點時回傳空字串</returns>
    public static string Normalize(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var normalized = name
            .Trim()
            .Normalize(NormalizationForm.FormKC)
            .ToLowerInvariant()
            .Replace('臺', '台');

        var builder = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            if (IsRemovable(ch))
            {
                continue;
            }

            builder.Append(ch);
        }

        return builder.ToString();
    }

    /// <summary>
    /// 把作者輸入的名稱整理成要存進 Name 欄位的寫法：只去頭尾空白、把連續空白縮成一個，保留原本的大小寫和全半形
    /// </summary>
    /// <param name="name">作者輸入的標籤名稱</param>
    /// <returns>整理後的顯示名稱</returns>
    public static string CleanDisplayName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(name.Length);
        var lastWasSpace = false;
        foreach (var ch in name.Trim())
        {
            if (char.IsWhiteSpace(ch) || char.IsControl(ch))
            {
                if (!lastWasSpace)
                {
                    builder.Append(' ');
                }

                lastWasSpace = true;
                continue;
            }

            builder.Append(ch);
            lastWasSpace = false;
        }

        return builder.ToString();
    }

    /// <summary>
    /// 是否為要去掉的字元：空白、標點、控制字元與零寬字元等格式字元
    /// </summary>
    private static bool IsRemovable(char ch)
    {
        if (char.IsWhiteSpace(ch) || char.IsPunctuation(ch) || char.IsControl(ch))
        {
            return true;
        }

        return CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.Format;
    }
}
