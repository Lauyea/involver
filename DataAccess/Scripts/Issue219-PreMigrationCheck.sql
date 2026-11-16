-- =====================================================================
-- Issue #219 migration（AddConstraintsAndQueryIndexes）套用前的資料檢查
--
-- 用途：在正式環境勾選 run_migration 部署之前，先在正式 DB 執行這份腳本。
--       全部查詢都是唯讀的，不會修改任何資料。
--
-- 結果判讀：每一段都應該回傳 0 筆。
--   有任何一段回傳資料時，migration 的前置檢查會 THROW，整個 migration 會 rollback，
--   部署會停在「Apply migration」步驟，網站不會被更新，資料也不會被修改。
--   請先人工確認並處理這些資料，再重新部署。
--
-- 檔案最後附有清理範例，全部預設註解掉；請逐筆確認後再自行執行。
-- =====================================================================
SET NOCOUNT ON;

-- 1. 多父表外鍵沒有「剛好填一個」（CHECK 約束）
SELECT N'Comments 父表不是剛好一個' AS [檢查], CommentID, NovelID, EpisodeID, ArticleID, ProfileID
FROM dbo.Comments
WHERE (CASE WHEN NovelID IS NULL THEN 0 ELSE 1 END
     + CASE WHEN EpisodeID IS NULL THEN 0 ELSE 1 END
     + CASE WHEN ArticleID IS NULL THEN 0 ELSE 1 END) <> 1;

SELECT N'Involvings 對象不是剛好一個' AS [檢查], InvolvingID, ProfileID, NovelID, ArticleID, InvolverID, TotalValue
FROM dbo.Involvings
WHERE (CASE WHEN ProfileID IS NULL THEN 0 ELSE 1 END
     + CASE WHEN NovelID IS NULL THEN 0 ELSE 1 END
     + CASE WHEN ArticleID IS NULL THEN 0 ELSE 1 END) <> 1;

SELECT N'Follows 對象不是剛好一個' AS [檢查], FollowID, ProfileID, NovelID, FollowerID
FROM dbo.Follows
WHERE (CASE WHEN ProfileID IS NULL THEN 0 ELSE 1 END
     + CASE WHEN NovelID IS NULL THEN 0 ELSE 1 END) <> 1;

SELECT N'Agrees 對象不是剛好一個' AS [檢查], AgreeID, CommentID, MessageID, ProfileID
FROM dbo.Agrees
WHERE (CASE WHEN CommentID IS NULL THEN 0 ELSE 1 END
     + CASE WHEN MessageID IS NULL THEN 0 ELSE 1 END) <> 1;

SELECT N'Views 對象不是剛好一個' AS [檢查], Id, ArticleId, NovelId, CreateTime
FROM dbo.Views
WHERE (CASE WHEN ArticleId IS NULL THEN 0 ELSE 1 END
     + CASE WHEN NovelId IS NULL THEN 0 ELSE 1 END) <> 1;

-- 2. Unique 索引會衝突的重複資料
SELECT N'Profiles.UserName 重複（不分大小寫）' AS [檢查], UserName, COUNT(*) AS [筆數]
FROM dbo.Profiles
GROUP BY UserName
HAVING COUNT(*) > 1;

SELECT N'Follows 重複追蹤作者' AS [檢查], ProfileID, FollowerID, COUNT(*) AS [筆數]
FROM dbo.Follows
WHERE ProfileID IS NOT NULL
GROUP BY ProfileID, FollowerID
HAVING COUNT(*) > 1;

SELECT N'Follows 重複追蹤小說' AS [檢查], NovelID, FollowerID, COUNT(*) AS [筆數]
FROM dbo.Follows
WHERE NovelID IS NOT NULL
GROUP BY NovelID, FollowerID
HAVING COUNT(*) > 1;

SELECT N'Agrees 重複按讚留言' AS [檢查], CommentID, ProfileID, COUNT(*) AS [筆數]
FROM dbo.Agrees
WHERE CommentID IS NOT NULL
GROUP BY CommentID, ProfileID
HAVING COUNT(*) > 1;

SELECT N'Agrees 重複按讚回覆' AS [檢查], MessageID, ProfileID, COUNT(*) AS [筆數]
FROM dbo.Agrees
WHERE MessageID IS NOT NULL
GROUP BY MessageID, ProfileID
HAVING COUNT(*) > 1;

SELECT N'NovelTags.Name 重複（不分大小寫）' AS [檢查], Name, COUNT(*) AS [筆數]
FROM dbo.NovelTags
GROUP BY Name
HAVING COUNT(*) > 1;

SELECT N'ArticleTags.Name 重複（不分大小寫）' AS [檢查], Name, COUNT(*) AS [筆數]
FROM dbo.ArticleTags
GROUP BY Name
HAVING COUNT(*) > 1;

-- 3. 欄位縮短長度（nvarchar(max) → 有長度）會被截斷的資料
SELECT N'Payments.MerchantTradeNo 超過 64 字' AS [檢查], PaymentID, LEN(MerchantTradeNo) AS [長度]
FROM dbo.Payments WHERE LEN(MerchantTradeNo) > 64;

SELECT N'Payments.InvolverID 超過 450 字' AS [檢查], PaymentID, LEN(InvolverID) AS [長度]
FROM dbo.Payments WHERE LEN(InvolverID) > 450;

SELECT N'Episodes.OwnerID 超過 450 字' AS [檢查], EpisodeID, LEN(OwnerID) AS [長度]
FROM dbo.Episodes WHERE LEN(OwnerID) > 450;

SELECT N'Votings.OwnerID 超過 450 字' AS [檢查], VotingID, LEN(OwnerID) AS [長度]
FROM dbo.Votings WHERE LEN(OwnerID) > 450;

SELECT N'Votes.OwnerID 超過 450 字' AS [檢查], VoteID, LEN(OwnerID) AS [長度]
FROM dbo.Votes WHERE LEN(OwnerID) > 450;

-- =====================================================================
-- 清理範例（預設註解掉，請先確認上面的結果再決定是否執行）
-- =====================================================================
-- 重複追蹤／重複按讚：保留 ID 最小的一筆，其餘刪除。
-- 注意 Follows 的 NovelMonthlyInvolver / ProfileMonthlyInvolver 旗標：刪除前先確認要保留的那筆旗標正確。
--
-- BEGIN TRANSACTION;
-- WITH d AS (
--     SELECT FollowID, ROW_NUMBER() OVER (PARTITION BY ProfileID, FollowerID ORDER BY FollowID) AS rn
--     FROM dbo.Follows WHERE ProfileID IS NOT NULL)
-- DELETE FROM d WHERE rn > 1;
-- WITH d AS (
--     SELECT FollowID, ROW_NUMBER() OVER (PARTITION BY NovelID, FollowerID ORDER BY FollowID) AS rn
--     FROM dbo.Follows WHERE NovelID IS NOT NULL)
-- DELETE FROM d WHERE rn > 1;
-- WITH d AS (
--     SELECT AgreeID, ROW_NUMBER() OVER (PARTITION BY CommentID, ProfileID ORDER BY AgreeID) AS rn
--     FROM dbo.Agrees WHERE CommentID IS NOT NULL)
-- DELETE FROM d WHERE rn > 1;
-- WITH d AS (
--     SELECT AgreeID, ROW_NUMBER() OVER (PARTITION BY MessageID, ProfileID ORDER BY AgreeID) AS rn
--     FROM dbo.Agrees WHERE MessageID IS NOT NULL)
-- DELETE FROM d WHERE rn > 1;
-- -- 確認筆數正確後再 COMMIT，否則 ROLLBACK
-- -- COMMIT;
--
-- 沒有對象的孤兒按讚（留言／回覆已被刪除時 EF 把外鍵設成 NULL 留下的）：
-- DELETE FROM dbo.Agrees WHERE CommentID IS NULL AND MessageID IS NULL;
--
-- Profiles.UserName 重複、標籤名稱重複、Involvings 對象不明：牽涉帳號與 In 幣，不提供自動清理，請個別處理。
