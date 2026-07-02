using System.Security.Cryptography;
using System.Text;

namespace LoginSystem.Services
{
    public static class SecureTokenGenerator
    {
        // Raw, URL-safe token — this is what goes in the email link / to the client.
        public static string Generate(int byteLength = 32)
        {
            var bytes = RandomNumberGenerator.GetBytes(byteLength);
            return Convert.ToBase64String(bytes).Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        // What actually gets stored — same principle as password hashing: never
        // persist a value that's directly usable if the DB leaks.
        public static string Hash(string rawToken) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
    }
}
