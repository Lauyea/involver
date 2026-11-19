namespace DataAccess.Services.Recommendations
{
    /// <summary>
    /// 預設的推薦評分：兩階段（產生候選 → 評分排序），簡單、可解釋。
    /// <code>
    /// score = w1 × 內容相似度 + w2 × 協同訊號 + w3 × 熱度 + w4 × 支持強度 + w5 × 品質
    ///       + 新鮮度加成 + 追蹤作者加成 − 不感興趣作者扣分
    /// </code>
    /// 已讀過、已追蹤、不感興趣、自己的作品直接排除；同一作者最多出現 <see cref="RecommendationSettings.MaxPerAuthor"/> 次；
    /// 保留 <see cref="RecommendationSettings.ExplorationSlots"/> 個名額給新作。
    /// </summary>
    public sealed class WeightedRecommendationScorer : IRecommendationScorer
    {
        /// <summary>
        /// 推薦理由裡作品標題的長度上限
        /// </summary>
        public const int ReasonTitleMaxLength = 30;

        /// <summary>
        /// 新作探索名額的推薦理由
        /// </summary>
        public const string ExplorationReason = "新作推薦";

        /// <summary>
        /// 每一項都沒有分數時的推薦理由
        /// </summary>
        public const string FallbackReason = "你可能會喜歡";

        /// <inheritdoc />
        public IReadOnlyList<ScoredRecommendation> Recommend(RecommendationCorpus corpus, string profileId)
        {
            var user = corpus.UserOf(profileId);
            if (!user.HasSignal)
            {
                return [];
            }

            var settings = corpus.Settings;
            var candidates = GenerateCandidates(corpus, user);
            var scored = candidates
                .Select(key => Score(corpus, user, key))
                .OrderByDescending(s => s.Score)
                .ThenBy(s => s.Content)
                .ToList();

            var resultCount = settings.ResultCount;
            var slots = Math.Min(settings.ExplorationSlots, resultCount);
            var newWorks = corpus.NewTop.ToHashSet();
            var authorCounts = new Dictionary<string, int>(StringComparer.Ordinal);

            var main = Pick(corpus, scored, resultCount - slots, authorCounts, picked: []);
            var pickedKeys = main.Select(m => m.Content).ToHashSet();
            var exploration = Pick(
                    corpus,
                    scored.Where(s => newWorks.Contains(s.Content)),
                    slots,
                    authorCounts,
                    pickedKeys)
                .Select(s => s with { Reason = ExplorationReason })
                .ToList();

            // 新作不夠時，名額還給一般推薦
            if (exploration.Count < slots)
            {
                pickedKeys.UnionWith(exploration.Select(e => e.Content));
                main.AddRange(Pick(corpus, scored, slots - exploration.Count, authorCounts, pickedKeys));
            }

            return Interleave(main, exploration);
        }

        /// <summary>
        /// 第一階段：產生候選。追蹤作者的新作、相同標籤／主分類、共同觀看、熱門、新作
        /// </summary>
        private static HashSet<ContentKey> GenerateCandidates(RecommendationCorpus corpus, RecommendationCorpus.UserState user)
        {
            var settings = corpus.Settings;
            var candidates = new HashSet<ContentKey>();
            var newSince = corpus.Input.Now.AddDays(-settings.NewWorkDays);

            foreach (var item in corpus.Contents.Values)
            {
                if (user.FollowedAuthors.Contains(item.AuthorId) && item.UpdatedAt >= newSince)
                {
                    candidates.Add(item.Key);
                }
            }

            foreach (var feature in user.Preference.Keys)
            {
                candidates.UnionWith(corpus.ItemsWithFeature(feature));
            }

            foreach (var engaged in user.Engagement.Keys)
            {
                candidates.UnionWith(corpus.NeighborsOf(engaged).Select(n => n.Other));
            }

            candidates.UnionWith(corpus.PopularTop);
            candidates.UnionWith(corpus.NewTop);

            candidates.RemoveWhere(key =>
                user.Seen.Contains(key)
                || user.NotInterested.Contains(key)
                || corpus.Contents[key].AuthorId == user.ProfileId);

            return candidates;
        }

        /// <summary>
        /// 第二階段：評分，並以貢獻最大的一項當作推薦理由
        /// </summary>
        private static ScoredRecommendation Score(RecommendationCorpus corpus, RecommendationCorpus.UserState user, ContentKey key)
        {
            var settings = corpus.Settings;
            var item = corpus.Contents[key];
            var parts = new List<(double Value, string Reason)>();

            var (contentSimilarity, topFeature) = ContentSimilarity(corpus, user, key);
            parts.Add((settings.ContentWeight * contentSimilarity, ContentReason(corpus, topFeature)));

            var (collaborative, because) = Collaborative(corpus, user, key);
            parts.Add((settings.CollaborativeWeight * collaborative,
                because is { } source ? $"因為你讀過《{Shorten(corpus.Contents[source].Title)}》" : string.Empty));

            parts.Add((settings.PopularityWeight * corpus.NormalizedPopularity(key), "最近很多人在看"));
            parts.Add((settings.SupportWeight * corpus.NormalizedSupport(key), "很多讀者 Involve 支持"));
            parts.Add((settings.QualityWeight * corpus.QualityOf(key), "讀者讀完率高"));
            parts.Add((settings.FreshnessWeight * corpus.FreshnessOf(key), "最近更新"));

            if (user.FollowedAuthors.Contains(item.AuthorId))
            {
                parts.Add((settings.FollowedAuthorWeight, $"你追蹤的作者 {Shorten(item.AuthorName)}"));
            }

            var penalty = user.DislikedAuthors.Contains(item.AuthorId) ? settings.DislikedAuthorPenalty : 0;
            var score = parts.Sum(p => p.Value) - penalty;

            // 第一個最大值當理由，順序固定，結果可重現
            var reason = parts
                .Where(p => p.Reason.Length > 0 && p.Value > 0)
                .DefaultIfEmpty((0, FallbackReason))
                .Aggregate((best, next) => next.Value > best.Value ? next : best)
                .Reason;

            return new ScoredRecommendation(key, Math.Round(score, 6), reason);
        }

        /// <summary>
        /// 內容相似度：偏好向量與作品向量的內積（兩者都是單位向量，結果在 0～1）
        /// </summary>
        private static (double Similarity, int? TopFeature) ContentSimilarity(
            RecommendationCorpus corpus,
            RecommendationCorpus.UserState user,
            ContentKey key)
        {
            var similarity = 0.0;
            int? topFeature = null;
            var topValue = 0.0;

            foreach (var (feature, weight) in corpus.ItemVector(key).OrderBy(f => f.Key))
            {
                if (!user.Preference.TryGetValue(feature, out var preference) || preference <= 0)
                {
                    continue;
                }

                var value = weight * preference;
                similarity += value;
                if (value > topValue)
                {
                    topValue = value;
                    topFeature = feature;
                }
            }

            return (Math.Clamp(similarity, 0, 1), topFeature);
        }

        /// <summary>
        /// 協同訊號：會員互動過的作品中，與候選共同觀看相似度最高的一部
        /// </summary>
        private static (double Similarity, ContentKey? Source) Collaborative(
            RecommendationCorpus corpus,
            RecommendationCorpus.UserState user,
            ContentKey key)
        {
            var best = 0.0;
            ContentKey? source = null;

            foreach (var (other, similarity) in corpus.NeighborsOf(key))
            {
                if (!user.Engagement.ContainsKey(other))
                {
                    continue;
                }

                // 鄰居已依相似度、ID 排序，第一個符合的就是最大值
                best = similarity;
                source = other;
                break;
            }

            return (best, source);
        }

        private static string ContentReason(RecommendationCorpus corpus, int? feature)
        {
            if (feature is not { } f)
            {
                return string.Empty;
            }

            if (!corpus.Input.TagNames.TryGetValue(f, out var name))
            {
                return string.Empty;
            }

            return corpus.IsCategory(f) ? $"你喜歡{name}類作品" : $"你常看 #{name}";
        }

        /// <summary>
        /// 依序挑選，同一作者不超過上限
        /// </summary>
        private static List<ScoredRecommendation> Pick(
            RecommendationCorpus corpus,
            IEnumerable<ScoredRecommendation> ordered,
            int count,
            Dictionary<string, int> authorCounts,
            HashSet<ContentKey> picked)
        {
            var result = new List<ScoredRecommendation>();
            if (count <= 0)
            {
                return result;
            }

            foreach (var candidate in ordered)
            {
                if (picked.Contains(candidate.Content))
                {
                    continue;
                }

                var author = corpus.Contents[candidate.Content].AuthorId;
                var used = authorCounts.GetValueOrDefault(author);
                if (used >= corpus.Settings.MaxPerAuthor)
                {
                    continue;
                }

                authorCounts[author] = used + 1;
                result.Add(candidate);
                if (result.Count >= count)
                {
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// 把探索名額平均插進一般推薦之間
        /// </summary>
        private static List<ScoredRecommendation> Interleave(List<ScoredRecommendation> main, List<ScoredRecommendation> exploration)
        {
            if (exploration.Count == 0)
            {
                return main;
            }

            var total = main.Count + exploration.Count;
            var positions = Enumerable.Range(1, exploration.Count)
                .Select(i => (int)Math.Round((double)i * total / (exploration.Count + 1)))
                .ToHashSet();

            var result = new List<ScoredRecommendation>(total);
            var m = 0;
            var e = 0;
            for (var i = 0; i < total; i++)
            {
                if ((positions.Contains(i) && e < exploration.Count) || m >= main.Count)
                {
                    result.Add(exploration[e++]);
                }
                else
                {
                    result.Add(main[m++]);
                }
            }

            return result;
        }

        private static string Shorten(string text) =>
            text.Length <= ReasonTitleMaxLength ? text : $"{text[..(ReasonTitleMaxLength - 1)]}…";
    }
}
