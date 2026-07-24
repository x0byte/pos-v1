using System;
using System.Security.Cryptography;
using System.Text;

namespace WindowsFormsApp1
{
    public static class SecretProtector
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("STC_POS_LOCAL_SECRET_V1");

        public static string Protect(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            byte[] raw = Encoding.UTF8.GetBytes(value);
            byte[] protectedBytes = ProtectedData.Protect(raw, Entropy, DataProtectionScope.LocalMachine);
            return Convert.ToBase64String(protectedBytes);
        }

        public static string Unprotect(string protectedValue)
        {
            if (string.IsNullOrEmpty(protectedValue))
            {
                return string.Empty;
            }

            byte[] protectedBytes = Convert.FromBase64String(protectedValue);
            try
            {
                byte[] raw = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.LocalMachine);
                return Encoding.UTF8.GetString(raw);
            }
            catch (CryptographicException)
            {
                byte[] raw = ProtectedData.Unprotect(protectedBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(raw);
            }
        }
    }
}
