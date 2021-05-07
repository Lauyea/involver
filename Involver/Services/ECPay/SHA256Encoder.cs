using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Involver.Services.ECPay
{
    internal static class SHA256Encoder
    {
        /// <summary>
        /// 雜湊加密演算法物件。
        /// </summary>
        private static readonly HashAlgorithm Crypto = null;

        static SHA256Encoder()
        {
            Crypto = new SHA256CryptoServiceProvider();
        }

        public static string Encrypt(string originalString)
        {
            byte[] source = Encoding.Default.GetBytes(originalString);//將字串轉為Byte[]
            byte[] crypto = Crypto.ComputeHash(source);//進行SHA256加密
            string result = string.Empty;

            for (int i = 0; i < crypto.Length; i++)
            {
                result += crypto[i].ToString("X2");
            }

            return result.ToUpper();
        }
    }
}
