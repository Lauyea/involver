#:project ../../DataAccess/DataAccess.csproj
#:property TargetFramework=net10.0
#:property PublishAot=false

// 推薦演算法模擬器（Issue #238）：在本機資料庫灌入模擬的作品與讀者行為，比較不同權重的推薦結果。
//
// 用法（在這個資料夾執行，需要 .NET 10 SDK 以上）：
//   dotnet run recsim.cs -- seed       清掉舊的模擬資料後重新灌入：8 種分類 × 6 部小說、240 位讀者、25 個灌票帳號、5 個測試人物
//   dotnet run recsim.cs -- articles   加入 10 篇文章，並讓測試人物追蹤幾位作者（追蹤文章頁用）
//   dotnet run recsim.cs -- run        用目前啟用的推薦設定執行一次排程，寫入 UserRecommendations
//   dotnet run recsim.cs -- compare    用同一份資料比較不同權重：測試人物的前 8 名、離線評估（不寫入資料庫）
//   dotnet run recsim.cs -- spam       比較有沒有灌票、防灌設定對灌票作品曝光的影響（不寫入資料庫）
//   dotnet run recsim.cs -- clean      刪除所有模擬資料（分類標籤保留）
//
// 連線字串：環境變數 INVOLVER_SIM_CONNECTION，預設為本機 .\SQLEXPRESS 的 Involver 資料庫。
// 這支工具會新增與刪除資料，只能連本機資料庫；連到其他主機會直接停止。
// 所有模擬帳號的 ProfileID 都以 00000000-0238- 開頭，可以用開發登入（使用者 ID）登入測試人物：
//   00000000-0238-0001-0000-000000000001  新會員，只選了「推理」
//   00000000-0238-0001-0000-000000000002  推理讀者（追蹤 4 位作者，有追蹤文章）
//   00000000-0238-0001-0000-000000000003  奇幻讀者
//   00000000-0238-0001-0000-000000000004  歷史＋愛情讀者
//   00000000-0238-0001-0000-000000000005  全新會員，什麼都沒做
// 分類標籤需要先有（migration 會從舊的小說類型建立奇幻、歷史、愛情、真實、現代、科幻、驚悚、推理）。

using System.Text;

using DataAccess.Data;
using DataAccess.Models;
using DataAccess.Models.NovelModel;
using DataAccess.Models.Recommendation;
using DataAccess.Services.Recommendations;

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;


Console.OutputEncoding = Encoding.UTF8;

const string DefaultConnectionString = @"Server=.\SQLEXPRESS;Database=Involver;Trusted_Connection=True;TrustServerCertificate=True";
const string Prefix = "00000000-0238-";

var connectionString = Environment.GetEnvironmentVariable("INVOLVER_SIM_CONNECTION") ?? DefaultConnectionString;
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

// ── 測試人物 ──────────────────────────────

static string Id(int group, int n) => $"{Prefix}{group:0000}-0000-{n:000000000000}";

string PersonaColdMystery = Id(1, 1);
string PersonaMysteryReader = Id(1, 2);
string PersonaFantasyReader = Id(1, 3);
string PersonaHistoryLove = Id(1, 4);
string PersonaBrandNew = Id(1, 5);

(string Id, string Name, string Note)[] Personas =
[
    (PersonaColdMystery, "測試_新會員選推理", "剛註冊，只選了「推理」，什麼都還沒看"),
    (PersonaMysteryReader, "測試_推理讀者", "看過《密室殺人事件簿》《鐘樓謎案》，追蹤了其中一部"),
    (PersonaFantasyReader, "測試_奇幻讀者", "看過三部奇幻"),
    (PersonaHistoryLove, "測試_歷史愛情讀者", "看過兩部歷史、一部愛情"),
    (PersonaBrandNew, "測試_全新會員", "什麼都沒做"),
];

switch (mode)
{
    case "seed":
        await CleanAsync();
        await SeedAsync();
        break;
    case "clean":
        await CleanAsync();
        break;
    case "articles":
        await SeedArticlesAsync();
        break;
    case "spam":
        await SpamAsync();
        break;
    case "run":
        {
            await using var context = new ApplicationDbContext(options);
            var result = await new RecommendationEngine(context).RunAsync(now);
            Console.WriteLine($"寫入 {result.Users} 位會員、{result.Rows} 筆推薦，設定版本 {result.SettingVersion?.ToString() ?? "預設"}");
            break;
        }
    case "compare":
        await CompareAsync();
        break;
    default:
        Console.Error.WriteLine("用法：dotnet run recsim.cs -- seed | articles | run | compare | spam | clean");
        return 1;
}

return 0;

// ── 灌資料 ──────────────────────────────

async Task SeedAsync()
{
    var random = new Random(238);
    await using var context = new ApplicationDbContext(options);

    var catalog = new Dictionary<NovelType, (string[] Titles, string[] Tags)>
    {
        [NovelType.Fantasy] = (["第七位勇者", "星火魔導師", "龍脈繼承者", "異界旅店經營記", "封印之森", "銀月騎士團"], ["魔法", "異世界", "冒險", "龍"]),
        [NovelType.History] = (["長安夜行錄", "大明錦衣衛", "三國謀士傳", "宋朝美食家", "戰國少女記", "清宮秘檔"], ["古代", "宮廷", "戰爭", "穿越"]),
        [NovelType.Love] = (["夏日告白", "咖啡館的約定", "十年後的你", "雨天的傘", "青梅竹馬協定", "錯過的列車"], ["戀愛", "甜文", "校園", "虐心"]),
        [NovelType.Real] = (["夜市人生", "北漂十年", "父親的修車廠", "社畜日記", "便利商店的早晨", "漁村少年"], ["寫實", "家庭", "職場", "成長"]),
        [NovelType.Modern] = (["都市狩魔人", "超能力上班族", "直播主的秘密", "重生之商業帝國", "地下偶像", "外送員傳說"], ["都市", "超能力", "職場", "重生"]),
        [NovelType.Science] = (["火星殖民地", "時間迴圈研究所", "機械之心", "深空信號", "基因樂園", "量子偵探社"], ["太空", "科技", "末日", "時間旅行"]),
        [NovelType.Horror] = (["午夜公寓", "校園七大不可思議", "山村祭典", "鏡中人", "廢棄醫院", "第十三號病房"], ["靈異", "怪談", "校園", "驚悚"]),
        [NovelType.Detective] = (["密室殺人事件簿", "名偵探的休假", "消失的證人", "鐘樓謎案", "列車上的不在場證明", "雪夜山莊"], ["推理", "密室", "本格", "懸疑"]),
    };
    var crossTags = new[] { "懸疑", "冒險", "穿越", "成長", "校園" };
    // 分類標籤（migration 從舊的小說類型轉來）
    var categoryTags = (await context.Tags.Where(t => t.IsCategory).ToListAsync())
        .Where(t => TypeNames.ContainsValue(t.Name))
        .ToDictionary(t => TypeNames.First(x => x.Value == t.Name).Key);

    // 讀者的主要類型分布：奇幻最多，推理是少數
    var typeShare = new (NovelType Type, double Share)[]
    {
        (NovelType.Fantasy, 0.30), (NovelType.Love, 0.15), (NovelType.Modern, 0.12), (NovelType.Detective, 0.10),
        (NovelType.Science, 0.10), (NovelType.Horror, 0.08), (NovelType.History, 0.08), (NovelType.Real, 0.07),
    };

    // 會員
    var profiles = new List<Profile>();
    void AddProfile(string id, string name, DateTime enrolled, DateTime lastLogin, bool prime = false)
    {
        context.Users.Add(new InvolverUser
        {
            Id = id,
            UserName = name,
            NormalizedUserName = name.ToUpperInvariant(),
            Email = $"{id}@example.test",
            NormalizedEmail = $"{id}@EXAMPLE.TEST",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
        });
        var profile = new Profile
        {
            ProfileID = id,
            UserName = name,
            EnrollmentDate = enrolled,
            LastTimeLogin = lastLogin,
            Prime = prime,
        };
        context.Profiles.Add(profile);
        context.Missions.Add(new Missions { ProfileID = id });
        profiles.Add(profile);
    }

    var authors = Enumerable.Range(1, 10).Select(i => Id(2, i)).ToList();
    for (var i = 0; i < authors.Count; i++)
    {
        AddProfile(authors[i], $"sim_作者{i + 1:00}", now.AddDays(-800), now.AddDays(-2));
    }

    foreach (var persona in Personas)
    {
        var isNew = persona.Id == PersonaColdMystery || persona.Id == PersonaBrandNew;
        AddProfile(persona.Id, persona.Name, isNew ? now.AddDays(-1) : now.AddDays(-200), now.AddHours(-1));
    }

    var readerTypes = new Dictionary<string, (NovelType Primary, NovelType? Secondary)>();
    for (var i = 1; i <= 240; i++)
    {
        var id = Id(3, i);
        AddProfile(id, $"sim_讀者{i:000}", now.AddDays(-random.Next(60, 700)), now.AddDays(-random.Next(0, 30)), prime: random.NextDouble() < 0.1);
        var primary = PickShare(random, typeShare);
        NovelType? secondary = random.NextDouble() < 0.4 ? PickShare(random, typeShare) : null;
        readerTypes[id] = (primary, secondary == primary ? null : secondary);
    }

    var spammers = Enumerable.Range(1, 25).Select(i => Id(4, i)).ToList();
    foreach (var id in spammers)
    {
        AddProfile(id, $"sim_灌票{id[^2..]}", now.AddDays(-1), now.AddHours(-3));
    }

    await context.SaveChangesAsync();

    // 標籤：已存在就沿用
    var tagNames = catalog.Values.SelectMany(c => c.Tags).Concat(crossTags).Distinct().ToList();
    var tags = await context.Tags.Where(t => tagNames.Contains(t.Name)).ToDictionaryAsync(t => t.Name);
    foreach (var name in tagNames.Where(n => !tags.ContainsKey(n)))
    {
        var tag = new Tag { Name = name, NormalizedName = name.ToUpperInvariant(), CreatorProfileID = authors[0], CreateTime = now.AddDays(-300) };
        context.Tags.Add(tag);
        tags[name] = tag;
    }

    // 作品：每個類型 6 部，第 1 部最熱門（Zipf）；奇幻第 1 部是全站爆紅作
    var novels = new List<(Novel Novel, NovelType Type, double Appeal, double Quality, List<Episode> Episodes)>();
    var authorIndex = 0;
    foreach (var (type, (titles, typeTags)) in catalog)
    {
        for (var rank = 0; rank < titles.Length; rank++)
        {
            var isNew = rank == titles.Length - 1 || (type == NovelType.Detective && rank == 4);
            var created = isNew ? now.AddDays(-random.Next(3, 20)) : now.AddDays(-random.Next(60, 300));
            var novelTags = typeTags.OrderBy(_ => random.Next()).Take(random.Next(2, 4)).ToList();
            if (random.NextDouble() < 0.2)
            {
                novelTags.Add(crossTags[random.Next(crossTags.Length)]);
            }

            var novel = new Novel
            {
                Title = titles[rank],
                Introduction = $"【測試資料】{titles[rank]}的故事簡介，用來驗證推薦演算法。",
                ProfileID = authors[authorIndex++ % authors.Count],
                CreateTime = created,
                UpdateTime = isNew ? now.AddDays(-random.Next(0, 3)) : now.AddDays(-random.Next(0, 40)),
                Tags = novelTags.Distinct().Select(n => tags[n]).Append(categoryTags[type]).ToList(),
            };
            var episodes = Enumerable.Range(1, 6).Select(e => new Episode
            {
                Title = $"第 {e} 章",
                Content = $"<p>【測試資料】{titles[rank]} 第 {e} 章。</p>",
                OwnerID = novel.ProfileID,
                UpdateTime = created.AddDays(e),
            }).ToList();
            novel.Episodes = episodes;
            context.Novels.Add(novel);

            var appeal = 1.0 / Math.Pow(rank + 1, 1.1);
            if (type == NovelType.Fantasy && rank == 0)
            {
                appeal *= 3;
            }

            var quality = titles[rank] == "第十三號病房" ? 0.2 : 0.35 + random.NextDouble() * 0.55;
            novels.Add((novel, type, appeal, quality, episodes));
        }
    }

    await context.SaveChangesAsync();

    var byTitle = novels.ToDictionary(n => n.Novel.Title);
    var global = novels.Select(n => (n, Weight: n.Appeal * typeShare.First(t => t.Type == n.Type).Share)).ToList();

    // 讀者行為
    foreach (var (readerId, (primary, secondary)) in readerTypes)
    {
        var count = random.Next(3, 10);
        var picked = new HashSet<string>();
        for (var attempt = 0; picked.Count < count && attempt < 50; attempt++)
        {
            var roll = random.NextDouble();
            var pool = roll < 0.7
                ? novels.Where(n => n.Type == primary).Select(n => (n, Weight: n.Appeal)).ToList()
                : roll < 0.85
                    ? novels.Where(n => n.Type == (secondary ?? primary)).Select(n => (n, Weight: n.Appeal)).ToList()
                    : global;
            var choice = PickWeighted(random, pool);
            if (picked.Add(choice.Novel.Title))
            {
                AddReading(readerId, choice, now.AddHours(-random.Next(1, 24 * 60)), random);
            }
        }
    }

    // 灌票：25 個剛註冊的帳號在 12 小時內都看、都追蹤《第十三號病房》
    var target = byTitle["第十三號病房"];
    foreach (var id in spammers)
    {
        AddReading(id, target, now.AddHours(-random.Next(1, 12)), random, forceFollow: true, readAll: false);
    }

    // 測試人物
    context.ProfileCategoryPreferences.Add(new ProfileCategoryPreference { ProfileId = PersonaColdMystery, TagId = categoryTags[NovelType.Detective].TagId, CreatedAt = DateTime.UtcNow });
    AddReading(PersonaMysteryReader, byTitle["密室殺人事件簿"], now.AddDays(-5), random, forceFollow: true);
    AddReading(PersonaMysteryReader, byTitle["鐘樓謎案"], now.AddDays(-2), random, forceFollow: false);
    foreach (var title in new[] { "星火魔導師", "龍脈繼承者", "封印之森" })
    {
        AddReading(PersonaFantasyReader, byTitle[title], now.AddDays(-random.Next(1, 10)), random, forceFollow: false);
    }

    foreach (var title in new[] { "長安夜行錄", "大明錦衣衛", "夏日告白" })
    {
        AddReading(PersonaHistoryLove, byTitle[title], now.AddDays(-random.Next(1, 10)), random, forceFollow: false);
    }

    // 作品總觀看數（熱門頁的備用排序）
    foreach (var n in novels)
    {
        n.Novel.TotalViews = context.ChangeTracker.Entries<NovelViewer>().Count(v => v.Entity.NovelID == n.Novel.NovelID) * 7;
    }

    await context.SaveChangesAsync();
    Console.WriteLine($"灌入 {profiles.Count} 位會員、{novels.Count} 部小說、{novels.Count * 6} 個章節");
    Console.WriteLine($"NovelViewer {context.ChangeTracker.Entries<NovelViewer>().Count()} 筆、Follow {context.ChangeTracker.Entries<Follow>().Count()} 筆、EpisodeRead {context.ChangeTracker.Entries<EpisodeRead>().Count()} 筆");

    void AddReading(string profileId, (Novel Novel, NovelType Type, double Appeal, double Quality, List<Episode> Episodes) n, DateTime at, Random rnd, bool? forceFollow = null, bool readAll = true)
    {
        context.Set<NovelViewer>().Add(new NovelViewer { ProfileID = profileId, NovelID = n.Novel.NovelID, ViewDate = at });
        if (at >= now.AddDays(-30))
        {
            context.Views.Add(new View { UserId = profileId, NovelId = n.Novel.NovelID, CreateTime = at });
        }

        // 讀到第幾章：每章讀完的機率 = 作品品質
        for (var e = 0; e < n.Episodes.Count; e++)
        {
            var reachedEnd = readAll && rnd.NextDouble() < n.Quality;
            context.EpisodeReads.Add(new EpisodeRead
            {
                ProfileId = profileId,
                EpisodeId = n.Episodes[e].EpisodeID,
                FirstReadAt = at.AddMinutes(e * 10),
                LastReadAt = at.AddMinutes(e * 10),
                ReachedEnd = reachedEnd,
                ReachedEndAt = reachedEnd ? at.AddMinutes(e * 10 + 5) : null,
            });
            if (!reachedEnd)
            {
                break;
            }
        }

        var follow = forceFollow ?? rnd.NextDouble() < 0.1 + 0.3 * n.Quality;
        if (follow)
        {
            context.Follows.Add(new Follow { FollowerID = profileId, NovelID = n.Novel.NovelID, UpdateTime = at.AddMinutes(30) });
        }

        if (forceFollow is null && rnd.NextDouble() < 0.03)
        {
            context.Involvings.Add(new Involving { InvolverID = profileId, NovelID = n.Novel.NovelID, Value = 10, TotalValue = 10, LastTime = at.AddHours(1) });
        }
    }
}

static NovelType PickShare(Random random, (NovelType Type, double Share)[] shares)
{
    var roll = random.NextDouble() * shares.Sum(s => s.Share);
    foreach (var (type, share) in shares)
    {
        roll -= share;
        if (roll <= 0)
        {
            return type;
        }
    }

    return shares[^1].Type;
}

static T PickWeighted<T>(Random random, List<(T Item, double Weight)> pool)
{
    var roll = random.NextDouble() * pool.Sum(p => p.Weight);
    foreach (var (item, weight) in pool)
    {
        roll -= weight;
        if (roll <= 0)
        {
            return item;
        }
    }

    return pool[^1].Item;
}

// ── 比較 ──────────────────────────────

async Task CompareAsync()
{
    await using var context = new ApplicationDbContext(options);
    var input = await new RecommendationEngine(context).LoadInputAsync(now);
    var contents = input.Contents.ToDictionary(c => c.Key);
    NovelType? TypeOf(ContentItem c) => c.TagIds
        .Where(input.CategoryTagIds.Contains)
        .Select(id => input.TagNames[id])
        .Where(TypeNames.ContainsValue)
        .Select(name => (NovelType?)TypeNames.First(x => x.Value == name).Key)
        .FirstOrDefault();

    var variants = new (string Name, RecommendationSettings Settings)[]
    {
        ("只看熱門（基準）", new RecommendationSettings { ContentWeight = 0, CollaborativeWeight = 0, FollowedAuthorWeight = 0, DislikedAuthorPenalty = 0 }),
        ("內容相似度 1.0", new RecommendationSettings { ContentWeight = 1.0 }),
        ("內容相似度 2.0（目前預設）", new RecommendationSettings()),
        ("內容相似度 3.0", new RecommendationSettings { ContentWeight = 3.0 }),
    };

    string Label(ContentKey key) => contents.TryGetValue(key, out var c)
        ? $"{(TypeOf(c) is { } t ? TypeNames[t] : "文章")}｜{c.Title}"
        : key.ToString();

    Console.WriteLine("════ 測試人物的前 8 名 ════");
    foreach (var persona in Personas)
    {
        Console.WriteLine();
        Console.WriteLine($"■ {persona.Name}：{persona.Note}");
        foreach (var (name, settings) in variants.Skip(1).Take(2))
        {
            var corpus = new RecommendationCorpus(input, settings);
            var results = new WeightedRecommendationScorer().Recommend(corpus, persona.Id).Take(8).ToList();
            Console.WriteLine($"  [{name}]");
            for (var i = 0; i < results.Count; i++)
            {
                Console.WriteLine($"    {i + 1}. {Label(results[i].Content),-22} {results[i].Score,6:0.00}  {results[i].Reason}");
            }
        }
    }

    // 離線評估：每位模擬讀者藏起最後看的一部，看推薦前 10 名有沒有猜中
    var readers = input.Interactions
        .Where(i => i.ProfileId.StartsWith(Id(3, 0)[..19], StringComparison.Ordinal))
        .GroupBy(i => i.ProfileId)
        .Select(g => (ProfileId: g.Key, Items: g.GroupBy(i => i.Content).Select(x => (Content: x.Key, Last: x.Max(i => i.At))).ToList()))
        .Where(r => r.Items.Count >= 4)
        .ToList();
    var hidden = readers.ToDictionary(r => r.ProfileId, r => r.Items.OrderByDescending(i => i.Last).ThenBy(i => i.Content).First().Content);
    var heldOutInput = new RecommendationInput
    {
        Now = input.Now,
        Contents = input.Contents,
        TagNames = input.TagNames,
        Accounts = input.Accounts,
        Interactions = input.Interactions.Where(i => !(hidden.TryGetValue(i.ProfileId, out var h) && h == i.Content)).ToList(),
        Readings = input.Readings.Where(r => !(hidden.TryGetValue(r.ProfileId, out var h) && h == ContentKey.Novel(r.NovelId))).ToList(),
        AuthorFollows = input.AuthorFollows,
        NotInterested = input.NotInterested,
        CategoryTagIds = input.CategoryTagIds,
        PreferredCategories = input.PreferredCategories,
    };

    // 讀者的主要類型 = 看過最多的類型
    NovelType? MainType(string profileId) => heldOutInput.Interactions
        .Where(i => i.ProfileId == profileId && contents.TryGetValue(i.Content, out var c) && TypeOf(c) is not null)
        .GroupBy(i => TypeOf(contents[i.Content])!.Value)
        .OrderByDescending(g => g.Select(i => i.Content).Distinct().Count())
        .Select(g => (NovelType?)g.Key)
        .FirstOrDefault();
    var mainTypes = readers.ToDictionary(r => r.ProfileId, r => MainType(r.ProfileId));
    var hitFantasy = contents.Values.First(c => c.Title == "第七位勇者").Key;

    Console.WriteLine();
    Console.WriteLine($"════ 離線評估：{readers.Count} 位模擬讀者，每人藏起最後看的一部 ════");
    Console.WriteLine($"{"設定",-16} {"猜中率@10",8} {"主類型占比",8} {"爆紅作出現率",10} {"推薦過的作品數",10}");
    foreach (var (name, settings) in variants)
    {
        var corpus = new RecommendationCorpus(heldOutInput, settings);
        var scorer = new WeightedRecommendationScorer();
        int hits = 0, sameType = 0, total = 0, withHit = 0;
        var covered = new HashSet<ContentKey>();
        foreach (var reader in readers)
        {
            var top = scorer.Recommend(corpus, reader.ProfileId).Take(10).Select(r => r.Content).ToList();
            covered.UnionWith(top);
            hits += top.Contains(hidden[reader.ProfileId]) ? 1 : 0;
            withHit += top.Contains(hitFantasy) && mainTypes[reader.ProfileId] != NovelType.Fantasy ? 1 : 0;
            total += top.Count;
            sameType += top.Count(k => TypeOf(contents[k]) == mainTypes[reader.ProfileId]);
        }

        var nonFantasy = mainTypes.Values.Count(t => t != NovelType.Fantasy);
        Console.WriteLine($"{name,-16} {hits * 100.0 / readers.Count,8:0.0}% {sameType * 100.0 / Math.Max(total, 1),8:0.0}% {withHit * 100.0 / Math.Max(nonFantasy, 1),10:0.0}% {covered.Count,10}");
    }

    Console.WriteLine("（爆紅作出現率：主要類型不是奇幻的讀者，前 10 名裡出現《第七位勇者》的比例）");
}

// ── 清除 ──────────────────────────────

async Task CleanAsync()
{
    await using var context = new ApplicationDbContext(options);
    var ids = await context.Profiles.Where(p => p.ProfileID.StartsWith(Prefix)).Select(p => p.ProfileID).ToListAsync();
    var novelIds = await context.Novels.Where(n => n.ProfileID.StartsWith(Prefix)).Select(n => n.NovelID).ToListAsync();
    var episodeIds = await context.Episodes.Where(e => novelIds.Contains(e.NovelID)).Select(e => e.EpisodeID).ToListAsync();

    await context.UserRecommendations.Where(r => r.ProfileId.StartsWith(Prefix) || (r.ContentType == RecommendationContentType.Novel && novelIds.Contains(r.ContentId))).ExecuteDeleteAsync();
    await context.RecommendationImpressions.Where(r => r.ProfileId.StartsWith(Prefix) || (r.ContentType == RecommendationContentType.Novel && novelIds.Contains(r.ContentId))).ExecuteDeleteAsync();
    await context.RecommendationFeedbacks.Where(r => r.ProfileId.StartsWith(Prefix) || (r.ContentType == RecommendationContentType.Novel && novelIds.Contains(r.ContentId))).ExecuteDeleteAsync();
    await context.ProfileCategoryPreferences.Where(p => p.ProfileId.StartsWith(Prefix)).ExecuteDeleteAsync();
    await context.EpisodeReads.Where(r => r.ProfileId.StartsWith(Prefix) || episodeIds.Contains(r.EpisodeId)).ExecuteDeleteAsync();
    await context.Views.Where(v => (v.UserId != null && v.UserId.StartsWith(Prefix)) || (v.NovelId != null && novelIds.Contains(v.NovelId.Value))).ExecuteDeleteAsync();
    await context.Set<NovelViewer>().Where(v => (v.ProfileID != null && v.ProfileID.StartsWith(Prefix)) || novelIds.Contains(v.NovelID)).ExecuteDeleteAsync();
    await context.Follows.Where(f => f.FollowerID.StartsWith(Prefix) || (f.NovelID != null && novelIds.Contains(f.NovelID.Value)) || (f.ProfileID != null && f.ProfileID.StartsWith(Prefix))).ExecuteDeleteAsync();
    await context.Involvings.Where(i => i.InvolverID.StartsWith(Prefix) || (i.NovelID != null && novelIds.Contains(i.NovelID.Value))).ExecuteDeleteAsync();

    // 其他表（例如用測試人物登入時產生的通知、任務、成就、金幣紀錄）：依外鍵逐一刪除
    await context.Database.ExecuteSqlRawAsync("""
        DECLARE @sql nvarchar(max) = N'';
        SELECT @sql += N'DELETE FROM ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id)) + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
            + N' WHERE ' + QUOTENAME(c.name) + N' LIKE ''00000000-0238-%'';' + CHAR(10)
        FROM sys.foreign_keys fk
        JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
        JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
        WHERE OBJECT_NAME(fk.referenced_object_id) IN ('Profiles', 'AspNetUsers')
          AND OBJECT_NAME(fk.parent_object_id) NOT IN ('Profiles', 'Novels', 'Tags', 'Articles')
          AND (SELECT COUNT(*) FROM sys.foreign_key_columns x WHERE x.constraint_object_id = fk.object_id) = 1;
        EXEC sp_executesql @sql;
        """);

    await context.Database.ExecuteSqlRawAsync("DELETE FROM Comments WHERE NovelID IN (SELECT NovelID FROM Novels WHERE ProfileID LIKE '00000000-0238-%') OR EpisodeID IN (SELECT e.EpisodeID FROM Episodes e JOIN Novels n ON n.NovelID = e.NovelID WHERE n.ProfileID LIKE '00000000-0238-%')");
    await context.Database.ExecuteSqlRawAsync("DELETE FROM Comments WHERE ArticleID IN (SELECT ArticleID FROM Articles WHERE ProfileID LIKE '00000000-0238-%')");
    await context.Database.ExecuteSqlRawAsync("DELETE FROM ArticleTaggings WHERE ArticleID IN (SELECT ArticleID FROM Articles WHERE ProfileID LIKE '00000000-0238-%')");
    await context.Database.ExecuteSqlRawAsync("DELETE FROM ArticleViewer WHERE ArticleID IN (SELECT ArticleID FROM Articles WHERE ProfileID LIKE '00000000-0238-%')");
    await context.Database.ExecuteSqlRawAsync("DELETE FROM Views WHERE ArticleId IN (SELECT ArticleID FROM Articles WHERE ProfileID LIKE '00000000-0238-%')");
    await context.Database.ExecuteSqlRawAsync("DELETE FROM Articles WHERE ProfileID LIKE '00000000-0238-%'");
    await context.Database.ExecuteSqlRawAsync("DELETE FROM NovelTaggings WHERE NovelID IN (SELECT NovelID FROM Novels WHERE ProfileID LIKE '00000000-0238-%')");
    await context.Episodes.Where(e => novelIds.Contains(e.NovelID)).ExecuteDeleteAsync();
    await context.Novels.Where(n => novelIds.Contains(n.NovelID)).ExecuteDeleteAsync();
    await context.Database.ExecuteSqlRawAsync("""
        DELETE FROM Tags WHERE IsCategory = 0 AND CreatorProfileID LIKE '00000000-0238-%'
          AND TagId NOT IN (SELECT TagId FROM NovelTaggings) AND TagId NOT IN (SELECT TagId FROM ArticleTaggings);
        UPDATE Tags SET CreatorProfileID = NULL WHERE CreatorProfileID LIKE '00000000-0238-%';
        """);
    await context.Profiles.Where(p => p.ProfileID.StartsWith(Prefix)).ExecuteDeleteAsync();
    await context.Users.Where(u => u.Id.StartsWith(Prefix)).ExecuteDeleteAsync();
    Console.WriteLine($"已清除模擬資料：{ids.Count} 位會員、{novelIds.Count} 部小說");
}

// ── 灌票影響 ──────────────────────────────

async Task SpamAsync()
{
    await using var context = new ApplicationDbContext(options);
    var input = await new RecommendationEngine(context).LoadInputAsync(now);
    var target = input.Contents.First(c => c.Title == "第十三號病房").Key;
    var compare = input.Contents.First(c => c.Title == "午夜公寓").Key;
    var spam = Id(4, 0)[..19];
    var readers = input.Accounts.Select(a => a.ProfileId).Where(id => id.StartsWith(Id(3, 0)[..19], StringComparison.Ordinal)).ToList();
    var clean = new RecommendationInput
    {
        Now = input.Now, Contents = input.Contents, TagNames = input.TagNames, Accounts = input.Accounts,
        Interactions = input.Interactions.Where(i => !i.ProfileId.StartsWith(spam, StringComparison.Ordinal)).ToList(),
        Readings = input.Readings.Where(r => !r.ProfileId.StartsWith(spam, StringComparison.Ordinal)).ToList(),
        AuthorFollows = input.AuthorFollows, NotInterested = input.NotInterested, CategoryTagIds = input.CategoryTagIds, PreferredCategories = input.PreferredCategories,
    };

    Console.WriteLine($"{"情境",-30} {"《第十三號病房》推薦給",12} {"平均名次",6} {"《午夜公寓》推薦給",10}");
    foreach (var (name, data, settings) in new[]
    {
        ("沒有灌票", clean, new RecommendationSettings()),
        ("有灌票，修正前（新帳號照算）", input, new RecommendationSettings { TrustMinAccountAgeDays = 0 }),
        ("有灌票，修正後（未滿 7 天不計）", input, new RecommendationSettings()),
    })
    {
        var corpus = new RecommendationCorpus(data, settings);
        var scorer = new WeightedRecommendationScorer();
        int count = 0, rankSum = 0, other = 0;
        foreach (var reader in readers)
        {
            var top = scorer.Recommend(corpus, reader).Select(r => r.Content).ToList();
            var rank = top.IndexOf(target);
            if (rank >= 0) { count++; rankSum += rank + 1; }
            if (top.Contains(compare)) other++;
        }
        Console.WriteLine($"{name,-30} {count,8} 人 {(count == 0 ? 0 : rankSum / (double)count),8:0.0} {other,10} 人");
    }
}

// ── 文章與追蹤創作者（追蹤文章頁用） ──────────────────────────────

async Task SeedArticlesAsync()
{
    await using var context = new ApplicationDbContext(options);
    await context.Database.ExecuteSqlRawAsync("DELETE FROM Comments WHERE ArticleID IN (SELECT ArticleID FROM Articles WHERE ProfileID LIKE '00000000-0238-%')");
    await context.Database.ExecuteSqlRawAsync("DELETE FROM ArticleTaggings WHERE ArticleID IN (SELECT ArticleID FROM Articles WHERE ProfileID LIKE '00000000-0238-%')");
    await context.Database.ExecuteSqlRawAsync("DELETE FROM Articles WHERE ProfileID LIKE '00000000-0238-%'");
    await context.Follows.Where(f => f.FollowerID.StartsWith(Prefix) && f.ProfileID != null).ExecuteDeleteAsync();

    var random = new Random(2380);
    var tagNames = new[] { "推理", "寫作心得", "讀書心得", "創作技巧", "奇幻" };
    var tags = new Dictionary<string, Tag>();
    foreach (var name in tagNames)
    {
        tags[name] = await context.Tags.FirstOrDefaultAsync(t => t.Name == name)
            ?? context.Tags.Add(new Tag { Name = name, NormalizedName = name.ToUpperInvariant(), CreatorProfileID = Id(2, 1), CreateTime = now }).Entity;
    }

    // 只適用文章的分類（正式環境請在後台「分類」頁新增）
    var nextOrder = (await context.Tags.Where(t => t.IsCategory).MaxAsync(t => (int?)t.CategoryOrder) ?? 0) + 1;
    foreach (var (name, icon) in new[] { ("寫作心得", "fa-pen-nib"), ("讀書心得", "fa-book-open-reader") })
    {
        var tag = tags[name];
        if (!tag.IsCategory)
        {
            tag.IsCategory = true;
            tag.CategoryScope = DataAccess.Common.CategoryScope.Article;
            tag.CategoryOrder = nextOrder++;
            tag.CategoryIcon = icon;
            tag.Status = DataAccess.Common.TagStatus.Approved;
        }
    }

    var samples = new (int Author, string Title, string Body, string[] Tags, bool Image)[]
    {
        (1, "寫推理小說時，我怎麼埋伏筆", "伏筆不是藏起來，而是放在讀者看得到、卻不會特別在意的地方。這篇整理我寫《密室殺人事件簿》時用過的三種手法：重複出現的道具、看似無關的對話，以及刻意安排的時間差。", ["推理", "創作技巧"], true),
        (3, "《雪夜山莊》創作札記：封閉空間的節奏", "封閉空間的故事最怕節奏拖沓。我把每一章都設計成「發現一個新事實」，讓讀者一直有往下讀的理由。", ["推理", "寫作心得"], false),
        (5, "讀書心得：重讀經典本格推理", "最近重讀幾本經典本格推理，發現當年沒注意到的細節。公平性是本格的核心，作者必須把所有線索交給讀者。", ["讀書心得", "推理"], true),
        (7, "連載三個月的心得", "從第一章到第三十章，讀者的留言讓我改了好幾次結局。這篇聊聊連載和一次寫完的差別，以及怎麼面對讀者的期待。", ["寫作心得"], false),
        (1, "角色動機要寫到什麼程度？", "反派的動機寫太滿，懸念就沒了；寫太少，又顯得扁平。我的做法是先寫一份完整的人物小傳，正文只露出三成。", ["創作技巧"], false),
        (3, "奇幻世界觀的設定順序", "先有地圖還是先有角色？我的答案是先有衝突。世界觀是為了讓衝突成立而存在的。", ["奇幻", "創作技巧"], true),
        (5, "這個月讀了五本書", "簡單記錄這個月讀的五本書，其中兩本推理、兩本奇幻、一本散文。每本附上一句話的感想。", ["讀書心得"], false),
        (7, "投票功能讓讀者決定劇情之後", "第一次開放讀者投票決定主角要不要背叛同伴，結果出乎我意料。這篇分享投票前後的數據和讀者反應。", ["寫作心得"], true),
        (1, "短篇推理練習：五百字的密室", "用五百字寫一個完整的密室推理，是很好的練習。這篇附上我的練習稿和寫完後的檢討。", ["推理", "寫作心得"], false),
        (3, "長標題測試：當文章標題長到需要換行的時候，列表應該只顯示兩行並以省略號結尾，避免版面被撐開", "這篇用來測試很長的標題和很長的內文。內文會被截成預覽，只顯示前兩行，後面的文字以省略號表示。內文繼續寫下去，直到超過預覽的長度。內文繼續寫下去，直到超過預覽的長度。內文繼續寫下去，直到超過預覽的長度。", ["寫作心得"], false),
    };

    var commenters = Enumerable.Range(1, 30).Select(i => Id(3, i)).ToList();
    for (var i = 0; i < samples.Length; i++)
    {
        var sample = samples[i];
        var article = new DataAccess.Models.ArticleModel.Article
        {
            Title = sample.Title,
            Content = $"<p>{sample.Body}</p><p>【測試資料】</p>",
            ProfileID = Id(2, sample.Author),
            CreateTime = now.AddDays(-i * 2 - 1),
            UpdateTime = now.AddHours(-i * 20 - 1),
            TotalViews = random.Next(5, 800),
            ImageUrl = sample.Image ? $"https://picsum.photos/seed/involver-sim-a{i}/400/300" : null,
            Type = DataAccess.Common.ArticleType.General,
            Tags = sample.Tags.Select(t => tags[t]).ToList(),
        };
        context.Articles.Add(article);
        foreach (var commenter in commenters.Take(random.Next(0, 6)))
        {
            context.Comments.Add(new Comment { Content = "<p>測試留言</p>", ProfileID = commenter, Article = article, UpdateTime = now.AddHours(-i) });
        }
    }

    // 推理讀者追蹤 4 位創作者；奇幻讀者追蹤 1 位但他沒有文章；其他人物沒有追蹤
    foreach (var author in new[] { 1, 3, 5, 7 })
    {
        context.Follows.Add(new Follow { FollowerID = PersonaMysteryReader, ProfileID = Id(2, author), UpdateTime = now.AddDays(-10) });
    }

    context.Follows.Add(new Follow { FollowerID = PersonaFantasyReader, ProfileID = Id(2, 9), UpdateTime = now.AddDays(-10) });
    await context.SaveChangesAsync();
    Console.WriteLine($"灌入 {samples.Length} 篇文章，推理讀者追蹤 4 位創作者");
}

static partial class Program
{
    internal static readonly Dictionary<NovelType, string> TypeNames = new()
    {
        [NovelType.Fantasy] = "奇幻",
        [NovelType.History] = "歷史",
        [NovelType.Love] = "愛情",
        [NovelType.Real] = "真實",
        [NovelType.Modern] = "現代",
        [NovelType.Science] = "科幻",
        [NovelType.Horror] = "驚悚",
        [NovelType.Detective] = "推理",
    };
}

enum NovelType { Fantasy, History, Love, Real, Modern, Science, Horror, Detective }
