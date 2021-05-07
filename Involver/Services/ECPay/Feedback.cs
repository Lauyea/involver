using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Involver.Services.ECPay
{
    public class Feedback
    {
        public string CustomField1 { get; set; }
        public string CustomField2 { get; set; }
        public string CustomField3 { get; set; }
        public string CustomField4 { get; set; }
        public int MerchantID { get; set; }
        public string MerchantTradeNo { get; set; }
        public DateTime PaymentDate { get; set; }
        public string PaymentType { get; set; }
        public int PaymentTypeChargeFee { get; set; }
        public int RtnCode { get; set; }
        public string RtnMsg { get; set; }
        public int SimulatePaid { get; set; }
        public int? StoreID { get; set; }
        public int TradeAmt { get; set; }
        public DateTime TradeDate { get; set; }
        public string TradeNo { get; set; }
        public string CheckMacValue { get; set; }
    }
}
