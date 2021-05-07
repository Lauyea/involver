using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Involver.Services.ECPay
{
    internal static class MD5Encoder
    {
        /// <summary>
        /// 雜湊加密演算法物件。
        /// </summary>
        private static readonly HashAlgorithm Crypto = null;

        static MD5Encoder()
        {
            Crypto = new MD5CryptoServiceProvider();
        }

        public static string Encrypt(string originalString)
        {
            byte[] byValue = Encoding.UTF8.GetBytes(originalString);
            byte[] byHash = Crypto.ComputeHash(byValue);

            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < byHash.Length; i++)
            {
                //stringBuilder.Append(byHash[i].ToString("X2"));
                stringBuilder.Append(byHash[i].ToString("X").PadLeft(2, '0'));
            }

            return stringBuilder.ToString().ToUpper();
        }
    }
}
