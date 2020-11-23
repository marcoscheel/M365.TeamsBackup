using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace M365.TeamsBackup.Core.Data.Util
{
    public static class MD5Hash
    {
        private static MD5 _MD5 = MD5.Create();
        public static string Get(string unicodeText)
        {
            byte[] result = _MD5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(unicodeText));
            var sb = new StringBuilder();
            for (int i = 0; i < result.Length; i++)
            {
                sb.Append(result[i].ToString("X2"));
            }
            return sb.ToString();
        }

    }
}
