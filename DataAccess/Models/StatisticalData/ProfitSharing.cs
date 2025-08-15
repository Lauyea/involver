using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace DataAccess.Models.StatisticalData
{
    public class ProfitSharing
    {
        public int ProfitSharingID { get; set; }
        [Display(Name = "用戶ID")]
        public string? InvolverID { get; set; }
        [Display(Name = "用戶銀行帳戶")]
        public string? CreditCard { get; set; }
        [Display(Name = "分潤總額")]
        public int SharingValue { get; set; }
        [Display(Name = "分潤動作完成")]
        public bool SharingDone { get; set; }
    }
}
