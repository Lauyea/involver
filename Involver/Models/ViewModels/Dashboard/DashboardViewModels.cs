using System.Text.Json.Serialization;

namespace Involver.Models.ViewModels.Dashboard;

/// <summary>
/// KPI 卡片（管理員後台與創作者中心共用）
/// </summary>
public class KpiCardViewModel
{
    /// <summary>
    /// 指標名稱
    /// </summary>
    public string Label { get; init; }

    /// <summary>
    /// 目前的值
    /// </summary>
    public decimal Value { get; init; }

    /// <summary>
    /// 數值格式（.NET 格式字串），預設千分位整數
    /// </summary>
    public string Format { get; init; } = "N0";

    /// <summary>
    /// 數值後綴，例如「%」、「In」
    /// </summary>
    public string Suffix { get; init; }

    /// <summary>
    /// 和上一期相比的變化百分比；無法比較時為 null
    /// </summary>
    public decimal? ChangePercent { get; init; }

    /// <summary>
    /// 比較的說明，例如「與 30 天前相比」
    /// </summary>
    public string ComparisonLabel { get; init; }

    /// <summary>
    /// 補充說明（顯示在數值下方）
    /// </summary>
    public string Hint { get; init; }

    /// <summary>
    /// Font Awesome 圖示 class，例如 fa-users
    /// </summary>
    public string Icon { get; init; }

    /// <summary>
    /// 點擊卡片前往的網址
    /// </summary>
    public string Url { get; init; }

    /// <summary>
    /// 是否為核心指標（數字較大）
    /// </summary>
    public bool IsCore { get; init; }

    /// <summary>
    /// 目標值進度；沒有設定目標時為 null
    /// </summary>
    public KpiTargetProgressViewModel Target { get; init; }
}

/// <summary>
/// KPI 目標值進度
/// </summary>
public class KpiTargetProgressViewModel
{
    /// <summary>
    /// 期間名稱，例如「2026 Q4」
    /// </summary>
    public string PeriodLabel { get; init; }

    /// <summary>
    /// 目標值
    /// </summary>
    public decimal TargetValue { get; init; }

    /// <summary>
    /// 達成百分比（可超過 100）
    /// </summary>
    public decimal Percent { get; init; }
}

/// <summary>
/// 圖表區塊（管理員後台與創作者中心共用）；<see cref="Config"/> 會序列化成 data-chart-config 給 chart-panels.js
/// </summary>
public class ChartPanelViewModel
{
    /// <summary>
    /// 區塊標題
    /// </summary>
    public string Title { get; init; }

    /// <summary>
    /// 副標題
    /// </summary>
    public string Subtitle { get; init; }

    /// <summary>
    /// 較矮的圖表
    /// </summary>
    public bool Small { get; init; }

    /// <summary>
    /// 沒有資料時顯示的文字
    /// </summary>
    public string EmptyText { get; init; } = "尚無資料";

    /// <summary>
    /// 圖表設定
    /// </summary>
    public ChartConfigViewModel Config { get; init; } = new();

    /// <summary>
    /// 是否有任何資料
    /// </summary>
    public bool HasData => Config.Labels.Count > 0 && Config.Datasets.Any(d => d.Data.Count > 0);
}

/// <summary>
/// 圖表設定（序列化給前端）
/// </summary>
public class ChartConfigViewModel
{
    /// <summary>
    /// 圖表類型：bar、line、doughnut
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = "line";

    /// <summary>
    /// X 軸標籤
    /// </summary>
    [JsonPropertyName("labels")]
    public List<string> Labels { get; init; } = [];

    /// <summary>
    /// 資料集
    /// </summary>
    [JsonPropertyName("datasets")]
    public List<ChartDatasetViewModel> Datasets { get; init; } = [];

    /// <summary>
    /// 是否堆疊
    /// </summary>
    [JsonPropertyName("stacked")]
    public bool Stacked { get; init; }

    /// <summary>
    /// 是否為橫向長條圖
    /// </summary>
    [JsonPropertyName("horizontal")]
    public bool Horizontal { get; init; }

    /// <summary>
    /// 左側 Y 軸標題
    /// </summary>
    [JsonPropertyName("yTitle")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string YTitle { get; init; }

    /// <summary>
    /// 右側 Y 軸標題
    /// </summary>
    [JsonPropertyName("y1Title")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Y1Title { get; init; }
}

/// <summary>
/// 圖表的一組資料
/// </summary>
public class ChartDatasetViewModel
{
    /// <summary>
    /// 名稱
    /// </summary>
    [JsonPropertyName("label")]
    public string Label { get; init; }

    /// <summary>
    /// 數值
    /// </summary>
    [JsonPropertyName("data")]
    public List<decimal> Data { get; init; } = [];

    /// <summary>
    /// 調色盤顏色（對應 --chart-color-N，從 0 開始）
    /// </summary>
    [JsonPropertyName("colorIndex")]
    public int ColorIndex { get; init; }

    /// <summary>
    /// 混合圖表時個別指定類型（bar、line）
    /// </summary>
    [JsonPropertyName("type")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string Type { get; init; }

    /// <summary>
    /// 使用的 Y 軸：y（左）或 y1（右）
    /// </summary>
    [JsonPropertyName("yAxisId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string YAxisId { get; init; }

    /// <summary>
    /// 折線圖是否填色
    /// </summary>
    [JsonPropertyName("fill")]
    public bool Fill { get; init; }
}
