namespace DataAccess.Models.StatisticalData
{
    /// <summary>
    /// 金流商。名稱會組進流水帳的關聯 ID 與防重複鍵，只能新增，不要改名或修改既有的數值
    /// </summary>
    public enum PaymentProvider : short
    {
        /// <summary>綠界</summary>
        ECPay = 1,

        /// <summary>藍新</summary>
        NewebPay = 2,

        /// <summary>PayPal</summary>
        PayPal = 3,

        /// <summary>Stripe</summary>
        Stripe = 4,

        /// <summary>Patreon</summary>
        Patreon = 5,

        /// <summary>直接串接收單銀行的信用卡</summary>
        CreditCard = 6,
    }
}
