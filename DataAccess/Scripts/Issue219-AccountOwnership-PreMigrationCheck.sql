-- =====================================================================
-- Issue #219 migration（AccountOwnershipAndPaymentTimes）套用前的資料檢查
--
-- 用途：在正式環境勾選 run_migration 部署之前，先在正式 DB 執行這份腳本。
--       全部查詢都是唯讀的，不會修改任何資料。
--
-- 結果判讀：第 1 到第 4 段都應該回傳 0 筆。
--   有任何一段回傳資料時，migration 的前置檢查會 THROW，整個 migration 會 rollback，
--   部署會停在「Apply migration」步驟，網站不會被更新，資料也不會被修改。
--   第 5 段只是預覽付款時間回填結果，不會擋部署。
-- =====================================================================
SET NOCOUNT ON;

-- 1. Profile 找不到對應的登入帳號（無法加 Profiles → AspNetUsers 外鍵）
SELECT N'Profiles 找不到 AspNetUsers' AS [檢查], p.ProfileID, p.UserName
FROM dbo.Profiles p
WHERE NOT EXISTS (SELECT 1 FROM dbo.AspNetUsers u WHERE u.Id = p.ProfileID);

-- 2. Prime/Banned 兩邊不一致（移除 AspNetUsers 的欄位後，以 Profiles 為準）
SELECT N'Prime/Banned 不一致' AS [檢查], p.ProfileID, p.UserName,
       p.Prime AS [Profile.Prime], u.Prime AS [User.Prime],
       p.Banned AS [Profile.Banned], u.Banned AS [User.Banned]
FROM dbo.Profiles p
JOIN dbo.AspNetUsers u ON u.Id = p.ProfileID
WHERE p.Prime <> u.Prime OR p.Banned <> u.Banned;

SELECT N'沒有 Profile 的帳號有 Prime/Banned' AS [檢查], u.Id, u.UserName, u.Prime, u.Banned
FROM dbo.AspNetUsers u
WHERE (u.Prime = 1 OR u.Banned = 1)
  AND NOT EXISTS (SELECT 1 FROM dbo.Profiles p WHERE p.ProfileID = u.Id);

-- 3. 要移除的明文敏感欄位必須是空的（只列筆數，不列出內容）
SELECT N'AspNetUsers.BankAccount 有資料' AS [檢查], COUNT(*) AS [筆數]
FROM dbo.AspNetUsers
WHERE BankAccount IS NOT NULL AND LTRIM(RTRIM(BankAccount)) <> N''
HAVING COUNT(*) > 0;

SELECT N'ProfitSharings.CreditCard 有資料' AS [檢查], COUNT(*) AS [筆數]
FROM dbo.ProfitSharings
WHERE CreditCard IS NOT NULL AND LTRIM(RTRIM(CreditCard)) <> N''
HAVING COUNT(*) > 0;

-- 4. 擁有者欄位指向不存在的 Profile
SELECT N'Episodes.OwnerID 找不到 Profile' AS [檢查], e.EpisodeID, e.OwnerID
FROM dbo.Episodes e
WHERE e.OwnerID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Profiles p WHERE p.ProfileID = e.OwnerID);

SELECT N'Votings.OwnerID 找不到 Profile' AS [檢查], v.VotingID, v.OwnerID
FROM dbo.Votings v
WHERE v.OwnerID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Profiles p WHERE p.ProfileID = v.OwnerID);

SELECT N'Votes.OwnerID 找不到 Profile' AS [檢查], v.VoteID, v.OwnerID
FROM dbo.Votes v
WHERE v.OwnerID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Profiles p WHERE p.ProfileID = v.OwnerID);

-- 5. 預覽：付款時間無法由原始字串解析的紀錄（回填後會是 NULL，原始字串保留，頁面會顯示原始字串）
SELECT N'付款時間無法解析（不擋部署）' AS [檢查], PaymentID, PaymentDate, TradeDate
FROM dbo.Payments
WHERE (PaymentDate IS NOT NULL AND TRY_CONVERT(datetime2, REPLACE(PaymentDate, N'/', N'-'), 120) IS NULL)
   OR (TradeDate IS NOT NULL AND TRY_CONVERT(datetime2, REPLACE(TradeDate, N'/', N'-'), 120) IS NULL);
