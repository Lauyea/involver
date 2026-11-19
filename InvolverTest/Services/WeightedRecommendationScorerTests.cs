using System;
using System.Collections.Generic;
using System.Linq;

using DataAccess.Services.Recommendations;

using NUnit.Framework;


namespace InvolverTest.Services
{
    /// <summary>
    /// Issue #238：推薦評分（純函式，不需要資料庫）
    /// </summary>
    [TestFixture]
    public class WeightedRecommendationScorerTests
    {
        private static readonly DateTime Now = new(2026, 11, 19, 12, 0, 0);

        private const string Reader = "reader";
        private const string AuthorA = "authorA";
        private const string AuthorB = "authorB";
        private const string AuthorC = "authorC";

        private const int FantasyTag = 1;
        private const int MysteryTag = 2;
        private const int RomanceTag = 3;
        private const int FantasyCategory = 11;
        private const int DetectiveCategory = 12;
        private const int LoveCategory = 13;

        private readonly WeightedRecommendationScorer _scorer = new();

        [Test]
        public void Recommend_WithSameInput_ReturnsSameOrder()
        {
            var input = BuildInput();
            var settings = new RecommendationSettings();

            var first = _scorer.Recommend(new RecommendationCorpus(input, settings), Reader);
            var second = _scorer.Recommend(new RecommendationCorpus(input, settings), Reader);

            Assert.That(first, Is.Not.Empty);
            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Recommend_WhenInputOrderIsShuffled_ReturnsSameOrder()
        {
            var input = BuildInput();
            var shuffled = new RecommendationInput
            {
                Now = input.Now,
                Contents = input.Contents.Reverse().ToList(),
                TagNames = input.TagNames,
                CategoryTagIds = input.CategoryTagIds,
                Accounts = input.Accounts.Reverse().ToList(),
                Interactions = input.Interactions.Reverse().ToList(),
                Readings = input.Readings.Reverse().ToList(),
                AuthorFollows = input.AuthorFollows.Reverse().ToList(),
                NotInterested = input.NotInterested,
                PreferredCategories = input.PreferredCategories,
            };
            var settings = new RecommendationSettings();

            var original = _scorer.Recommend(new RecommendationCorpus(input, settings), Reader);
            var reordered = _scorer.Recommend(new RecommendationCorpus(shuffled, settings), Reader);

            Assert.That(reordered, Is.EqualTo(original));
        }

        [Test]
        public void Recommend_WhenContentWasViewedOrFollowed_ExcludesIt()
        {
            var input = BuildInput();
            var result = Keys(_scorer.Recommend(new RecommendationCorpus(input, new RecommendationSettings()), Reader));

            // 看過的 1、追蹤的 2 不會出現
            Assert.That(result, Does.Not.Contain(ContentKey.Novel(1)));
            Assert.That(result, Does.Not.Contain(ContentKey.Novel(2)));
        }

        [Test]
        public void Recommend_WhenMarkedNotInterested_ExcludesIt()
        {
            var baseline = BuildInput();
            var before = Keys(_scorer.Recommend(new RecommendationCorpus(baseline, new RecommendationSettings()), Reader));
            Assert.That(before, Does.Contain(ContentKey.Novel(3)));

            var input = BuildInput(notInterested: [(Reader, ContentKey.Novel(3))]);
            var after = Keys(_scorer.Recommend(new RecommendationCorpus(input, new RecommendationSettings()), Reader));

            Assert.That(after, Does.Not.Contain(ContentKey.Novel(3)));
        }

        [Test]
        public void Recommend_ForAuthor_ExcludesOwnWorks()
        {
            var input = BuildInput();
            var result = _scorer.Recommend(new RecommendationCorpus(input, new RecommendationSettings()), AuthorB);

            Assert.That(result.Select(r => input.Contents.Single(c => c.Key == r.Content).AuthorId), Does.Not.Contain(AuthorB));
        }

        [Test]
        public void Recommend_EveryResult_HasReason()
        {
            var input = BuildInput();
            var result = _scorer.Recommend(new RecommendationCorpus(input, new RecommendationSettings()), Reader);

            Assert.That(result, Is.Not.Empty);
            Assert.That(result.Select(r => r.Reason), Has.All.Not.Empty);
        }

        [Test]
        public void Recommend_WhenCoViewed_ExplainsWithSourceTitle()
        {
            var input = BuildInput();
            var settings = new RecommendationSettings
            {
                ContentWeight = 0,
                PopularityWeight = 0,
                SupportWeight = 0,
                QualityWeight = 0,
                FreshnessWeight = 0,
                FollowedAuthorWeight = 0,
                ExplorationSlots = 0,
            };

            var result = _scorer.Recommend(new RecommendationCorpus(input, settings), Reader);
            var coViewed = result.Single(r => r.Content == ContentKey.Novel(3));

            Assert.That(coViewed.Reason, Is.EqualTo("因為你讀過《奇幻一》"));
        }

        [Test]
        public void Recommend_WithSameAuthorLimit_RespectsMaxPerAuthor()
        {
            var input = BuildInput();
            var settings = new RecommendationSettings { MaxPerAuthor = 1, ExplorationSlots = 0 };

            var result = _scorer.Recommend(new RecommendationCorpus(input, settings), Reader);
            var authors = result.Select(r => input.Contents.Single(c => c.Key == r.Content).AuthorId).ToList();

            Assert.That(authors, Is.Unique);
        }

        [Test]
        public void Recommend_WithExplorationSlot_IncludesNewWork()
        {
            var input = BuildInput();
            var settings = new RecommendationSettings { ExplorationSlots = 1, ResultCount = 2, ContentWeight = 10 };

            var result = _scorer.Recommend(new RecommendationCorpus(input, settings), Reader);

            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result.Select(r => r.Reason), Does.Contain(WeightedRecommendationScorer.ExplorationReason));
        }

        [Test]
        public void Recommend_WithoutAnySignal_ReturnsEmpty()
        {
            var input = BuildInput();

            var result = _scorer.Recommend(new RecommendationCorpus(input, new RecommendationSettings()), "stranger");

            Assert.That(result, Is.Empty);
        }

        [Test]
        public void Recommend_WithPreferredCategoryOnly_RecommendsThatCategory()
        {
            var input = BuildInput(preferredCategories: [("newbie", DetectiveCategory)]);
            var settings = new RecommendationSettings { ExplorationSlots = 0 };

            var result = _scorer.Recommend(new RecommendationCorpus(input, settings), "newbie");

            Assert.That(result.First().Content, Is.EqualTo(ContentKey.Novel(4)));
            Assert.That(result.First().Reason, Is.EqualTo("你喜歡推理類作品"));
        }

        [Test]
        public void Recommend_FromSharedTag_UsesTagReason()
        {
            // 只有一般標籤相同（文章沒有分類）：理由是「你常看 #標籤」，不是分類
            var input = BuildInput(extraInteractions: [new Interaction("newbie", ContentKey.Article(1), InteractionKind.Follow, Now)]);
            var settings = new RecommendationSettings { ExplorationSlots = 0, CollaborativeWeight = 0 };

            var result = _scorer.Recommend(new RecommendationCorpus(input, settings), "newbie");

            Assert.That(result.First().Reason, Is.EqualTo("你常看 #魔法"));
        }

        [Test]
        public void Corpus_CategoryFeatureWeight_OnlyScalesCategoryTags()
        {
            var input = BuildInput();
            var baseline = new RecommendationCorpus(input, new RecommendationSettings());
            var weighted = new RecommendationCorpus(input, new RecommendationSettings { CategoryFeatureWeight = 3 });

            // 單位向量：分類權重變大，分類的比重上升、一般標籤的比重下降
            Assert.That(weighted.ItemVector(ContentKey.Novel(4))[DetectiveCategory], Is.GreaterThan(baseline.ItemVector(ContentKey.Novel(4))[DetectiveCategory]));
            Assert.That(weighted.ItemVector(ContentKey.Novel(4))[MysteryTag], Is.LessThan(baseline.ItemVector(ContentKey.Novel(4))[MysteryTag]));
            Assert.That(weighted.ItemVector(ContentKey.Article(1))[FantasyTag], Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void Corpus_AuthorsOwnInteractions_AreNotCounted()
        {
            // 作者自己看、自己 Involve 自己的作品 10 次
            var selfPromotion = Enumerable.Range(0, 10)
                .SelectMany(i => new[]
                {
                    new Interaction(AuthorC, ContentKey.Novel(5), InteractionKind.View, Now.AddHours(-i)),
                    new Interaction(AuthorC, ContentKey.Novel(5), InteractionKind.Involve, Now.AddHours(-i)),
                })
                .ToList();
            var input = BuildInput(extraInteractions: selfPromotion);

            var corpus = new RecommendationCorpus(input, new RecommendationSettings());

            Assert.That(corpus.PopularityOf(ContentKey.Novel(5)), Is.EqualTo(0));
            Assert.That(corpus.SupportOf(ContentKey.Novel(5)), Is.EqualTo(0));
        }

        [Test]
        public void Corpus_RepeatedViewsBySameUser_CountOnce()
        {
            var repeated = Enumerable.Range(0, 50)
                .Select(i => new Interaction("fan", ContentKey.Novel(5), InteractionKind.View, Now))
                .ToList();
            var input = BuildInput(extraInteractions: repeated, extraAccounts: [new ReaderAccount("fan", Now.AddYears(-1), false, true)]);

            var corpus = new RecommendationCorpus(input, new RecommendationSettings());

            // 可信度上限為 1：註冊滿 30 天 + 付費過 = 0.5 + 0.25 + 0.25
            Assert.That(corpus.PopularityOf(ContentKey.Novel(5)), Is.EqualTo(1).Within(1e-9));
        }

        [Test]
        public void Corpus_BannedUsers_AreNotCounted()
        {
            var input = BuildInput(
                extraInteractions: [new Interaction("banned", ContentKey.Novel(5), InteractionKind.Involve, Now)],
                extraAccounts: [new ReaderAccount("banned", Now.AddYears(-1), true, true)]);

            var corpus = new RecommendationCorpus(input, new RecommendationSettings());

            Assert.That(corpus.PopularityOf(ContentKey.Novel(5)), Is.EqualTo(0));
            Assert.That(corpus.SupportOf(ContentKey.Novel(5)), Is.EqualTo(0));
        }

        [Test]
        public void TrustOf_UnpaidAccountPastMinAge_ReturnsBaseTrust()
        {
            var settings = new RecommendationSettings();

            var trust = RecommendationCorpus.TrustOf(new ReaderAccount("new", Now.AddDays(-(settings.TrustMinAccountAgeDays + 1)), false, false), Now, settings);

            Assert.That(trust, Is.EqualTo(settings.TrustBase));
        }

        [Test]
        public void TrustOf_AccountYoungerThanMinAge_ReturnsZero()
        {
            var settings = new RecommendationSettings();

            var trust = RecommendationCorpus.TrustOf(new ReaderAccount("new", Now.AddDays(-1), false, true), Now, settings);

            Assert.That(trust, Is.Zero);
        }

        [Test]
        public void TrustOf_MinAgeZero_CountsBrandNewAccount()
        {
            var settings = new RecommendationSettings { TrustMinAccountAgeDays = 0 };

            var trust = RecommendationCorpus.TrustOf(new ReaderAccount("new", Now, false, false), Now, settings);

            Assert.That(trust, Is.EqualTo(settings.TrustBase));
        }

        [Test]
        public void Recommend_NewAccountRing_DoesNotBoostTarget_ButNewAccountStillGetsRecommendations()
        {
            // 25 個剛註冊的帳號集中看、追蹤同一部作品
            var ring = Enumerable.Range(1, 25).Select(i => $"ring{i}").ToList();
            var baseline = new RecommendationCorpus(BuildInput(), new RecommendationSettings());
            var input = BuildInput(
                extraInteractions: ring.SelectMany(id => new[]
                {
                    new Interaction(id, ContentKey.Novel(5), InteractionKind.View, Now),
                    new Interaction(id, ContentKey.Novel(5), InteractionKind.Follow, Now),
                }).ToList(),
                extraAccounts: ring.Select(id => new ReaderAccount(id, Now.AddDays(-1), false, false)).ToList());

            var corpus = new RecommendationCorpus(input, new RecommendationSettings());

            Assert.That(corpus.PopularityOf(ContentKey.Novel(5)), Is.EqualTo(baseline.PopularityOf(ContentKey.Novel(5))));
            Assert.That(corpus.QualityOf(ContentKey.Novel(5)), Is.EqualTo(baseline.QualityOf(ContentKey.Novel(5))));
            Assert.That(new WeightedRecommendationScorer().Recommend(corpus, "ring1"), Is.Not.Empty);
        }

        [Test]
        public void Settings_FromJsonWithMissingFields_UsesDefaults()
        {
            var settings = RecommendationSettings.FromJson("{\"popularityWeight\":0.9}");

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings!.PopularityWeight, Is.EqualTo(0.9));
            Assert.That(settings.ContentWeight, Is.EqualTo(new RecommendationSettings().ContentWeight));
        }

        [Test]
        public void Settings_FromJsonOutOfRange_ReturnsNull()
        {
            Assert.That(RecommendationSettings.FromJson("{\"maxPerAuthor\":0}"), Is.Null);
            Assert.That(RecommendationSettings.FromJson("not json"), Is.Null);
        }

        private static List<ContentKey> Keys(IEnumerable<ScoredRecommendation> results) => results.Select(r => r.Content).ToList();

        /// <summary>
        /// 小說 1～5、文章 1：
        /// reader 看過 1（奇幻）、追蹤 2（奇幻、懸疑）；fan1、fan2 看過 1 也看過 3（共同觀看）；
        /// 4 是推理新作；5 是 authorC 的作品；文章 1 有奇幻標籤。
        /// </summary>
        private static RecommendationInput BuildInput(
            IReadOnlyList<(string, ContentKey)>? notInterested = null,
            IReadOnlyList<(string, int)>? preferredCategories = null,
            IReadOnlyList<Interaction>? extraInteractions = null,
            IReadOnlyList<ReaderAccount>? extraAccounts = null)
        {
            var contents = new List<ContentItem>
            {
                new(ContentKey.Novel(1), "奇幻一", AuthorA, AuthorA, [FantasyCategory, FantasyTag], Now.AddDays(-100), Now.AddDays(-2)),
                new(ContentKey.Novel(2), "奇幻二", AuthorA, AuthorA, [FantasyCategory, FantasyTag, MysteryTag], Now.AddDays(-90), Now.AddDays(-3)),
                new(ContentKey.Novel(3), "奇幻三", AuthorB, AuthorB, [FantasyCategory, FantasyTag], Now.AddDays(-80), Now.AddDays(-5)),
                new(ContentKey.Novel(4), "推理新作", AuthorB, AuthorB, [DetectiveCategory, MysteryTag], Now.AddDays(-2), Now.AddDays(-1)),
                new(ContentKey.Novel(5), "愛情", AuthorC, AuthorC, [LoveCategory, RomanceTag], Now.AddDays(-60), Now.AddDays(-30)),
                new(ContentKey.Article(1), "奇幻設定集", AuthorC, AuthorC, [FantasyTag], Now.AddDays(-40), Now.AddDays(-40)),
            };

            var accounts = new List<ReaderAccount>
            {
                new(Reader, Now.AddYears(-1), false, false),
                new("fan1", Now.AddYears(-1), false, true),
                new("fan2", Now.AddYears(-1), false, false),
                new(AuthorA, Now.AddYears(-2), false, false),
                new(AuthorB, Now.AddYears(-2), false, false),
                new(AuthorC, Now.AddYears(-2), false, false),
                new("newbie", Now.AddDays(-1), false, false),
            };
            accounts.AddRange(extraAccounts ?? []);

            var interactions = new List<Interaction>
            {
                new(Reader, ContentKey.Novel(1), InteractionKind.View, Now.AddDays(-1)),
                new(Reader, ContentKey.Novel(2), InteractionKind.Follow, Now.AddDays(-1)),
                new("fan1", ContentKey.Novel(1), InteractionKind.View, Now.AddDays(-2)),
                new("fan1", ContentKey.Novel(3), InteractionKind.View, Now.AddDays(-2)),
                new("fan1", ContentKey.Novel(3), InteractionKind.Involve, Now.AddDays(-2)),
                new("fan2", ContentKey.Novel(1), InteractionKind.View, Now.AddDays(-3)),
                new("fan2", ContentKey.Novel(3), InteractionKind.View, Now.AddDays(-3)),
                new("fan2", ContentKey.Article(1), InteractionKind.View, Now.AddDays(-3)),
                new(AuthorB, ContentKey.Novel(1), InteractionKind.View, Now.AddDays(-4)),
            };
            interactions.AddRange(extraInteractions ?? []);

            return new RecommendationInput
            {
                Now = Now,
                Contents = contents,
                TagNames = new Dictionary<int, string>
                {
                    [FantasyTag] = "魔法",
                    [MysteryTag] = "懸疑",
                    [RomanceTag] = "戀愛",
                    [FantasyCategory] = "奇幻",
                    [DetectiveCategory] = "推理",
                    [LoveCategory] = "愛情",
                },
                CategoryTagIds = new HashSet<int> { FantasyCategory, DetectiveCategory, LoveCategory },
                Accounts = accounts,
                Interactions = interactions,
                Readings =
                [
                    new ReadingRecord("fan1", 3, true),
                    new ReadingRecord("fan2", 3, false),
                ],
                AuthorFollows = [],
                NotInterested = notInterested ?? [],
                PreferredCategories = preferredCategories ?? [],
            };
        }
    }
}
