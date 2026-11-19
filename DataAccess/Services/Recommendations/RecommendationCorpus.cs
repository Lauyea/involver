
namespace DataAccess.Services.Recommendations
{
    /// <summary>
    /// 從 <see cref="RecommendationInput"/> 整理出評分要用的訊號。每次排程只建立一次，所有會員共用。
    /// <para>防灌規則都在這裡處理：</para>
    /// <list type="bullet">
    /// <item>所有次數都是「不重複會員」，同一人對同一作品只算一次（貢獻上限為該帳號的可信度，最多 1）。</item>
    /// <item>依帳號可信度加權（註冊天數、是否付費過），Banned 為 0。</item>
    /// <item>作者對自己作品的行為不計。</item>
    /// </list>
    /// </summary>
    public sealed class RecommendationCorpus
    {
        /// <summary>
        /// 每位會員最多拿最近幾部作品計算共同觀看，避免重度使用者讓計算量爆增
        /// </summary>
        public const int MaxItemsPerUserForCoView = 200;

        private readonly Dictionary<ContentKey, ContentItem> _contents;
        private readonly Dictionary<string, double> _trust;
        private readonly Dictionary<ContentKey, double> _popularity = new();
        private readonly Dictionary<ContentKey, double> _support = new();
        private readonly Dictionary<ContentKey, double> _quality = new();
        private readonly Dictionary<ContentKey, Dictionary<int, double>> _itemVectors = new();
        private readonly Dictionary<int, double> _idf = new();
        private readonly Dictionary<int, List<ContentKey>> _featureIndex = new();
        private readonly Dictionary<ContentKey, List<(ContentKey Other, double Similarity)>> _neighbors = new();
        private readonly Dictionary<string, UserState> _users = new(StringComparer.Ordinal);
        private readonly double _maxLogPopularity;
        private readonly double _maxLogSupport;

        /// <summary>
        /// 建立訊號
        /// </summary>
        /// <param name="input">原始資料</param>
        /// <param name="settings">權重與門檻</param>
        public RecommendationCorpus(RecommendationInput input, RecommendationSettings settings)
        {
            Input = input;
            Settings = settings;
            _contents = input.Contents.ToDictionary(c => c.Key);
            _trust = input.Accounts.ToDictionary(a => a.ProfileId, a => TrustOf(a, input.Now, settings), StringComparer.Ordinal);

            BuildUsers();
            BuildPopularityAndSupport();
            BuildQuality();
            BuildFeatures();
            BuildCoViews();

            _maxLogPopularity = _popularity.Count == 0 ? 0 : _popularity.Values.Max(p => Math.Log(1 + p));
            _maxLogSupport = _support.Count == 0 ? 0 : _support.Values.Max(s => Math.Log(1 + s));

            PopularTop = _contents.Keys
                .Where(k => PopularityOf(k) > 0)
                .OrderByDescending(PopularityOf)
                .ThenBy(k => k)
                .Take(settings.PopularCandidates)
                .ToList();

            var newSince = input.Now.AddDays(-settings.NewWorkDays);
            NewTop = _contents.Values
                .Where(c => c.CreatedAt >= newSince)
                .OrderByDescending(c => c.CreatedAt)
                .ThenBy(c => c.Key)
                .Take(settings.NewCandidates)
                .Select(c => c.Key)
                .ToList();
        }

        /// <summary>
        /// 原始資料
        /// </summary>
        public RecommendationInput Input { get; }

        /// <summary>
        /// 權重與門檻
        /// </summary>
        public RecommendationSettings Settings { get; }

        /// <summary>
        /// 熱門候選（依熱度）
        /// </summary>
        public IReadOnlyList<ContentKey> PopularTop { get; }

        /// <summary>
        /// 新作候選（依建立時間）
        /// </summary>
        public IReadOnlyList<ContentKey> NewTop { get; }

        /// <summary>
        /// 可以被推薦的內容
        /// </summary>
        public IReadOnlyDictionary<ContentKey, ContentItem> Contents => _contents;

        /// <summary>
        /// 帳號可信度：Banned、不存在或註冊未滿 <see cref="RecommendationSettings.TrustMinAccountAgeDays"/> 天為 0；
        /// 否則為基本值加上註冊天數、付費過的加分，最多 1
        /// </summary>
        /// <param name="account">帳號</param>
        /// <param name="now">現在時間</param>
        /// <param name="settings">設定</param>
        /// <returns>0～1</returns>
        public static double TrustOf(ReaderAccount account, DateTime now, RecommendationSettings settings)
        {
            var accountAgeDays = (now - account.EnrolledAt).TotalDays;
            if (account.Banned || accountAgeDays < settings.TrustMinAccountAgeDays)
            {
                return 0;
            }

            var trust = settings.TrustBase;
            if (accountAgeDays >= settings.TrustAccountAgeDays)
            {
                trust += settings.TrustAgeBonus;
            }

            if (account.HasPaid)
            {
                trust += settings.TrustPaidBonus;
            }

            return Math.Clamp(trust, 0, 1);
        }

        /// <summary>
        /// 會員的可信度；沒有帳號資料時為 0
        /// </summary>
        /// <param name="profileId">會員</param>
        /// <returns>0～1</returns>
        public double TrustOf(string profileId) => _trust.GetValueOrDefault(profileId);

        /// <summary>
        /// 熱度原始值：不重複讀者的可信度加總，依最後互動時間衰減
        /// </summary>
        /// <param name="key">內容</param>
        /// <returns>熱度</returns>
        public double PopularityOf(ContentKey key) => _popularity.GetValueOrDefault(key);

        /// <summary>
        /// 熱度（0～1）：取 log 後除以全站最大值
        /// </summary>
        /// <param name="key">內容</param>
        /// <returns>0～1</returns>
        public double NormalizedPopularity(ContentKey key) =>
            _maxLogPopularity <= 0 ? 0 : Math.Log(1 + PopularityOf(key)) / _maxLogPopularity;

        /// <summary>
        /// 支持強度原始值：不同 Involve 者的可信度加總
        /// </summary>
        /// <param name="key">內容</param>
        /// <returns>支持強度</returns>
        public double SupportOf(ContentKey key) => _support.GetValueOrDefault(key);

        /// <summary>
        /// 支持強度（0～1）：取 log 後除以全站最大值
        /// </summary>
        /// <param name="key">內容</param>
        /// <returns>0～1</returns>
        public double NormalizedSupport(ContentKey key) =>
            _maxLogSupport <= 0 ? 0 : Math.Log(1 + SupportOf(key)) / _maxLogSupport;

        /// <summary>
        /// 品質（0～1）：平滑後的讀完率與追蹤轉換率，各自除以全站最大值後平均
        /// </summary>
        /// <param name="key">內容</param>
        /// <returns>0～1</returns>
        public double QualityOf(ContentKey key) => _quality.GetValueOrDefault(key);

        /// <summary>
        /// 新鮮度（0～1）：依更新時間衰減
        /// </summary>
        /// <param name="key">內容</param>
        /// <returns>0～1</returns>
        public double FreshnessOf(ContentKey key)
        {
            var item = _contents[key];
            var ageDays = Math.Max(0, (Input.Now - item.UpdatedAt).TotalDays);
            return Math.Pow(0.5, ageDays / Settings.FreshnessHalfLifeDays);
        }

        /// <summary>
        /// 內容的特徵向量（標籤與主分類，TF-IDF 加權並正規化）
        /// </summary>
        /// <param name="key">內容</param>
        /// <returns>特徵 → 權重</returns>
        public IReadOnlyDictionary<int, double> ItemVector(ContentKey key) => _itemVectors[key];

        /// <summary>
        /// 有某個特徵的內容
        /// </summary>
        /// <param name="feature">特徵</param>
        /// <returns>內容</returns>
        public IReadOnlyList<ContentKey> ItemsWithFeature(int feature) =>
            _featureIndex.TryGetValue(feature, out var items) ? items : [];

        /// <summary>
        /// 共同觀看的相似內容（相似度為讀者集合的 cosine）
        /// </summary>
        /// <param name="key">內容</param>
        /// <returns>相似內容</returns>
        public IReadOnlyList<(ContentKey Other, double Similarity)> NeighborsOf(ContentKey key) =>
            _neighbors.TryGetValue(key, out var list) ? list : [];

        /// <summary>
        /// 會員的狀態；沒有任何資料時回傳空狀態
        /// </summary>
        /// <param name="profileId">會員</param>
        /// <returns>狀態</returns>
        public UserState UserOf(string profileId) =>
            _users.TryGetValue(profileId, out var state) ? state : new UserState(profileId);

        /// <summary>
        /// 特徵（標籤 ID）是否為分類標籤
        /// </summary>
        /// <param name="feature">標籤 ID</param>
        /// <returns>是否為分類標籤</returns>
        public bool IsCategory(int feature) => Input.CategoryTagIds.Contains(feature);

        /// <summary>
        /// 作品的原始特徵（未加權）：標籤 ID，分類標籤也是標籤
        /// </summary>
        private static IEnumerable<int> RawFeatures(ContentItem item) => item.TagIds.Distinct();

        private UserState GetOrAddUser(string profileId)
        {
            if (!_users.TryGetValue(profileId, out var state))
            {
                state = new UserState(profileId);
                _users[profileId] = state;
            }

            return state;
        }

        private double SignalWeight(InteractionKind kind) => kind switch
        {
            InteractionKind.Follow => Settings.FollowSignalWeight,
            InteractionKind.Involve => Settings.InvolveSignalWeight,
            _ => Settings.ViewSignalWeight,
        };

        /// <summary>
        /// 整理每位會員的互動、追蹤的作者、不感興趣、自選主分類
        /// </summary>
        private void BuildUsers()
        {
            // 同一人對同一內容的同一種互動只算一次
            foreach (var group in Input.Interactions
                .Where(i => _contents.ContainsKey(i.Content))
                .GroupBy(i => (i.ProfileId, i.Content, i.Kind)))
            {
                var (profileId, content, kind) = group.Key;
                var state = GetOrAddUser(profileId);
                var latest = group.Max(i => i.At);

                state.Seen.Add(content);

                // 作者對自己作品的行為不計入偏好
                if (_contents[content].AuthorId == profileId)
                {
                    continue;
                }

                state.Engagement[content] = state.Engagement.GetValueOrDefault(content) + SignalWeight(kind);
                state.LastEngagedAt[content] = state.LastEngagedAt.TryGetValue(content, out var at) && at > latest ? at : latest;
            }

            foreach (var (followerId, authorId) in Input.AuthorFollows)
            {
                GetOrAddUser(followerId).FollowedAuthors.Add(authorId);
            }

            foreach (var (profileId, content) in Input.NotInterested)
            {
                var state = GetOrAddUser(profileId);
                state.NotInterested.Add(content);
                if (_contents.TryGetValue(content, out var item))
                {
                    state.DislikedAuthors.Add(item.AuthorId);
                }
            }

            foreach (var (profileId, tagId) in Input.PreferredCategories)
            {
                GetOrAddUser(profileId).PreferredCategories.Add(tagId);
            }
        }

        /// <summary>
        /// 熱度與支持強度：不重複、可信度加權、作者本人不計
        /// </summary>
        private void BuildPopularityAndSupport()
        {
            foreach (var group in Input.Interactions
                .Where(i => _contents.ContainsKey(i.Content))
                .GroupBy(i => (i.ProfileId, i.Content)))
            {
                var (profileId, content) = group.Key;
                if (_contents[content].AuthorId == profileId)
                {
                    continue;
                }

                var trust = TrustOf(profileId);
                if (trust <= 0)
                {
                    continue;
                }

                var latest = group.Max(i => i.At);
                var ageDays = Math.Max(0, (Input.Now - latest).TotalDays);
                var decay = Math.Pow(0.5, ageDays / Settings.PopularityHalfLifeDays);
                _popularity[content] = _popularity.GetValueOrDefault(content) + trust * decay;

                if (group.Any(i => i.Kind == InteractionKind.Involve))
                {
                    _support[content] = _support.GetValueOrDefault(content) + trust;
                }
            }
        }

        /// <summary>
        /// 品質：讀完率（章節層級）與追蹤轉換率，用全站平均做平滑，避免讀者很少的作品因為一兩個人而極端
        /// </summary>
        private void BuildQuality()
        {
            var started = new Dictionary<ContentKey, double>();
            var ended = new Dictionary<ContentKey, double>();
            foreach (var reading in Input.Readings)
            {
                var key = ContentKey.Novel(reading.NovelId);
                if (!_contents.TryGetValue(key, out var item) || item.AuthorId == reading.ProfileId)
                {
                    continue;
                }

                var trust = TrustOf(reading.ProfileId);
                if (trust <= 0)
                {
                    continue;
                }

                started[key] = started.GetValueOrDefault(key) + trust;
                if (reading.ReachedEnd)
                {
                    ended[key] = ended.GetValueOrDefault(key) + trust;
                }
            }

            var readers = new Dictionary<ContentKey, double>();
            var followers = new Dictionary<ContentKey, double>();
            foreach (var group in Input.Interactions
                .Where(i => _contents.ContainsKey(i.Content))
                .GroupBy(i => (i.ProfileId, i.Content)))
            {
                var (profileId, content) = group.Key;
                var trust = TrustOf(profileId);
                if (trust <= 0 || _contents[content].AuthorId == profileId)
                {
                    continue;
                }

                readers[content] = readers.GetValueOrDefault(content) + trust;
                if (group.Any(i => i.Kind == InteractionKind.Follow))
                {
                    followers[content] = followers.GetValueOrDefault(content) + trust;
                }
            }

            var totalStarted = started.Values.Sum();
            var completionMean = totalStarted > 0 ? ended.Values.Sum() / totalStarted : 0.5;
            var totalReaders = readers.Values.Sum();
            var conversionMean = totalReaders > 0 ? followers.Values.Sum() / totalReaders : 0;
            var prior = Settings.QualityPriorCount;

            var completion = new Dictionary<ContentKey, double>();
            var conversion = new Dictionary<ContentKey, double>();
            foreach (var key in _contents.Keys)
            {
                var s = started.GetValueOrDefault(key);
                var r = readers.GetValueOrDefault(key);
                completion[key] = s + prior > 0 ? (ended.GetValueOrDefault(key) + prior * completionMean) / (s + prior) : completionMean;
                conversion[key] = r + prior > 0 ? (followers.GetValueOrDefault(key) + prior * conversionMean) / (r + prior) : conversionMean;
            }

            var maxCompletion = completion.Count == 0 ? 0 : completion.Values.Max();
            var maxConversion = conversion.Count == 0 ? 0 : conversion.Values.Max();
            foreach (var key in _contents.Keys)
            {
                var c = maxCompletion > 0 ? completion[key] / maxCompletion : 0;
                var f = maxConversion > 0 ? conversion[key] / maxConversion : 0;
                _quality[key] = (c + f) / 2;
            }
        }

        /// <summary>
        /// 內容向量：標籤與主分類，以 IDF 加權（冷門標籤權重較高），再正規化成單位向量
        /// </summary>
        private void BuildFeatures()
        {
            var documentFrequency = new Dictionary<int, int>();
            foreach (var item in _contents.Values)
            {
                foreach (var feature in RawFeatures(item))
                {
                    documentFrequency[feature] = documentFrequency.GetValueOrDefault(feature) + 1;
                }
            }

            var total = _contents.Count;
            foreach (var (feature, df) in documentFrequency)
            {
                var idf = Math.Log((total + 1.0) / (df + 1.0)) + 1;
                _idf[feature] = IsCategory(feature) ? idf * Settings.CategoryFeatureWeight : idf;
            }

            foreach (var item in _contents.Values.OrderBy(c => c.Key))
            {
                var vector = RawFeatures(item).ToDictionary(f => f, f => _idf[f]);
                _itemVectors[item.Key] = Normalize(vector);

                foreach (var feature in vector.Keys)
                {
                    if (!_featureIndex.TryGetValue(feature, out var list))
                    {
                        list = [];
                        _featureIndex[feature] = list;
                    }

                    list.Add(item.Key);
                }
            }

            foreach (var state in _users.Values)
            {
                var vector = new Dictionary<int, double>();
                foreach (var (content, weight) in state.Engagement)
                {
                    foreach (var feature in RawFeatures(_contents[content]))
                    {
                        vector[feature] = vector.GetValueOrDefault(feature) + weight * _idf[feature];
                    }
                }

                // 自選的分類：還沒有作品使用的分類沒有 IDF，以分類權重代替
                foreach (var feature in state.PreferredCategories)
                {
                    var idf = _idf.TryGetValue(feature, out var value) ? value : Settings.CategoryFeatureWeight;
                    vector[feature] = vector.GetValueOrDefault(feature) + Settings.PreferredCategoryWeight * idf;
                }

                state.Preference = Normalize(vector);
            }
        }

        /// <summary>
        /// 共同觀看：兩部作品的讀者集合的 cosine 相似度。只算可信度大於 0、不是作者本人的讀者
        /// </summary>
        private void BuildCoViews()
        {
            var readers = new Dictionary<ContentKey, int>();
            var pairs = new Dictionary<(ContentKey, ContentKey), int>();

            foreach (var state in _users.Values)
            {
                if (TrustOf(state.ProfileId) <= 0)
                {
                    continue;
                }

                var items = state.LastEngagedAt
                    .OrderByDescending(e => e.Value)
                    .ThenBy(e => e.Key)
                    .Take(MaxItemsPerUserForCoView)
                    .Select(e => e.Key)
                    .OrderBy(k => k)
                    .ToList();

                foreach (var item in items)
                {
                    readers[item] = readers.GetValueOrDefault(item) + 1;
                }

                for (var i = 0; i < items.Count; i++)
                {
                    for (var j = i + 1; j < items.Count; j++)
                    {
                        var pair = (items[i], items[j]);
                        pairs[pair] = pairs.GetValueOrDefault(pair) + 1;
                    }
                }
            }

            foreach (var ((a, b), count) in pairs)
            {
                if (count < Settings.MinCoViewers)
                {
                    continue;
                }

                var similarity = count / Math.Sqrt((double)readers[a] * readers[b]);
                AddNeighbor(a, b, similarity);
                AddNeighbor(b, a, similarity);
            }

            foreach (var list in _neighbors.Values)
            {
                list.Sort((x, y) =>
                {
                    var bySimilarity = y.Similarity.CompareTo(x.Similarity);
                    return bySimilarity != 0 ? bySimilarity : x.Other.CompareTo(y.Other);
                });
            }
        }

        private void AddNeighbor(ContentKey from, ContentKey to, double similarity)
        {
            if (!_neighbors.TryGetValue(from, out var list))
            {
                list = [];
                _neighbors[from] = list;
            }

            list.Add((to, similarity));
        }

        private static Dictionary<int, double> Normalize(Dictionary<int, double> vector)
        {
            var norm = Math.Sqrt(vector.Values.Sum(v => v * v));
            return norm <= 0
                ? new Dictionary<int, double>()
                : vector.ToDictionary(e => e.Key, e => e.Value / norm);
        }

        /// <summary>
        /// 一位會員的互動與偏好
        /// </summary>
        /// <param name="profileId">會員</param>
        public sealed class UserState(string profileId)
        {
            /// <summary>
            /// 會員
            /// </summary>
            public string ProfileId { get; } = profileId;

            /// <summary>
            /// 看過、追蹤或 Involve 過的內容（含自己的作品）；這些不會再被推薦
            /// </summary>
            public HashSet<ContentKey> Seen { get; } = [];

            /// <summary>
            /// 對別人作品的互動強度（看過 + 追蹤 + Involve 的權重加總）
            /// </summary>
            public Dictionary<ContentKey, double> Engagement { get; } = new();

            /// <summary>
            /// 對別人作品最後一次互動的時間
            /// </summary>
            public Dictionary<ContentKey, DateTime> LastEngagedAt { get; } = new();

            /// <summary>
            /// 追蹤中的作者
            /// </summary>
            public HashSet<string> FollowedAuthors { get; } = new(StringComparer.Ordinal);

            /// <summary>
            /// 按過「不感興趣」的內容
            /// </summary>
            public HashSet<ContentKey> NotInterested { get; } = [];

            /// <summary>
            /// 按過「不感興趣」的內容的作者
            /// </summary>
            public HashSet<string> DislikedAuthors { get; } = new(StringComparer.Ordinal);

            /// <summary>
            /// 自選的分類（標籤 ID）
            /// </summary>
            public HashSet<int> PreferredCategories { get; } = [];

            /// <summary>
            /// 偏好向量（單位向量）
            /// </summary>
            public IReadOnlyDictionary<int, double> Preference { get; internal set; } = new Dictionary<int, double>();

            /// <summary>
            /// 是否有任何可以用來推薦的訊號
            /// </summary>
            public bool HasSignal => Engagement.Count > 0 || PreferredCategories.Count > 0 || FollowedAuthors.Count > 0;
        }
    }
}
