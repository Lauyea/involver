using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace DataAccess.Services.Recommendations
{
    /// <summary>
    /// 推薦演算法的權重與門檻。
    /// 這裡只放公開的預設值，資料庫沒有啟用的設定時使用；正式數值透過管理員後台存進 <c>RecommendationSettings</c> 表。
    /// 新增屬性時一定要給預設值：舊版本的 JSON 沒有這個欄位，反序列化後會使用預設值。
    /// </summary>
    public sealed class RecommendationSettings
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
        };

        // ── 評分權重 ──────────────────────────────

        /// <summary>
        /// w1：內容相似度（使用者偏好的標籤、分類 vs 作品，TF-IDF 加權）。
        /// 最主要的個人化訊號，預設權重最高，讓剛選完分類的新會員也能看到對應的作品
        /// </summary>
        [Display(Name = "內容相似度", GroupName = "評分權重")]
        [Range(0, 100)]
        public double ContentWeight { get; set; } = 2.0;

        /// <summary>
        /// w2：協同訊號（看過 X 的人也看過 Y）
        /// </summary>
        [Display(Name = "協同訊號（共同觀看）", GroupName = "評分權重")]
        [Range(0, 100)]
        public double CollaborativeWeight { get; set; } = 1.0;

        /// <summary>
        /// w3：熱度（不重複讀者數取 log，隨時間衰減）
        /// </summary>
        [Display(Name = "熱度", GroupName = "評分權重")]
        [Range(0, 100)]
        public double PopularityWeight { get; set; } = 0.5;

        /// <summary>
        /// w4：支持強度（不同 Involve 人數取 log）
        /// </summary>
        [Display(Name = "支持強度（Involve）", GroupName = "評分權重")]
        [Range(0, 100)]
        public double SupportWeight { get; set; } = 0.5;

        /// <summary>
        /// w5：品質（讀完率、追蹤轉換率）
        /// </summary>
        [Display(Name = "品質（讀完率、追蹤轉換率）", GroupName = "評分權重")]
        [Range(0, 100)]
        public double QualityWeight { get; set; } = 0.3;

        /// <summary>
        /// 新鮮度加成
        /// </summary>
        [Display(Name = "新鮮度加成", GroupName = "評分權重")]
        [Range(0, 100)]
        public double FreshnessWeight { get; set; } = 0.3;

        /// <summary>
        /// 追蹤中的作者的作品加成
        /// </summary>
        [Display(Name = "追蹤作者加成", GroupName = "評分權重")]
        [Range(0, 100)]
        public double FollowedAuthorWeight { get; set; } = 0.8;

        /// <summary>
        /// 對某作者的作品按過「不感興趣」時，同作者其他作品的扣分
        /// </summary>
        [Display(Name = "不感興趣作者扣分", GroupName = "評分權重")]
        [Range(0, 100)]
        public double DislikedAuthorPenalty { get; set; } = 1.0;

        // ── 使用者偏好 ──────────────────────────────

        /// <summary>
        /// 看過一部作品對偏好的貢獻
        /// </summary>
        [Display(Name = "看過的權重", GroupName = "使用者偏好")]
        [Range(0, 100)]
        public double ViewSignalWeight { get; set; } = 1.0;

        /// <summary>
        /// 追蹤一部作品對偏好的貢獻
        /// </summary>
        [Display(Name = "追蹤的權重", GroupName = "使用者偏好")]
        [Range(0, 100)]
        public double FollowSignalWeight { get; set; } = 2.0;

        /// <summary>
        /// Involve 一部作品對偏好的貢獻（付費支持，最難造假）
        /// </summary>
        [Display(Name = "Involve 的權重", GroupName = "使用者偏好")]
        [Range(0, 100)]
        public double InvolveSignalWeight { get; set; } = 3.0;

        /// <summary>
        /// 冷啟動時選擇的分類對偏好的貢獻
        /// </summary>
        [Display(Name = "自選分類的權重", GroupName = "使用者偏好")]
        [Range(0, 100)]
        public double PreferredCategoryWeight { get; set; } = 2.0;

        /// <summary>
        /// 分類標籤在內容向量中的權重倍數（相對於一般標籤）
        /// </summary>
        [Display(Name = "分類標籤 vs 一般標籤", GroupName = "使用者偏好")]
        [Range(0, 100)]
        public double CategoryFeatureWeight { get; set; } = 1.0;

        // ── 時間與門檻 ──────────────────────────────

        /// <summary>
        /// 熱度的半衰期（天）
        /// </summary>
        [Display(Name = "熱度半衰期（天）", GroupName = "時間與門檻")]
        [Range(1, 365)]
        public double PopularityHalfLifeDays { get; set; } = 14;

        /// <summary>
        /// 新鮮度的半衰期（天，依作品更新時間）
        /// </summary>
        [Display(Name = "新鮮度半衰期（天）", GroupName = "時間與門檻")]
        [Range(1, 365)]
        public double FreshnessHalfLifeDays { get; set; } = 14;

        /// <summary>
        /// 建立幾天內算「新作」（探索名額、追蹤作者的新作）
        /// </summary>
        [Display(Name = "新作天數", GroupName = "時間與門檻")]
        [Range(1, 365)]
        public int NewWorkDays { get; set; } = 30;

        /// <summary>
        /// 共同觀看至少要有幾位不重複讀者才算數
        /// </summary>
        [Display(Name = "共同觀看最少人數", GroupName = "時間與門檻")]
        [Range(1, 1000)]
        public int MinCoViewers { get; set; } = 2;

        /// <summary>
        /// 讀完率、追蹤轉換率的平滑強度（相當於先假設有幾位讀者落在平均值）
        /// </summary>
        [Display(Name = "品質平滑人數", GroupName = "時間與門檻")]
        [Range(0, 1000)]
        public double QualityPriorCount { get; set; } = 10;

        /// <summary>
        /// 只為幾天內登入過的會員計算推薦
        /// </summary>
        [Display(Name = "活躍會員天數", GroupName = "時間與門檻")]
        [Range(1, 3650)]
        public int ActiveUserDays { get; set; } = 60;

        // ── 帳號可信度（防灌） ──────────────────────────────

        /// <summary>
        /// 註冊未滿幾天的帳號，行為不計入熱度、支持強度、品質、共同觀看與熱門排行（可信度為 0）；
        /// 這些帳號自己仍會收到推薦。0 表示不限制。
        /// 熱度依互動時間衰減，剛發生的觀看權重最高，只降低新帳號的可信度擋不住短時間大量開帳號灌票
        /// </summary>
        [Display(Name = "新帳號不計入天數", GroupName = "帳號可信度")]
        [Range(0, 365)]
        public int TrustMinAccountAgeDays { get; set; } = 7;

        /// <summary>
        /// 一般帳號的基本可信度（0～1）。Banned 一律為 0
        /// </summary>
        [Display(Name = "基本可信度", GroupName = "帳號可信度")]
        [Range(0, 1)]
        public double TrustBase { get; set; } = 0.5;

        /// <summary>
        /// 註冊滿幾天加分
        /// </summary>
        [Display(Name = "註冊滿幾天加分", GroupName = "帳號可信度")]
        [Range(0, 3650)]
        public int TrustAccountAgeDays { get; set; } = 30;

        /// <summary>
        /// 註冊滿天數的加分
        /// </summary>
        [Display(Name = "註冊天數加分", GroupName = "帳號可信度")]
        [Range(0, 1)]
        public double TrustAgeBonus { get; set; } = 0.25;

        /// <summary>
        /// 付費過（Prime 或儲值過）的加分
        /// </summary>
        [Display(Name = "付費過加分", GroupName = "帳號可信度")]
        [Range(0, 1)]
        public double TrustPaidBonus { get; set; } = 0.25;

        // ── 結果 ──────────────────────────────

        /// <summary>
        /// 每位會員保留幾筆推薦
        /// </summary>
        [Display(Name = "推薦筆數", GroupName = "結果")]
        [Range(1, 200)]
        public int ResultCount { get; set; } = 24;

        /// <summary>
        /// 多樣性：同一作者最多出現幾次
        /// </summary>
        [Display(Name = "同作者上限", GroupName = "結果")]
        [Range(1, 100)]
        public int MaxPerAuthor { get; set; } = 2;

        /// <summary>
        /// 保留給新作的探索名額
        /// </summary>
        [Display(Name = "新作探索名額", GroupName = "結果")]
        [Range(0, 50)]
        public int ExplorationSlots { get; set; } = 2;

        /// <summary>
        /// 候選：熱門作品取前幾名
        /// </summary>
        [Display(Name = "熱門候選數", GroupName = "結果")]
        [Range(0, 1000)]
        public int PopularCandidates { get; set; } = 50;

        /// <summary>
        /// 候選：新作取前幾名
        /// </summary>
        [Display(Name = "新作候選數", GroupName = "結果")]
        [Range(0, 1000)]
        public int NewCandidates { get; set; } = 30;

        // ── 異常警示 ──────────────────────────────

        /// <summary>
        /// 異常警示：最近幾小時的不重複讀者
        /// </summary>
        [Display(Name = "暴增觀察時數", GroupName = "異常警示")]
        [Range(1, 168)]
        public int SpikeWindowHours { get; set; } = 24;

        /// <summary>
        /// 異常警示：是過去平均的幾倍
        /// </summary>
        [Display(Name = "暴增倍數", GroupName = "異常警示")]
        [Range(1, 1000)]
        public double SpikeMultiplier { get; set; } = 5;

        /// <summary>
        /// 異常警示：最少要有幾位不重複讀者才列出
        /// </summary>
        [Display(Name = "暴增最少人數", GroupName = "異常警示")]
        [Range(1, 100000)]
        public int SpikeMinReaders { get; set; } = 20;

        /// <summary>
        /// 轉成 JSON（存進資料庫）
        /// </summary>
        /// <returns>JSON</returns>
        public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

        /// <summary>
        /// 從 JSON 讀回設定；缺少的欄位使用預設值，格式錯誤時回傳 null
        /// </summary>
        /// <param name="json">JSON</param>
        /// <returns>設定，或 null</returns>
        public static RecommendationSettings? FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                var settings = JsonSerializer.Deserialize<RecommendationSettings>(json, JsonOptions);
                return settings is not null && settings.Validate().Count == 0 ? settings : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// 檢查每個欄位是否在允許的範圍內
        /// </summary>
        /// <returns>錯誤訊息；沒有錯誤時為空</returns>
        public IReadOnlyList<ValidationResult> Validate()
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, new ValidationContext(this), results, validateAllProperties: true);
            return results;
        }

        /// <summary>
        /// 複製一份
        /// </summary>
        /// <returns>新的設定</returns>
        public RecommendationSettings Clone() => FromJson(ToJson()) ?? new RecommendationSettings();
    }
}
