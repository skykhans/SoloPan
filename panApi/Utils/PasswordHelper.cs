using System.Security.Cryptography;
using System.Text;

namespace PanSystem.Utils
{
    public static class PasswordHelper
    {
        private const string AlgorithmTag = "PBKDF2";
        private const string DigestTag = "SHA256";
        private const int SaltSize = 16;
        private const int KeySize = 32;
        private const int Iterations = 120000;

        public static string HashPassword(string password)
        {
            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(password),
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                KeySize
            );

            return $"{AlgorithmTag}${DigestTag}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool VerifyPassword(string password, string storedHash, out bool needsRehash)
        {
            needsRehash = false;
            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            if (storedHash.StartsWith($"{AlgorithmTag}${DigestTag}$", StringComparison.Ordinal))
            {
                var parts = storedHash.Split('$');
                if (parts.Length != 5) return false;
                if (!int.TryParse(parts[2], out var iterations) || iterations <= 0) return false;

                byte[] salt;
                byte[] storedKey;
                try
                {
                    salt = Convert.FromBase64String(parts[3]);
                    storedKey = Convert.FromBase64String(parts[4]);
                }
                catch
                {
                    return false;
                }

                var actualKey = Rfc2898DeriveBytes.Pbkdf2(
                    Encoding.UTF8.GetBytes(password),
                    salt,
                    iterations,
                    HashAlgorithmName.SHA256,
                    storedKey.Length
                );

                var ok = CryptographicOperations.FixedTimeEquals(actualKey, storedKey);
                if (ok && iterations < Iterations)
                {
                    needsRehash = true;
                }
                return ok;
            }

            // 兼容历史 MD5 密码：验证通过后在登录流程迁移为 PBKDF2-SHA256。
            var legacyOk = string.Equals(HashHelper.ComputeMd5(password), storedHash, StringComparison.OrdinalIgnoreCase);
            if (legacyOk)
            {
                needsRehash = true;
            }
            return legacyOk;
        }
    }
}
