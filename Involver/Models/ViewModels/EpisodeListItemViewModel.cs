namespace Involver.Models.ViewModels;

/// <summary>
/// 章節列表的一列：只帶列表顯示需要的欄位，不載入章節全文
/// </summary>
public class EpisodeListItemViewModel
{
    /// <summary>
    /// 章節 ID
    /// </summary>
    public int EpisodeID { get; set; }

    /// <summary>
    /// 章節標題
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 更新時間
    /// </summary>
    public DateTime UpdateTime { get; set; }

    /// <summary>
    /// 觀看數
    /// </summary>
    public int Views { get; set; }
}
