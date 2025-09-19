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
        public static string Encrypt(string originalString)
        {
            byte[] byValue = Encoding.UTF8.GetBytes(originalString);

            using var md5 = MD5.Create();

            byte[] byHash = md5.ComputeHash(byValue);

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