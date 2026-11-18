namespace Involver.Helpers;

/// <summary>
/// 兩個標籤的比對結果
/// </summary>
public enum TagMatchKind
{
    /// <summary>
    /// 不相似
    /// </summary>
    None,

    /// <summary>
    /// 相似：可以新增，但前端要提示作者改用現有標籤
    /// </summary>
    Similar,

    /// <summary>
    /// 重複：去掉結尾數字或符號之後名稱相同，不能新增
    /// </summary>
    Duplicate,

    /// <summary>
    /// 統一格式後完全相同，就是同一個標籤
    /// </summary>
    Same
}

/// <summary>
/// 標籤相似度判斷。輸入一律是 <see cref="TagNameNormalizer.Normalize"/> 統一格式後的名稱。
/// 標籤最長 15 字、數量不多，全部在記憶體裡計算。
/// </summary>
public static class TagSimilarity
{
    /// <summary>
    /// 相鄰兩字重疊比例（bigram Jaccard）達到這個值就列為相似
    /// </summary>
    public const double BigramJaccardThreshold = 0.5;

    /// <summary>
    /// 編輯距離換算的相似度（1 - 距離 / 較長的字數）達到這個值就列為相似。
    /// 兩字的標籤只差一個字（例如「奇幻」和「科幻」）是 0.5，不會被列入。
    /// </summary>
    public const double EditSimilarityThreshold = 0.7;

    /// <summary>
    /// 比對新標籤與現有標籤
    /// </summary>
    /// <param name="newName">新標籤統一格式後的名稱</param>
    /// <param name="existingName">現有標籤統一格式後的名稱</param>
    /// <returns>比對結果與分數（0～1，越高越像）</returns>
    public static (TagMatchKind Kind, double Score) Compare(string newName, string existingName)
    {
        if (string.IsNullOrEmpty(newName) || string.IsNullOrEmpty(existingName))
        {
            return (TagMatchKind.None, 0);
        }

        if (newName == existingName)
        {
            return (TagMatchKind.Same, 1);
        }

        var newBase = StripTrailingNumber(newName);
        var existingBase = StripTrailingNumber(existingName);

        // 純數字或符號的標籤（例如年份「2023」「2024」）數字不同就是不同意思，只比對完全相同
        if (newBase.Length == 0 || existingBase.Length == 0)
        {
            return (TagMatchKind.None, 0);
        }

        // 新標籤有結尾數字或符號，去掉之後和現有標籤一樣（例如「讀書心得1」對「讀書心得」或「讀書心得2」）
        if (newBase != newName && newBase == existingBase)
        {
            return (TagMatchKind.Duplicate, 1);
        }

        // 反過來：新標籤是乾淨的名稱，現有的是加了編號的版本，作者可能是想用同一個
        if (existingBase != existingName && newName == existingBase)
        {
            return (TagMatchKind.Similar, 0.95);
        }

        var jaccard = BigramJaccard(newName, existingName);
        var edit = EditSimilarity(newName, existingName);
        var score = Math.Max(jaccard, edit);

        if (jaccard >= BigramJaccardThreshold || edit >= EditSimilarityThreshold)
        {
            return (TagMatchKind.Similar, score);
        }

        return (TagMatchKind.None, score);
    }

    /// <summary>
    /// 去掉結尾的數字和符號（例如「讀書心得01」→「讀書心得」）
    /// </summary>
    /// <param name="normalizedName">統一格式後的名稱</param>
    /// <returns>去掉結尾數字和符號後的名稱；整個都是數字時回傳空字串</returns>
    public static string StripTrailingNumber(string normalizedName)
    {
        if (string.IsNullOrEmpty(normalizedName))
        {
            return string.Empty;
        }

        var end = normalizedName.Length;
        while (end > 0)
        {
            var ch = normalizedName[end - 1];
            if (!char.IsDigit(ch) && !char.IsSymbol(ch) && !char.IsPunctuation(ch))
            {
                break;
            }

            end--;
        }

        return normalizedName[..end];
    }

    /// <summary>
    /// 相鄰兩字重疊比例（bigram Jaccard）。只有一個字時以單字比較。
    /// </summary>
    /// <param name="a">名稱 A</param>
    /// <param name="b">名稱 B</param>
    /// <returns>0～1，兩者 bigram 集合的交集除以聯集</returns>
    public static double BigramJaccard(string a, string b)
    {
        var setA = GetBigrams(a);
        var setB = GetBigrams(b);

        if (setA.Count == 0 || setB.Count == 0)
        {
            return 0;
        }

        var intersection = setA.Count(setB.Contains);
        var union = setA.Count + setB.Count - intersection;

        return (double)intersection / union;
    }

    /// <summary>
    /// 編輯距離換算的相似度：1 - 距離 / 較長的字數
    /// </summary>
    /// <param name="a">名稱 A</param>
    /// <param name="b">名稱 B</param>
    /// <returns>0～1，完全相同為 1</returns>
    public static double EditSimilarity(string a, string b)
    {
        var maxLength = Math.Max(a?.Length ?? 0, b?.Length ?? 0);
        if (maxLength == 0)
        {
            return 0;
        }

        return 1 - (double)LevenshteinDistance(a ?? string.Empty, b ?? string.Empty) / maxLength;
    }

    /// <summary>
    /// 編輯距離（Levenshtein）
    /// </summary>
    /// <param name="a">名稱 A</param>
    /// <param name="b">名稱 B</param>
    /// <returns>把 A 改成 B 需要的最少插入、刪除、替換次數</returns>
    public static int LevenshteinDistance(string a, string b)
    {
        if (a.Length == 0)
        {
            return b.Length;
        }

        if (b.Length == 0)
        {
            return a.Length;
        }

        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }

    private static HashSet<string> GetBigrams(string value)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(value))
        {
            return set;
        }

        if (value.Length == 1)
        {
            set.Add(value);
            return set;
        }

        for (var i = 0; i < value.Length - 1; i++)
        {
            set.Add(value.Substring(i, 2));
        }

        return set;
    }
}
