using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Involver.Data;
using Involver.Models;
using Involver.Pages;
using Involver.Services.ECPay;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Involver.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    public class CheckOutFeedbackController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public CheckOutFeedbackController(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET api/CheckOutFeedback/version
        [HttpGet("version")]
        public string Version()
        {
            return "Version 1.0.0";
        }

        // POST: api/TodoItems
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ContentResult> PostCheckOutFeedbackAsync([FromForm]Feedback feedback)
        {
            List<string> enErrors = new List<string>();
            Hashtable htFeedback = null;
            /* 支付後的回傳的基本參數 */
            string CustomField1 = string.Empty;
            string szMerchantID = string.Empty;
            string szMerchantTradeNo = string.Empty;
            string szPaymentDate = string.Empty;
            string szPaymentType = string.Empty;
            string szPaymentTypeChargeFee = string.Empty;
            string szRtnCode = string.Empty;
            string szRtnMsg = string.Empty;
            string szSimulatePaid = string.Empty;
            string szTradeAmt = string.Empty;
            string szTradeDate = string.Empty;
            string szTradeNo = string.Empty;

            string QueryString = "";

            //紀錄HTTP Request內容用
            //HttpRequest req = Request;
            //string BodyString = "";
            //BodyString = await GetRequestBodyAsync(req);
            //_logger.LogInformation("BodyString: " + BodyString);

            //if(req.QueryString != null)
            //{
            //    _logger.LogInformation("QueryString: " + BodyString);
            //}
            //foreach(var header in req.Headers)
            //{
            //    _logger.LogInformation(string.Format("Header key: {0}, value: {1}", header.Key, header.Value));
            //}
            //if (req.HasFormContentType)
            //{
            //    _logger.LogInformation("Form Keys: ");
            //    foreach (var key in req.Form.Keys)
            //    {
            //        _logger.LogInformation(" Key: " + key + ", Value: " + req.Form[key]);
            //    }
            //}

            try
            {
                using (Payment oPayment = new Payment(_logger))
                {
                    oPayment.HashKey = "5294y06JbISpM5x9";//ECPay 提供的 HashKey
                    oPayment.HashIV = "v77hoKGq4kWxNNIS";//ECPay 提供的 HashIV
                    /* 取回付款結果 */
                    enErrors.AddRange(oPayment.CheckOutFeedback(ref htFeedback, Request, ref QueryString));
                }
                // 取回所有資料
                if (enErrors.Count() == 0)
                {
                    // 取得資料
                    foreach (string szKey in htFeedback.Keys)
                    {
                        switch (szKey)
                        {
                            /* 支付後的回傳的基本參數 */
                            case "CustomField1": CustomField1 = htFeedback[szKey].ToString(); break;
                            case "MerchantID": szMerchantID = htFeedback[szKey].ToString(); break;
                            case "MerchantTradeNo": szMerchantTradeNo = htFeedback[szKey].ToString(); break;
                            case "PaymentDate": szPaymentDate = htFeedback[szKey].ToString(); break;
                            case "PaymentType": szPaymentType = htFeedback[szKey].ToString(); break;
                            case "PaymentTypeChargeFee": szPaymentTypeChargeFee = htFeedback[szKey].ToString(); break;
                            case "RtnCode": szRtnCode = htFeedback[szKey].ToString(); break;
                            case "RtnMsg": szRtnMsg = htFeedback[szKey].ToString(); break;
                            case "SimulatePaid": szSimulatePaid = htFeedback[szKey].ToString(); break;
                            case "TradeAmt": szTradeAmt = htFeedback[szKey].ToString(); break;
                            case "TradeDate": szTradeDate = htFeedback[szKey].ToString(); break;
                            case "TradeNo": szTradeNo = htFeedback[szKey].ToString(); break;
                            default: break;
                        }
                    }
                }
                else
                {
                    // 其他資料處理。
                    foreach (string szKey in htFeedback.Keys)
                    {
                        switch (szKey)
                        {
                            /* 支付後的回傳的基本參數 */
                            case "CustomField1": CustomField1 = htFeedback[szKey].ToString(); break;
                            case "MerchantID": szMerchantID = htFeedback[szKey].ToString(); break;
                            case "MerchantTradeNo": szMerchantTradeNo = htFeedback[szKey].ToString(); break;
                            case "PaymentDate": szPaymentDate = htFeedback[szKey].ToString(); break;
                            case "PaymentType": szPaymentType = htFeedback[szKey].ToString(); break;
                            case "PaymentTypeChargeFee": szPaymentTypeChargeFee = htFeedback[szKey].ToString(); break;
                            case "RtnCode": szRtnCode = htFeedback[szKey].ToString(); break;
                            case "RtnMsg": szRtnMsg = htFeedback[szKey].ToString(); break;
                            case "SimulatePaid": szSimulatePaid = htFeedback[szKey].ToString(); break;
                            case "TradeAmt": szTradeAmt = htFeedback[szKey].ToString(); break;
                            case "TradeDate": szTradeDate = htFeedback[szKey].ToString(); break;
                            case "TradeNo": szTradeNo = htFeedback[szKey].ToString(); break;
                            default: break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 例外錯誤處理。
                enErrors.Add(ex.Message);
            }

            string ReturnString = "";

            // 回覆成功訊息。
            if (enErrors.Count() == 0)
            {
                ReturnString = "1|OK";
                if (szSimulatePaid == "0" && szRtnCode == "1" && CustomField1 != "")
                {
                    //用戶資料增加 RealCoins
                    Profile profile = await _context.Profiles.Where(p => p.ProfileID == CustomField1).FirstOrDefaultAsync();
                    profile.RealCoins += decimal.Parse(szTradeAmt);
                }
                
            }
            // 回覆錯誤訊息。
            else
            {
                ReturnString = string.Format("0|{0}", string.Join("\\r\\n", enErrors));
            }

            //紀錄帳單與回應資訊
            try
            {
                _context.Payments.Add(
                new Models.StatisticalData.Payment
                {
                    InvolverID = CustomField1,
                    PaymentDate = szPaymentDate,
                    RtnMsg = szRtnMsg,
                    TradeAmt = int.Parse(szTradeAmt),
                    TradeDate = szTradeDate,
                    TradeNo = szTradeNo,
                    ReturnString = ReturnString,
                    RequestBody = QueryString,
                    MerchantTradeNo = szMerchantTradeNo,
                    RtnCode = int.Parse(szRtnCode),
                    SimulatePaid = int.Parse(szSimulatePaid)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.ToString());
                _context.Payments.Add(
                new Models.StatisticalData.Payment
                {
                    PaymentDate = DateTime.Now.ToString(),
                    ReturnString = ReturnString,
                    RequestBody = QueryString
                });
            }

            await _context.SaveChangesAsync();

            return Content(ReturnString);
        }

        private async Task<string> GetRequestBodyAsync(HttpRequest Request)
        {
            string BodyString = "";
            Request.EnableBuffering();
            Request.Body.Seek(0, SeekOrigin.Begin);
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8, false, 1024, true))
            {
                BodyString = await reader.ReadToEndAsync();
            }
            Request.Body.Position = 0;
            return BodyString;
        }
    }
}
