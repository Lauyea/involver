namespace DataAccess.Services.Recommendations
{
    /// <summary>
    /// 推薦評分。輸入一位會員和整理好的訊號，回傳排好序、附推薦理由的結果。
    /// <para>
    /// 實作必須是純函式：相同的輸入一定得到相同的排序；不能回傳已看過、已追蹤、不感興趣或自己的作品。
    /// 公開 repo 只放機制與預設實作，正式權重存在資料庫；若要隱藏正式實作，可以在私有專案實作這個介面，
    /// 另外寫入 <c>UserRecommendations</c>，網站只讀結果。
    /// </para>
    /// </summary>
    public interface IRecommendationScorer
    {
        /// <summary>
        /// 為一位會員產生推薦
        /// </summary>
        /// <param name="corpus">整理好的訊號（含設定）</param>
        /// <param name="profileId">會員</param>
        /// <returns>依排名排序的推薦；沒有任何訊號的會員回傳空集合</returns>
        IReadOnlyList<ScoredRecommendation> Recommend(RecommendationCorpus corpus, string profileId);
    }
}
