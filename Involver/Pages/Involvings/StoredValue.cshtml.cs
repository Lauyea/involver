using System.ComponentModel.DataAnnotations;
using System.Web;

using DataAccess.Data;

using Involver.Common;
using Involver.Services.ECPay;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Involver.Pages.Involvings
{
    [AllowAnonymous]
    public class StoredValueModel : DI_BasePageModel
    {
        public StoredValueModel(
        ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<InvolverUser> userManager,
        IHttpClientFactory clientFactory,
        IConfiguration configuration)
        : base(context, authorizationService, userManager)
        {
            _clientFactory = clientFactory;
            Configuration = configuration;
        }

        private readonly IHttpClientFactory _clientFactory;
        public IConfiguration Configuration { get; }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        [Display(Name = "數量")]
        [Range(1, 100, ErrorMessage = "大小只能介於0到100之間")]
        public int Quantity { get; set; }
        [BindProperty]
        public string UserID { get; set; }

        // To protect from overposting attacks, please enable the specific properties you want to bind to, for
        // more details see https://aka.ms/RazorPagesCRUD.
        public IActionResult OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            UserID = _userManager.GetUserId(User);

            if (UserID == null)
            {
                return Challenge();
            }

            var client = _clientFactory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/x-www-form-urlencoded");

            //### 建立Service
            CommonService _CommonService = new CommonService();

            //### 組合檢查碼
            string PostURL = "https://payment.ecpay.com.tw/Cashier/AioCheckOut/V5";
            string MerchantID = "3216093";
            string HashKey = Configuration["ECPay:HashKey"];
            string HashIV = Configuration["ECPay:HashIV"];

            SortedDictionary<string, string> PostCollection = new SortedDictionary<string, string>();
            PostCollection.Add("MerchantID", MerchantID);
            PostCollection.Add("MerchantTradeNo", DateTime.Now.ToString("yyyyMMddHHmmss"));//廠商訂單編號
            PostCollection.Add("MerchantTradeDate", DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"));//廠商訂單日期
            PostCollection.Add("PaymentType", "aio");//固定帶aio
            PostCollection.Add("TotalAmount", (Quantity * 150).ToString());
            PostCollection.Add("TradeDesc", "交易描述:Involver的ECPay訂單");
            PostCollection.Add("ItemName", "Involver實體In幣");
            PostCollection.Add("ReturnURL", "https://involver.tw/api/CheckOutFeedback");//廠商通知付款結果API
            PostCollection.Add("ChoosePayment", "ALL");//選擇預設付款方式   
            PostCollection.Add("OrderResultURL", ""); //導回頁面
            PostCollection.Add("EncryptType", "1");//固定
            PostCollection.Add("StoreID", ""); //特店旗下店舖代號
            PostCollection.Add("CustomField1", UserID); //會員ID

            //壓碼
            string str = string.Empty;
            string str_pre = string.Empty;
            foreach (var item in PostCollection)
            {
                str += string.Format("&{0}={1}", item.Key, item.Value);
            }

            str_pre += string.Format("HashKey={0}" + str + "&HashIV={1}", HashKey, HashIV);

            string urlEncodeStrPost = HttpUtility.UrlEncode(str_pre);
            string ToLower = urlEncodeStrPost.ToLower();
            string sCheckMacValue = _CommonService.GetSHA256(ToLower);
            PostCollection.Add("CheckMacValue", sCheckMacValue);

            //### Form Post To ECPay
            string ParameterString = string.Join("&", PostCollection.Select(p => p.Key + "=" + p.Value));

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("<html><body>").AppendLine();
            sb.Append("<form name='ECPayAIO'  id='ECPayAIO' action='" + PostURL + "' method='POST'>").AppendLine();
            foreach (var aa in PostCollection)
            {
                sb.Append("<input type='hidden' name='" + aa.Key + "' value='" + aa.Value + "'>").AppendLine();
            }

            sb.Append("</form>").AppendLine();
            sb.Append("<script> var theForm = document.forms['ECPayAIO'];  if (!theForm) { theForm = document.ECPayAIO; } theForm.submit(); </script>").AppendLine();
            sb.Append("<html><body>").AppendLine();

            TempData["PostForm"] = sb.ToString();

            //use http client
            //var formData = new FormUrlEncodedContent(PostCollection);

            //using var response = await client.PostAsync(PostURL, formData);

            //if (response.IsSuccessStatusCode)
            //{
            //    using var responseStream = await response.Content.ReadAsStreamAsync();
            //    string TempStr = await response.Content.ReadAsStringAsync();
            //    TempData["PostForm"] = await response.Content.ReadAsStringAsync();
            //}
            //else
            //{

            //}

            return Page();
        }
    }
}