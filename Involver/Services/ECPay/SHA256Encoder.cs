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
        public static string Encrypt(string originalString)
        {
            byte[] source = Encoding.Default.GetBytes(originalString);//將字串轉為Byte[]

            using var sHA256 = SHA256.Create();

            byte[] crypto = sHA256.ComputeHash(source);//進行SHA256加密
            string result = string.Empty;

            for (int i = 0; i < crypto.Length; i++)
            {
                result += crypto[i].ToString("X2");
            }

            return result.ToUpper();
        }
    }
}