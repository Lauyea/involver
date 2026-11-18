-- =====================================================================
-- Issue #234 migration（UnifyTags）套用前的資料檢查
--
-- 用途：在正式環境勾選 run_migration 部署之前，先在「正式資料的備份」上執行這份腳本，
--       預覽 NovelTags、ArticleTags 合併成一張 Tags 表的結果。
--       只在暫存表上計算，不會修改任何資料。
--
-- NormalizedName 的計算方式與 migration（20261118012621_UnifyTags.cs 的 NormalizeSql）一致。
--
-- 結果判讀：
--   1. 會被合併的標籤：同一個 NormalizedName 有多筆舊標籤。合併後只留「保留的名稱」那一筆，
--      其他標籤的作品會改連到它。請確認這些標籤確實是同一個意思。
--   2. 合併後同一部作品重複的標籤：這些重複的關聯會被刪掉一筆（作品仍保有該標籤）。
--   3. 只有空白或標點的標籤：NormalizedName 會改用名稱的小寫。
--   4. 合併後的統計。
-- =====================================================================
SET NOCOUNT ON;

IF OBJECT_ID('tempdb..#TagMigrationMap') IS NOT NULL DROP TABLE #TagMigrationMap;

CREATE TABLE #TagMigrationMap (
    [Source] char(1) NOT NULL,
    [OldTagId] int NOT NULL,
    [Name] nvarchar(15) NOT NULL,
    [NormalizedName] nvarchar(64) COLLATE DATABASE_DEFAULT NULL,
    [UsageCount] int NOT NULL,
    [OnlyPunctuation] bit NOT NULL DEFAULT 0,
    PRIMARY KEY ([Source], [OldTagId])
);

INSERT INTO #TagMigrationMap ([Source], [OldTagId], [Name], [UsageCount])
SELECT 'N', t.[TagId], t.[Name], (SELECT COUNT(*) FROM dbo.[NovelTaggings] nt WHERE nt.[NovelTagsTagId] = t.[TagId])
FROM dbo.[NovelTags] t;

INSERT INTO #TagMigrationMap ([Source], [OldTagId], [Name], [UsageCount])
SELECT 'A', t.[TagId], t.[Name], (SELECT COUNT(*) FROM dbo.[ArticleTaggings] at WHERE at.[ArticleTagsTagId] = t.[TagId])
FROM dbo.[ArticleTags] t;

-- ---- 與 migration 相同的格式統一 ----
UPDATE #TagMigrationMap SET [NormalizedName] = LTRIM(RTRIM([Name]));

DECLARE @code int = 65281;
WHILE @code <= 65374
BEGIN
    UPDATE #TagMigrationMap
    SET [NormalizedName] = REPLACE([NormalizedName] COLLATE Latin1_General_100_BIN2, NCHAR(@code), NCHAR(@code - 65248));
    SET @code += 1;
END;

UPDATE #TagMigrationMap SET [NormalizedName] = LOWER([NormalizedName]);

UPDATE #TagMigrationMap SET [NormalizedName] = REPLACE([NormalizedName] COLLATE Latin1_General_100_BIN2, N'臺', N'台');

DECLARE @remove nvarchar(400) = N'!"#%&''()*,-./:;?@[\]_{}¡§«¶·»¿‐‑‒–—―‖‗‘’‚‛“”„‟†‡•‣․‥…‧‰‱′″‴‵‶‷‸‹›※‼‽‾‿⁀⁁⁂⁃⁅⁆⁇⁈⁉⁊⁋⁌⁍⁎⁏⁐⁑⁓⁔⁕⁖⁗⁘⁙⁚⁛⁜⁝⁞、。〃〈〉《》「」『』【】〔〕〖〗〘〙〚〛〜〝〞〟〰〽゠・｟｠｡｢｣､･'
    + N' ' + NCHAR(9) + NCHAR(10) + NCHAR(13) + NCHAR(160) + NCHAR(12288) + NCHAR(8203) + NCHAR(65279);
DECLARE @i int = 1;
DECLARE @length int = DATALENGTH(@remove) / 2;
WHILE @i <= @length
BEGIN
    UPDATE #TagMigrationMap
    SET [NormalizedName] = REPLACE([NormalizedName] COLLATE Latin1_General_100_BIN2, SUBSTRING(@remove, @i, 1), N'');
    SET @i += 1;
END;

UPDATE #TagMigrationMap SET [OnlyPunctuation] = 1, [NormalizedName] = LOWER(LTRIM(RTRIM([Name]))) WHERE [NormalizedName] = N'';
-- ---- 格式統一結束 ----

;WITH [ranked] AS (
    SELECT *,
        ROW_NUMBER() OVER (PARTITION BY [NormalizedName] ORDER BY [UsageCount] DESC, [Source] DESC, [OldTagId]) AS [rn],
        COUNT(*) OVER (PARTITION BY [NormalizedName]) AS [GroupSize]
    FROM #TagMigrationMap)
SELECT
    N'1. 會被合併的標籤' AS [檢查],
    r.[NormalizedName],
    keep.[Name] AS [保留的名稱],
    CASE r.[Source] WHEN 'N' THEN N'小說' ELSE N'文章' END AS [來源],
    r.[OldTagId] AS [舊 TagId],
    r.[Name] AS [舊名稱],
    r.[UsageCount] AS [使用次數]
FROM [ranked] r
INNER JOIN [ranked] keep ON keep.[NormalizedName] = r.[NormalizedName] AND keep.[rn] = 1
WHERE r.[GroupSize] > 1
ORDER BY r.[NormalizedName], r.[rn];

SELECT N'2. 合併後同一部小說重複的標籤' AS [檢查], j.[NovelsNovelID] AS [NovelID], m.[NormalizedName], COUNT(*) AS [關聯筆數]
FROM dbo.[NovelTaggings] j
INNER JOIN #TagMigrationMap m ON m.[Source] = 'N' AND m.[OldTagId] = j.[NovelTagsTagId]
GROUP BY j.[NovelsNovelID], m.[NormalizedName]
HAVING COUNT(*) > 1;

SELECT N'2. 合併後同一篇文章重複的標籤' AS [檢查], j.[ArticlesArticleID] AS [ArticleID], m.[NormalizedName], COUNT(*) AS [關聯筆數]
FROM dbo.[ArticleTaggings] j
INNER JOIN #TagMigrationMap m ON m.[Source] = 'A' AND m.[OldTagId] = j.[ArticleTagsTagId]
GROUP BY j.[ArticlesArticleID], m.[NormalizedName]
HAVING COUNT(*) > 1;

SELECT N'3. 只有空白或標點的標籤' AS [檢查], [Source], [OldTagId], [Name], [NormalizedName]
FROM #TagMigrationMap
WHERE [OnlyPunctuation] = 1;

SELECT
    N'4. 合併後的統計' AS [檢查],
    (SELECT COUNT(*) FROM #TagMigrationMap WHERE [Source] = 'N') AS [舊小說標籤數],
    (SELECT COUNT(*) FROM #TagMigrationMap WHERE [Source] = 'A') AS [舊文章標籤數],
    (SELECT COUNT(DISTINCT [NormalizedName]) FROM #TagMigrationMap) AS [合併後標籤數];

DROP TABLE #TagMigrationMap;
