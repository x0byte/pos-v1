using System;
using System.Security.Cryptography;

namespace WindowsFormsApp1
{
    public static class PasswordHasher
    {
        public const int DefaultIterations = 150000;
        private const int SaltBytes = 16;
        private const int HashBytes = 32;

        public static PasswordHash Create(string password)
        {
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            byte[] salt = new byte[SaltBytes];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = Derive(password, salt, DefaultIterations);
            return new PasswordHash
            {
                HashBase64 = Convert.ToBase64String(hash),
                SaltBase64 = Convert.ToBase64String(salt),
                Iterations = DefaultIterations
            };
        }

        public static bool Verify(string password, string hashBase64, string saltBase64, int iterations)
        {
            if (password == null || string.IsNullOrWhiteSpace(hashBase64) || string.IsNullOrWhiteSpace(saltBase64) || iterations <= 0)
            {
                return false;
            }

            byte[] expected;
            byte[] salt;
            try
            {
                expected = Convert.FromBase64String(hashBase64);
                salt = Convert.FromBase64String(saltBase64);
            }
            catch
            {
                return false;
            }

            byte[] actual = Derive(password, salt, iterations);
            return FixedTimeEquals(expected, actual);
        }

        private static byte[] Derive(string password, byte[] salt, int iterations)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256))
            {
                return pbkdf2.GetBytes(HashBytes);
            }
        }

        private static bool FixedTimeEquals(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length)
            {
                return false;
            }

            int diff = 0;
            for (int i = 0; i < left.Length; i++)
            {
                diff |= left[i] ^ right[i];
            }

            return diff == 0;
        }
    }

    public sealed class PasswordHash
    {
        public string HashBase64 { get; set; }
        public string SaltBase64 { get; set; }
        public int Iterations { get; set; }
    }
}
