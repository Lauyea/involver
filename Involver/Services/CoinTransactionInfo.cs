using DataAccess.Models.StatisticalData;

namespace Involver.Services;

/// <summary>
/// 寫入 In 幣流水帳時的交易說明
/// </summary>
/// <param name="Type">交易類型</param>
/// <param name="ReferenceType">關聯對象的種類</param>
/// <param name="ReferenceId">關聯對象的 ID</param>
/// <param name="CounterpartyProfileId">交易對象的 Profile ID</param>
/// <param name="Note">備註，超過 200 字會被截斷</param>
/// <param name="IdempotencyKey">防止重複入帳的鍵值，有值時不可重複</param>
public sealed record CoinTransactionInfo(
    CoinTransactionType Type,
    CoinReferenceType? ReferenceType = null,
    string ReferenceId = null,
    string CounterpartyProfileId = null,
    string Note = null,
    string IdempotencyKey = null)
{
    /// <summary>
    /// 金流付款編號的長度上限。Stripe 建議預留 255 字元，其他金流商都比這短
    /// </summary>
    public const int PaymentIdMaxLength = 255;

    /// <summary>
    /// 建立儲值的交易說明，適用於任何金流商。
    /// 關聯 ID 為「金流商:付款編號」，防重複鍵為「TopUp:金流商:付款編號」，
    /// 金流商重送通知時不會重複入帳，不同金流商的編號相同也不會互相衝突。
    /// </summary>
    /// <param name="provider">金流商</param>
    /// <param name="paymentId">能唯一識別這筆付款的編號，例如我方送給金流商的訂單編號或金流商回傳的交易編號；
    /// 同一個金流商必須固定使用同一種編號，重送的通知才會被擋下</param>
    /// <param name="note">備註</param>
    /// <returns>儲值的交易說明</returns>
    public static CoinTransactionInfo ForTopUp(PaymentProvider provider, string paymentId, string note = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentId);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(paymentId.Length, PaymentIdMaxLength, nameof(paymentId));
        if (!Enum.IsDefined(provider))
        {
            throw new ArgumentOutOfRangeException(nameof(provider), provider, null);
        }

        string reference = $"{provider}:{paymentId}";
        return new CoinTransactionInfo(
            CoinTransactionType.TopUp,
            CoinReferenceType.Payment,
            reference,
            Note: note,
            IdempotencyKey: $"{CoinTransactionType.TopUp}:{reference}");
    }
}
