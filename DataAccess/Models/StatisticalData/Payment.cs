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
        /// <summary>
        /// 綠界回傳的付款時間原始字串（金流紀錄留存，不做轉換）
        /// </summary>
        [Display(Name = "付款時間（原始）")]
        public string? PaymentDate { get; set; }
        /// <summary>
        /// 綠界回傳的訂單成立時間原始字串（金流紀錄留存，不做轉換）
        /// </summary>
        [Display(Name = "訂單成立時間（原始）")]
        public string? TradeDate { get; set; }
        /// <summary>
        /// 付款時間；由 <see cref="PaymentDate"/> 解析而來，格式無法解析時為 null
        /// </summary>
        [DataType(DataType.DateTime)]
        [Display(Name = "付款時間")]
        public DateTime? PaymentTime { get; set; }
        /// <summary>
        /// 訂單成立時間；由 <see cref="TradeDate"/> 解析而來，格式無法解析時為 null
        /// </summary>
        [DataType(DataType.DateTime)]
        [Display(Name = "訂單成立時間")]
        public DateTime? TradeTime { get; set; }
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

        /// <summary>
        /// 是否已作廢。金流紀錄不刪除；錯誤的訂單標記作廢，需要退回 In 幣時另以管理員調整沖正
        /// </summary>
        [Display(Name = "已作廢")]
        public bool IsVoided { get; set; }

        /// <summary>
        /// 作廢時間（UTC）
        /// </summary>
        [Display(Name = "作廢時間")]
        public DateTime? VoidedAt { get; set; }

        /// <summary>
        /// 作廢原因
        /// </summary>
        [StringLength(200)]
        [Display(Name = "作廢原因")]
        public string? VoidReason { get; set; }
    }
}