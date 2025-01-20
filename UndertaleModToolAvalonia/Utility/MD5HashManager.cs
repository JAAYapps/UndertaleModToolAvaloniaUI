using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModToolAvalonia.Utility
{
    public static class MD5HashManager
    {
        
        public static string GenerateMD5(string filename)
        {
            using (MD5 md5 = MD5.Create())
            {
                using (FileStream fs = File.OpenRead(filename))
                {
                    byte[] hash = md5.ComputeHash(fs);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
