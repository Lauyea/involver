using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

using DataAccess.Common;
using DataAccess.Data;
using DataAccess.Models;

using Involver.Authorization.Tag;
using Involver.Helpers;

using Microsoft.EntityFrameworkCore;

namespace Involver.Services;

/// <inheritdoc />
public class TagService(ApplicationDbContext context) : ITagService
{
    /// <summary>
    /// 相似標籤最多回傳幾筆
    /// </summary>
    private const int SimilarResultLimit = 5;

    /// <summary>
    /// 取得作品類型的標籤上限
    /// </summary>
    /// <param name="target">作品類型</param>
    /// <returns>每部作品最多的標籤數</returns>
    public static int GetMaxTags(TagTarget target)
    {
        return target == TagTarget.Novel ? Parameters.NovelTagSize : Parameters.ArticleTagSize;
    }

    /// <summary>
    /// 解析 Tagify 的值。Tagify 送出的是 [{"value":"標籤"}] 格式的 JSON；不是 JSON 時視為以逗號分隔的標籤。
    /// </summary>
    /// <param name="tagString">表單送出的標籤字串</param>
    /// <returns>整理後的顯示名稱（已去掉空白項目，尚未去重複）</returns>
    public static List<string> ParseTagifyValue(string tagString)
    {
        if (string.IsNullOrWhiteSpace(tagString))
        {
            return [];
        }

        IEnumerable<string> names;
        try
        {
            var tagObjects = JsonSerializer.Deserialize<List<TagifyTag>>(tagString);
            names = tagObjects?.Select(t => t?.Value) ?? [];
        }
        catch (JsonException)
        {
            names = tagString.Split([',', '，', '、']);
        }

        return names
            .Select(TagNameNormalizer.CleanDisplayName)
            .Where(n => n.Length > 0)
            .ToList();
    }

    /// <summary>
    /// 檢查單一標籤名稱是否符合長度與格式規定
    /// </summary>
    /// <param name="displayName">整理後的顯示名稱</param>
    /// <param name="normalizedName">統一格式後的名稱</param>
    /// <returns>不符合時的原因；符合時為 null</returns>
    public static string ValidateName(string displayName, string normalizedName)
    {
        if (displayName.Length > Parameters.TagNameMaxLength)
        {
            return $"標籤「{displayName}」超過 {Parameters.TagNameMaxLength} 個字，請重新設定";
        }

        if (normalizedName.Length == 0)
        {
            return $"標籤「{displayName}」只有空白或標點，請重新設定";
        }

        if (normalizedName.Length > Parameters.TagNormalizedNameMaxLength)
        {
            return $"標籤「{displayName}」太長，請重新設定";
        }

        return null;
    }

    /// <inheritdoc />
    public async Task<TagAuthorLevel> GetAuthorLevelAsync(ClaimsPrincipal user, string profileId, CancellationToken cancellationToken = default)
    {
        var profile = await context.Profiles
            .Where(p => p.ProfileID == profileId)
            .Select(p => new { p.Banned, p.Professional })
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (profile == null || profile.Banned)
        {
            return TagAuthorLevel.Banned;
        }

        if (profile.Professional || TagManagers.IsTagManager(user))
        {
            return TagAuthorLevel.Trusted;
        }

        return TagAuthorLevel.Author;
    }

    /// <inheritdoc />
    public async Task<TagResolveResult> ResolveTagsAsync(
        string tagString,
        TagTarget target,
        string profileId,
        TagAuthorLevel level,
        IReadOnlyCollection<int> currentTagIds = null,
        CancellationToken cancellationToken = default)
    {
        currentTagIds ??= [];

        // 統一格式後相同的標籤只留第一個
        var requested = new List<(string Name, string NormalizedName)>();
        foreach (var name in ParseTagifyValue(tagString))
        {
            var normalizedName = TagNameNormalizer.Normalize(name);
            var error = ValidateName(name, normalizedName);
            if (error != null)
            {
                return TagResolveResult.Fail(error);
            }

            if (requested.All(r => r.NormalizedName != normalizedName))
            {
                requested.Add((name, normalizedName));
            }
        }

        var maxTags = GetMaxTags(target);
        if (requested.Count > maxTags)
        {
            return TagResolveResult.Fail($"設定標籤超過 {maxTags} 個，請重新設定");
        }

        var tags = new List<Tag>();
        var newNames = new List<(string Name, string NormalizedName)>();

        foreach (var item in requested)
        {
            // 比對用資料庫的定序，與 Unique 索引一致
            var existing = await context.Tags
                .FirstOrDefaultAsync(t => t.NormalizedName == item.NormalizedName, cancellationToken);

            // 舊資料的 NormalizedName 是 migration 用 SQL 近似算出來的，可能和程式算的不同；名稱完全相同時仍視為同一個標籤
            existing ??= await context.Tags
                .FirstOrDefaultAsync(t => t.Name == item.Name, cancellationToken);

            if (existing == null)
            {
                newNames.Add(item);
                continue;
            }

            if (level == TagAuthorLevel.Banned && !currentTagIds.Contains(existing.TagId))
            {
                return TagResolveResult.Fail("帳號已被封鎖，不能設定標籤");
            }

            if (tags.All(t => t.TagId != existing.TagId))
            {
                tags.Add(existing);
            }
        }

        if (newNames.Count == 0)
        {
            return new TagResolveResult { Tags = tags };
        }

        if (level == TagAuthorLevel.Banned)
        {
            return TagResolveResult.Fail("帳號已被封鎖，不能新增標籤");
        }

        if (level == TagAuthorLevel.Author)
        {
            var today = DateTime.Today;
            var createdToday = await context.Tags
                .Where(t => t.CreatorProfileID == profileId && t.CreateTime >= today)
                .CountAsync(cancellationToken);

            if (createdToday + newNames.Count > Parameters.TagDailyCreateLimit)
            {
                return TagResolveResult.Fail($"每天最多只能申請 {Parameters.TagDailyCreateLimit} 個新標籤，請先使用現有標籤");
            }
        }

        // 送出時後端再檢查一次：只差結尾數字或符號的標籤不能新增
        var existingNames = await context.Tags
            .Select(t => new { t.Name, t.NormalizedName })
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var compared = existingNames
            .Select(t => (t.Name, t.NormalizedName))
            .ToList();

        var createdTags = new List<Tag>();
        foreach (var item in newNames)
        {
            foreach (var other in compared)
            {
                var (kind, _) = TagSimilarity.Compare(item.NormalizedName, other.NormalizedName);
                if (kind == TagMatchKind.Duplicate)
                {
                    return TagResolveResult.Fail(
                        $"標籤「{item.Name}」和現有標籤「{other.Name}」只差結尾的數字或符號，請直接使用「{other.Name}」");
                }
            }

            var tag = new Tag
            {
                Name = item.Name,
                NormalizedName = item.NormalizedName,
                Status = level == TagAuthorLevel.Trusted ? TagStatus.Approved : TagStatus.Pending,
                CreatorProfileID = profileId,
                CreateTime = DateTime.Now,
            };

            context.Tags.Add(tag);
            createdTags.Add(tag);
            tags.Add(tag);

            // 同一次送出的新標籤之間也要互相比對（例如同時新增「讀書心得1」和「讀書心得2」）
            compared.Add((item.Name, item.NormalizedName));
        }

        return new TagResolveResult { Tags = tags, CreatedTags = createdTags };
    }

    /// <inheritdoc />
    public async Task<int> PromotePendingTagsAsync(IEnumerable<int> tagIds, CancellationToken cancellationToken = default)
    {
        var ids = tagIds?.Distinct().ToList() ?? [];
        if (ids.Count == 0)
        {
            return 0;
        }

        var pendingIds = await context.Tags
            .Where(t => ids.Contains(t.TagId) && t.Status == TagStatus.Pending)
            .Select(t => t.TagId)
            .ToListAsync(cancellationToken);

        var promoted = 0;
        foreach (var tagId in pendingIds)
        {
            // Union 會去掉重複，得到不同作者的數量
            var authorCount = await context.Novels
                .Where(n => !n.IsDeleted && n.Tags.Any(t => t.TagId == tagId))
                .Select(n => n.ProfileID)
                .Union(context.Articles
                    .Where(a => !a.IsDeleted && a.Tags.Any(t => t.TagId == tagId))
                    .Select(a => a.ProfileID))
                .CountAsync(cancellationToken);

            if (authorCount >= Parameters.TagAutoApproveAuthorCount)
            {
                promoted += await UpdateStatusCoreAsync(tagId, TagStatus.Approved, cancellationToken);
            }
        }

        return promoted;
    }

    /// <inheritdoc />
    public async Task<bool> UpdateStatusAsync(int tagId, TagStatus status, CancellationToken cancellationToken = default)
    {
        return await UpdateStatusCoreAsync(tagId, status, cancellationToken) > 0;
    }

    /// <inheritdoc />
    public async Task<List<TagSuggestion>> GetSuggestionsAsync(TagTarget target, CancellationToken cancellationToken = default)
    {
        var tags = await QueryTagUsages()
            .Where(t => t.Status == TagStatus.Approved)
            .ToListAsync(cancellationToken);

        var other = target == TagTarget.Novel ? TagTarget.Article : TagTarget.Novel;

        // 這個類型用過的標籤排前面，再依使用次數排序；另一類型的使用次數只用來排後段
        return tags
            .OrderByDescending(t => t.CountOf(target) > 0)
            .ThenByDescending(t => t.CountOf(target))
            .ThenByDescending(t => t.CountOf(other))
            .ThenBy(t => t.Name, StringComparer.Ordinal)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<TagSimilarResult> FindSimilarAsync(string name, TagTarget target, CancellationToken cancellationToken = default)
    {
        var displayName = TagNameNormalizer.CleanDisplayName(name);
        var normalizedName = TagNameNormalizer.Normalize(displayName);

        var error = ValidateName(displayName, normalizedName);
        if (error != null)
        {
            return new TagSimilarResult { Name = displayName, NormalizedName = normalizedName, ErrorMessage = error };
        }

        var tags = await QueryTagUsages().ToListAsync(cancellationToken);

        TagMatch existing = null;
        var duplicates = new List<TagMatch>();
        var similar = new List<TagMatch>();

        foreach (var tag in tags)
        {
            var (kind, score) = TagSimilarity.Compare(normalizedName, tag.NormalizedName);
            if (kind == TagMatchKind.None && tag.Name != displayName)
            {
                continue;
            }

            var match = new TagMatch
            {
                TagId = tag.TagId,
                Name = tag.Name,
                IsPending = tag.Status == TagStatus.Pending,
                Score = Math.Round(score, 3),
                UsageCount = tag.CountOf(target),
            };

            if (kind == TagMatchKind.Same || tag.Name == displayName)
            {
                existing ??= match;
            }
            else if (kind == TagMatchKind.Duplicate)
            {
                duplicates.Add(match);
            }
            else
            {
                similar.Add(match);
            }
        }

        var baseName = TagSimilarity.StripTrailingNumber(normalizedName);

        return new TagSimilarResult
        {
            Name = displayName,
            NormalizedName = normalizedName,
            Existing = existing,
            // 優先建議沒有編號的那個（例如「讀書心得1」→「讀書心得」），其次是用得最多的
            Duplicate = existing == null
                ? duplicates
                    .OrderByDescending(d => TagNameNormalizer.Normalize(d.Name) == baseName)
                    .ThenByDescending(d => d.UsageCount)
                    .FirstOrDefault()
                : null,
            Similar = existing == null
                ? similar
                    .OrderByDescending(s => s.Score)
                    .ThenByDescending(s => s.UsageCount)
                    .Take(SimilarResultLimit)
                    .ToList()
                : [],
        };
    }

    /// <inheritdoc />
    public async Task<List<TagDuplicateReportRow>> GetDuplicateReportAsync(CancellationToken cancellationToken = default)
    {
        var tags = await QueryTagUsages().ToListAsync(cancellationToken);
        var rows = new List<TagDuplicateReportRow>();

        foreach (var tag in tags)
        {
            var expected = TagNameNormalizer.Normalize(tag.Name);
            if (expected != tag.NormalizedName)
            {
                rows.Add(new TagDuplicateReportRow
                {
                    Kind = "NormalizedNameMismatch",
                    TagA = tag,
                    Note = expected,
                });
            }
        }

        for (var i = 0; i < tags.Count; i++)
        {
            for (var j = i + 1; j < tags.Count; j++)
            {
                var a = tags[i];
                var b = tags[j];

                // 「重複」是有方向的（有編號的那個對沒編號的），兩個方向都比，取較嚴重的
                var (kindAb, scoreAb) = TagSimilarity.Compare(a.NormalizedName, b.NormalizedName);
                var (kindBa, scoreBa) = TagSimilarity.Compare(b.NormalizedName, a.NormalizedName);
                var kind = (TagMatchKind)Math.Max((int)kindAb, (int)kindBa);
                if (kind == TagMatchKind.None)
                {
                    continue;
                }

                rows.Add(new TagDuplicateReportRow
                {
                    Kind = TagDuplicateReportRow.KindOf(kind),
                    TagA = a,
                    TagB = b,
                    Score = Math.Round(Math.Max(scoreAb, scoreBa), 3),
                });
            }
        }

        return rows
            .OrderBy(r => r.Kind switch
            {
                "NormalizedNameMismatch" => 0,
                "Same" => 1,
                "Duplicate" => 2,
                _ => 3,
            })
            .ThenByDescending(r => r.Score)
            .ToList();
    }

    /// <summary>
    /// 標籤與小說、文章的使用次數（不含軟刪除的作品）
    /// </summary>
    private IQueryable<TagSuggestion> QueryTagUsages()
    {
        return context.Tags
            .Select(t => new TagSuggestion
            {
                TagId = t.TagId,
                Name = t.Name,
                NormalizedName = t.NormalizedName,
                Status = t.Status,
                NovelCount = t.Novels!.Count(n => !n.IsDeleted),
                ArticleCount = t.Articles!.Count(a => !a.IsDeleted),
            })
            .AsNoTracking();
    }

    private Task<int> UpdateStatusCoreAsync(int tagId, TagStatus status, CancellationToken cancellationToken)
    {
        return context.Tags
            .Where(t => t.TagId == tagId)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.Status, status), cancellationToken);
    }

    /// <summary>
    /// Tagify 送出的單一標籤
    /// </summary>
    private sealed class TagifyTag
    {
        [JsonPropertyName("value")]
        public string Value { get; set; }
    }
}
