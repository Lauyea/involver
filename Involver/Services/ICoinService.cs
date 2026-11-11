namespace Involver.Services;

/// <summary>
/// In 幣異動服務。
/// 所有金額都以單一 SQL UPDATE 原子更新，不經過 EF 的追蹤實體，避免併發請求互相覆寫餘額。
/// 每次改動使用者餘額，都會在同一個資料庫交易中寫入一筆 <c>CoinTransactions</c> 流水帳；
/// 呼叫端沒有開交易時，服務會自己開一個。
/// 需要多筆異動一起成功或失敗時，由呼叫端以 <c>Database.BeginTransactionAsync</c> 包住。
/// </summary>
public interface ICoinService
{
    /// <summary>
    /// 同一個服務實體（同一個 HTTP 請求）寫入的流水帳共用的 CorrelationId，
    /// 用來串起同一個動作產生的多筆紀錄
    /// </summary>
    Guid CorrelationId { get; }

    /// <summary>
    /// 扣除使用者的 In 幣並累加已使用的 In 幣。餘額不足時不會扣款，也不會寫入流水帳。
    /// 在交易中呼叫時，會鎖住付款人的資料列直到交易結束，同一使用者的併發扣款會依序執行。
    /// </summary>
    /// <param name="profileId">付款人的 Profile ID</param>
    /// <param name="amount">扣款金額，必須大於 0</param>
    /// <param name="info">交易說明</param>
    /// <param name="isVirtual">是否扣虛擬 In 幣，預設扣實體 In 幣</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>扣款成功回傳 true；餘額不足或找不到使用者回傳 false</returns>
    Task<bool> TrySpendAsync(string profileId, decimal amount, CoinTransactionInfo info, bool isVirtual = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加使用者的實體 In 幣（儲值）
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="amount">金額，必須大於 0</param>
    /// <param name="info">交易說明</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>找到使用者並加值回傳 true</returns>
    Task<bool> AddRealCoinsAsync(string profileId, decimal amount, CoinTransactionInfo info, CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加使用者的虛擬 In 幣（任務、成就獎勵）
    /// </summary>
    /// <param name="profileId">Profile ID</param>
    /// <param name="amount">金額，必須大於 0</param>
    /// <param name="info">交易說明</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>找到使用者並加值回傳 true</returns>
    Task<bool> AddVirtualCoinsAsync(string profileId, decimal amount, CoinTransactionInfo info, CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加創作者的月收入（分潤）
    /// </summary>
    /// <param name="profileId">創作者的 Profile ID</param>
    /// <param name="amount">金額，必須大於 0</param>
    /// <param name="info">交易說明</param>
    /// <param name="cancellationToken">取消權杖</param>
    /// <returns>找到使用者並加值回傳 true</returns>
    Task<bool> AddCreatorIncomeAsync(string profileId, decimal amount, CoinTransactionInfo info, CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加創作的月累積與總累積 In 幣。這是作品的統計數字，不寫入流水帳
    /// </summary>
    /// <param name="novelId">Novel ID</param>
    /// <param name="amount">金額，必須大於 0</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task AddNovelCoinsAsync(int novelId, decimal amount, CancellationToken cancellationToken = default);

    /// <summary>
    /// 增加文章的月累積與總累積 In 幣。這是作品的統計數字，不寫入流水帳
    /// </summary>
    /// <param name="articleId">Article ID</param>
    /// <param name="amount">金額，必須大於 0</param>
    /// <param name="cancellationToken">取消權杖</param>
    Task AddArticleCoinsAsync(int articleId, decimal amount, CancellationToken cancellationToken = default);
}
