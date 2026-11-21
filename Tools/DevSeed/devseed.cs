#:project ../../DataAccess/DataAccess.csproj
#:property TargetFramework=net10.0
#:property PublishAot=false

// 開發用種子資料：在本機資料庫建立各種身分的測試帳號與少量作品，搭配登入頁的「以使用者 ID 登入」（只在 Development 顯示）
// 讓人或 Agent 不用密碼就能用不同身分驗證頁面與權限。
//
// 用法（在這個資料夾執行，需要 .NET 10 SDK 以上）：
//   dotnet run devseed.cs -- seed    清掉舊的種子資料後重新建立帳號、角色、作品、留言與追蹤
//   dotnet run devseed.cs -- list    列出測試帳號的 ID、身分與目前是否存在
//   dotnet run devseed.cs -- clean   刪除所有種子資料（包含用測試帳號登入後產生的成就、任務、通知、金幣紀錄）
//
// 連線字串：環境變數 INVOLVER_DEV_CONNECTION，預設為本機 .\SQLEXPRESS 的 Involver 資料庫。
// 這支工具會新增與刪除資料，只能連本機資料庫；連到其他主機會直接停止。
// 所有種子帳號的 ProfileID 都以 00000000-de00- 開頭，作品標題都以「[DEV]」開頭。
// 角色需要的 AspNetRoles 不存在時會自動建立（名稱與 Involver/Authorization/*/*Operations.cs 相同）。

using System.Text;

using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.ArticleModel;
using DataAccess.Models.NovelModel;
using DataAccess.Models.StatisticalData;

using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;


Console.OutputEncoding = Encoding.UTF8;

const string DefaultConnectionString = @"Server=.\SQLEXPRESS;Database=Involver;Trusted_Connection=True;TrustServerCertificate=True";
const string Prefix = "00000000-de00-";
const string TitlePrefix = "[DEV] ";

var connectionString = Environment.GetEnvironmentVariable("INVOLVER_DEV_CONNECTION") ?? DefaultConnectionString;
var server = new SqlConnectionStringBuilder(connectionString).DataSource.Trim().ToLowerInvariant();
var isLocal = server.StartsWith('.') || server.StartsWith("(local") || server.StartsWith("localhost") || server.StartsWith("127.0.0.1")
    || server.StartsWith(Environment.MachineName.ToLowerInvariant());
if (!isLocal)
{
    Console.Error.WriteLine($"只能連本機資料庫，目前的主機是「{server}」。");
    return 1;
}

var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;
var mode = args.FirstOrDefault() ?? string.Empty;
var now = DateTime.Now;

// ── 角色（與 Involver/Authorization/*/*Operations.cs 相同） ──────────────────────────────

string[] domains = ["Profile", "Payment", "ProfitSharing", "Novel", "Article", "Comment", "Message", "Voting"];
var administratorRoles = domains.Select(d => $"{d}Administrators").ToArray();
var managerRoles = domains.Select(d => $"{d}Managers").ToArray();

// ── 測試帳號 ──────────────────────────────

static string Id(int n) => $"{Prefix}0000-0000-{n:000000000000}";

var reader = Id(1);
var author = Id(2);
var professional = Id(3);
var banned = Id(4);
var administrator = Id(5);
var manager = Id(6);
var newcomer = Id(7);

DevAccount[] accounts =
[
    new(reader, "dev_讀者", "一般會員：有 1000 虛擬 In 幣、追蹤作者、留過言", []),
    new(author, "dev_作者", "創作者：2 部小說（其中一部有 3 章）、2 篇文章，創作者中心有資料", []),
    new(professional, "dev_特約作家", "Professional = true，1 部小說", []),
    new(banned, "dev_被封鎖", "Banned = true", []),
    new(administrator, "dev_總管理員", "擁有所有 *Administrators 角色（後台全部項目）", administratorRoles),
    new(manager, "dev_管理者", "擁有所有 *Managers 角色（後台部分項目）", managerRoles),
    new(newcomer, "dev_新會員", "今天剛註冊，什麼都沒做", []),
];

switch (mode)
{
    case "seed":
        await CleanAsync();
        await SeedAsync();
        await ListAsync();
        break;
    case "list":
        await ListAsync();
        break;
    case "clean":
        await CleanAsync();
        break;
    default:
        Console.WriteLine("用法：dotnet run devseed.cs -- seed | list | clean");
        return 2;
}

return 0;

// ── 建立 ──────────────────────────────

async Task SeedAsync()
{
    await using var context = new ApplicationDbContext(options);

    foreach (var account in accounts)
    {
        var isNew = account.Id == newcomer;
        context.Users.Add(new InvolverUser
        {
            Id = account.Id,
            UserName = account.Name,
            NormalizedUserName = account.Name.ToUpperInvariant(),
            Email = $"{account.Id}@example.test",
            NormalizedEmail = $"{account.Id}@EXAMPLE.TEST",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
        });
        context.Profiles.Add(new Profile
        {
            ProfileID = account.Id,
            UserName = account.Name,
            Introduction = $"【測試帳號】{account.Note}",
            EnrollmentDate = isNew ? now : now.AddDays(-120),
            LastTimeLogin = isNew ? now : now.AddDays(-1),
            Professional = account.Id == professional,
            Banned = account.Id == banned,
        });
        context.Missions.Add(new Missions { ProfileID = account.Id });
    }

    await context.SaveChangesAsync();

    // 角色：不存在就建立
    var roleNames = accounts.SelectMany(a => a.Roles).Distinct().ToList();
    var existingRoles = await context.Roles.Where(r => roleNames.Contains(r.Name!)).ToDictionaryAsync(r => r.Name!);
    foreach (var name in roleNames.Where(n => !existingRoles.ContainsKey(n)))
    {
        var role = new IdentityRole(name) { NormalizedName = name.ToUpperInvariant(), ConcurrencyStamp = Guid.NewGuid().ToString() };
        context.Roles.Add(role);
        existingRoles[name] = role;
    }

    foreach (var account in accounts)
    {
        foreach (var roleName in account.Roles)
        {
            context.UserRoles.Add(new IdentityUserRole<string> { UserId = account.Id, RoleId = existingRoles[roleName].Id });
        }
    }

    // 讀者的虛擬 In 幣：餘額與流水帳一致
    const decimal readerCoins = 1000;
    await context.SaveChangesAsync();
    await context.Profiles.Where(p => p.ProfileID == reader).ExecuteUpdateAsync(s => s.SetProperty(p => p.VirtualCoins, readerCoins));
    context.CoinTransactions.Add(new CoinTransaction
    {
        ProfileId = reader,
        CoinType = CoinType.Virtual,
        TransactionType = CoinTransactionType.AdminAdjust,
        Amount = readerCoins,
        BalanceAfter = readerCoins,
        CorrelationId = Guid.NewGuid(),
        Note = "devseed 測試帳號初始金幣",
        CreatedAt = DateTime.UtcNow,
    });

    // 作品：沿用 migration 建立的分類標籤（沒有就不掛）
    var category = await context.Tags.FirstOrDefaultAsync(t => t.IsCategory && t.Name == "奇幻")
        ?? await context.Tags.FirstOrDefaultAsync(t => t.IsCategory);

    Novel AddNovel(string ownerId, string title, int episodeCount, int ageDays)
    {
        var created = now.AddDays(-ageDays);
        var novel = new Novel
        {
            Title = $"{TitlePrefix}{title}",
            Introduction = $"【測試資料】{title}的故事簡介。",
            ProfileID = ownerId,
            CreateTime = created,
            UpdateTime = created.AddDays(episodeCount),
            Tags = category is null ? [] : [category],
            Episodes = Enumerable.Range(1, episodeCount).Select(e => new Episode
            {
                Title = $"第 {e} 章",
                Content = $"<p>【測試資料】{title} 第 {e} 章。</p>",
                OwnerID = ownerId,
                UpdateTime = created.AddDays(e),
            }).ToList(),
        };
        context.Novels.Add(novel);
        return novel;
    }

    Article AddArticle(string ownerId, string title, string body, int ageDays)
    {
        var article = new Article
        {
            Title = $"{TitlePrefix}{title}",
            Content = $"<p>{body}</p><p>【測試資料】</p>",
            ProfileID = ownerId,
            CreateTime = now.AddDays(-ageDays),
            UpdateTime = now.AddDays(-ageDays),
            Type = DataAccess.Common.ArticleType.General,
        };
        context.Articles.Add(article);
        return article;
    }

    var novel = AddNovel(author, "測試小說：有三章", 3, 30);
    AddNovel(author, "測試小說：剛建立還沒有章節", 0, 1);
    AddNovel(professional, "特約作家的測試小說", 2, 60);
    var article = AddArticle(author, "測試文章：一般內容", "這是一篇用來驗證文章頁、留言與 Involve 的測試文章。", 5);
    AddArticle(author, "測試文章：長標題，當標題長到需要換行時列表應該只顯示兩行並以省略號結尾", "測試長標題與內文預覽。", 2);

    context.Comments.Add(new Comment { Content = "<p>【測試留言】小說寫得很好！</p>", ProfileID = reader, Novel = novel, UpdateTime = now.AddDays(-3) });
    context.Comments.Add(new Comment { Content = "<p>【測試留言】謝謝分享。</p>", ProfileID = reader, Article = article, UpdateTime = now.AddDays(-1) });
    context.Follows.Add(new Follow { FollowerID = reader, ProfileID = author, UpdateTime = now.AddDays(-10) });
    context.Follows.Add(new Follow { FollowerID = reader, Novel = novel, UpdateTime = now.AddDays(-10) });

    await context.SaveChangesAsync();
    Console.WriteLine($"已建立 {accounts.Length} 個測試帳號、3 部小說、2 篇文章。");
}

// ── 列出 ──────────────────────────────

async Task ListAsync()
{
    await using var context = new ApplicationDbContext(options);
    var existing = await context.Profiles.Where(p => p.ProfileID.StartsWith(Prefix)).Select(p => p.ProfileID).ToListAsync();

    Console.WriteLine();
    Console.WriteLine("登入頁「以使用者 ID 登入」（只在 Development 顯示）貼上 ID 即可登入：");
    Console.WriteLine();
    Console.WriteLine("| 使用者 ID | 名稱 | 身分 | 存在 |");
    Console.WriteLine("|---|---|---|---|");
    foreach (var account in accounts)
    {
        Console.WriteLine($"| `{account.Id}` | {account.Name} | {account.Note} | {(existing.Contains(account.Id) ? "是" : "否")} |");
    }
}

// ── 清除 ──────────────────────────────

async Task CleanAsync()
{
    await using var context = new ApplicationDbContext(options);
    var like = $"{Prefix}%";
    var count = await context.Profiles.CountAsync(p => p.ProfileID.StartsWith(Prefix));

    // 種子作品底下的資料（其他會員對它們的留言、觀看、追蹤等）
    await context.Database.ExecuteSqlRawAsync("""
        DECLARE @novels TABLE (Id int PRIMARY KEY);
        DECLARE @articles TABLE (Id int PRIMARY KEY);
        INSERT INTO @novels SELECT NovelID FROM Novels WHERE ProfileID LIKE {0};
        INSERT INTO @articles SELECT ArticleID FROM Articles WHERE ProfileID LIKE {0};

        DELETE FROM Comments WHERE NovelID IN (SELECT Id FROM @novels)
            OR EpisodeID IN (SELECT e.EpisodeID FROM Episodes e WHERE e.NovelID IN (SELECT Id FROM @novels))
            OR ArticleID IN (SELECT Id FROM @articles);
        DELETE FROM Follows WHERE NovelID IN (SELECT Id FROM @novels);
        DELETE FROM Involvings WHERE NovelID IN (SELECT Id FROM @novels);
        DELETE FROM NovelViewer WHERE NovelID IN (SELECT Id FROM @novels);
        DELETE FROM ArticleViewer WHERE ArticleID IN (SELECT Id FROM @articles);
        DELETE FROM Views WHERE NovelId IN (SELECT Id FROM @novels) OR ArticleId IN (SELECT Id FROM @articles);
        DELETE FROM EpisodeReads WHERE EpisodeId IN (SELECT e.EpisodeID FROM Episodes e WHERE e.NovelID IN (SELECT Id FROM @novels));
        DELETE FROM NovelTaggings WHERE NovelID IN (SELECT Id FROM @novels);
        DELETE FROM ArticleTaggings WHERE ArticleID IN (SELECT Id FROM @articles);
        DELETE FROM Episodes WHERE NovelID IN (SELECT Id FROM @novels);
        DELETE FROM Novels WHERE NovelID IN (SELECT Id FROM @novels);
        DELETE FROM Articles WHERE ArticleID IN (SELECT Id FROM @articles);
        """, like);

    // 其他參照會員的表（登入後產生的成就、任務、通知、金幣紀錄等）：依單欄外鍵逐一刪除
    await context.Database.ExecuteSqlRawAsync("""
        DECLARE @sql nvarchar(max) = N'';
        SELECT @sql += N'DELETE FROM ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
            + N' WHERE ' + QUOTENAME(c.name) + N' LIKE @like;' + CHAR(10)
        FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
        WHERE OBJECT_NAME(fk.referenced_object_id) IN ('Profiles', 'AspNetUsers')
          AND OBJECT_NAME(fk.parent_object_id) NOT IN ('Profiles', 'AspNetUsers', 'Novels', 'Articles', 'Tags')
          AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id = fk.object_id) = 1;
        EXEC sp_executesql @sql, N'@like nvarchar(50)', @like = {0};
        """, like);

    await context.Database.ExecuteSqlRawAsync("UPDATE Tags SET CreatorProfileID = NULL WHERE CreatorProfileID LIKE {0}", like);
    await context.Profiles.Where(p => p.ProfileID.StartsWith(Prefix)).ExecuteDeleteAsync();
    await context.Users.Where(u => u.Id.StartsWith(Prefix)).ExecuteDeleteAsync();
    Console.WriteLine($"已清除種子資料：{count} 個測試帳號。");
}

/// <summary>
/// 一個測試帳號
/// </summary>
/// <param name="Id">ProfileID 與 AspNetUsers.Id</param>
/// <param name="Name">使用者名稱</param>
/// <param name="Note">身分說明</param>
/// <param name="Roles">擁有的角色</param>
record DevAccount(string Id, string Name, string Note, string[] Roles);
