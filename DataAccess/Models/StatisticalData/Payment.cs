using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Models.StatisticalData
{
    /// <summary>
    /// 介接綠界用的Payment model
    /// </summary>
    public class Payment
    {
        public int PaymentID { get; set; }
        [Display(Name = "交易訊息")]
        public string? RtnMsg { get; set; }
        [Display(Name = "綠界的交易編號")]
        public string? TradeNo { get; set; }
        [Display(Name = "交易金額")]
        public int TradeAmt { get; set; }
        [Display(Name = "付款時間")]
        public string? PaymentDate { get; set; }
        [Display(Name = "訂單成立時間")]
        public string? TradeDate { get; set; }
        [Display(Name = "回應字串")]
        public string? ReturnString { get; set; }
        [Display(Name = "用戶ID")]
        public string? InvolverID { get; set; }
        [Display(Name = "要求字串")]
        public string? RequestBody { get; set; }
        [Display(Name = "特店交易編號")]
        public string? MerchantTradeNo { get; set; }
        [Display(Name = "交易狀態")]
        public int RtnCode { get; set; }
        [Display(Name = "模擬付款")]
        public int SimulatePaid { get; set; }
    }
}