using System;
using System.Text;

namespace Client.Common
{
    /// <summary>
    /// функции криптографии
    /// </summary>
    /// <author>balchugov_dv</author>
    /// <version>1</version>
    public static class Cryptor
    {
        public static string MakeMD5(string input)
        {
            using( var md5 = System.Security.Cryptography.MD5.Create() )
            {
                var inputBytes = Encoding.ASCII.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);

                // byte array -> hex string
                var sb = new StringBuilder();
                foreach( var t in hashBytes )
                {
                    sb.Append(t.ToString("X2"));
                }
                return sb.ToString();
            }
        }
        
        public static int MakeRandom(int start=111111, int finish=999999)
        {
            int result=0;
            var rand = new Random(Guid.NewGuid().GetHashCode());
            result = rand.Next(start, finish);
            return result;
        }

        public static string MakeUid()
        {
            var r = Cryptor.MakeRandom();
            return Cryptor.MakeMD5(r.ToString());
        }

        public static string Base64Encode(string plainText) 
        {
          var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
          return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData) 
        {
          var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
          return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

    }
}
