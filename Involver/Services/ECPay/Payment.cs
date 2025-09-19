using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

using Involver.Pages;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Involver.Services.ECPay
{
    public class Payment : IDisposable
    {
        private readonly ILogger<IndexModel> _logger;

        public Payment(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// 介接的 HashKey。
        /// </summary>
        [Required(ErrorMessage = "{0} is required.")]
        public string HashKey { get; set; }
        /// <summary>
        /// 介接的 HashIV。
        /// </summary>
        [Required(ErrorMessage = "{0} is required.")]
        public string HashIV { get; set; }

        public IEnumerable<string> CheckOutFeedback(ref Hashtable feedback, HttpRequest request, ref string QueryString)
        {
            string szParameters = string.Empty;
            string szCheckMacValue = string.Empty;

            List<string> errList = new List<string>();

            if (null == feedback) feedback = new Hashtable();

            NameValueCollection qscoll = new NameValueCollection();
            if (request.Form != null)
            {
                foreach (var key in request.Form.Keys)
                {
                    qscoll.Add(key, request.Form[key]);
                    QueryString += " Key: " + key + ", Value: " + request.Form[key];
                }
            }

            //NameValueCollection qscoll = HttpUtility.ParseQueryString(Request.QueryString.ToString());//if set Request as a parameter

            Array.Sort(qscoll.AllKeys);

            foreach (var szKey in qscoll.AllKeys)
            {
                string szValue = qscoll[szKey];
                if (szKey == null) continue;
                if (szKey != "CheckMacValue")
                {
                    szParameters += string.Format("&{0}={1}", szKey, szValue);

                    if (szKey == "PaymentType")
                    {
                        szValue = szValue.Replace("_CVS", string.Empty);
                        szValue = szValue.Replace("_BARCODE", string.Empty);
                        szValue = szValue.Replace("_CreditCard", string.Empty);
                    }

                    if (szKey == "PeriodType")
                    {
                        szValue = szValue.Replace("Y", "Year");
                        szValue = szValue.Replace("M", "Month");
                        szValue = szValue.Replace("D", "Day");
                    }

                    feedback.Add(szKey, szValue);
                }
                else
                {
                    szCheckMacValue = szValue;
                }
            }

            // 紀錄記錄檔
            _logger.LogInformation(string.Format("INFO   {0}  INPUT   AllInOne.CheckOutFeedback: {1}&CheckMacValue={2}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), szParameters, szCheckMacValue));
            // 比對驗證檢查碼。
            errList.AddRange(this.CompareCheckMacValue(szParameters, szCheckMacValue));

            return errList;
        }

        /// <summary>
        /// 比對驗證檢查碼。
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="checkMacValue"></param>
        /// <returns></returns>
        private IEnumerable<string> CompareCheckMacValue(string parameters, string checkMacValue)
        {
            List<string> errList = new List<string>();

            if (!String.IsNullOrEmpty(checkMacValue))
            {
                // 產生檢查碼。
                string szConfirmMacValueMD5 = this.BuildCheckMacValue(parameters, 0);

                // 產生檢查碼。
                string szConfirmMacValueSHA256 = this.BuildCheckMacValue(parameters, 1);

                // 比對檢查碼。
                if (checkMacValue != szConfirmMacValueMD5 && checkMacValue != szConfirmMacValueSHA256)
                {
                    errList.Add("CheckMacValue verify fail.");
                }

            }
            // 查無檢查碼時，拋出例外。
            else
            {
                if (String.IsNullOrEmpty(checkMacValue)) errList.Add("No CheckMacValue parameter.");
            }

            return errList;
        }

        /// <summary>
        /// 產生檢查碼。
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private string BuildCheckMacValue(string parameters, int encryptType = 0)
        {
            string szCheckMacValue = string.Empty;
            // 產生檢查碼。
            szCheckMacValue = string.Format("HashKey={0}{1}&HashIV={2}", this.HashKey, parameters, this.HashIV);
            szCheckMacValue = HttpUtility.UrlEncode(szCheckMacValue).ToLower();
            if (encryptType == 1)
            {
                szCheckMacValue = SHA256Encoder.Encrypt(szCheckMacValue);
            }
            else
            {
                szCheckMacValue = MD5Encoder.Encrypt(szCheckMacValue);
            }

            return szCheckMacValue;
        }

        #region - 釋放使用資源

        /// <summary>
        /// 執行與釋放 (Free)、釋放 (Release) 或重設 Unmanaged 資源相關聯之應用程式定義的工作。
        /// </summary>
        public void Dispose()
        {
            GC.Collect();
        }

        #endregion
    }
}