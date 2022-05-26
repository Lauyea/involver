using System.Security.Cryptography;
using System.Text;

namespace Involver.Extensions
{
    public static class StringExtensions
    {
        public static string ToMd5(this string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;

            var loweredBytes = Encoding.Default.GetBytes(s.ToLower());

            using var md5 = MD5.Create();

            var buffer = md5.ComputeHash(loweredBytes);

            var sb = new StringBuilder(buffer.Length * 2);

            for (var i = 0; i < buffer.Length; i++)
            {
                sb.Append(buffer[i].ToString("X2"));
            }

            return sb.ToString().ToLower();
        }
    }
}
