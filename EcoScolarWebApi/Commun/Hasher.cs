using Docker.DotNet.Models;
using System.Security.Cryptography;
using System.Text;

namespace EcoscolarWebApi.Commun
{
    public class Hasher
    {
        public static string HashString(string input)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            StringBuilder builder = new();
            foreach (var b in bytes)
            {
                builder.Append(b.ToString("x2"));
            }
            return builder.ToString();
        }
    }
}