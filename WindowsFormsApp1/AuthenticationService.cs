using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace WindowsFormsApp1
{
    public sealed class AuthenticationResult
    {
        public bool Success { get; set; }
        public bool IsAdmin { get; set; }
        public bool MigratedLegacyPassword { get; set; }
        public string ErrorMessage { get; set; }
    }

    public static class AuthenticationService
    {
        public static AuthenticationResult ValidateLogin(string username, string password)
        {
            var result = new AuthenticationResult();
            string normalizedUsername = (username ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedUsername) || password == null)
            {
                return result;
            }

            try
            {
                using (MySqlConnection conn = new MySqlConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE username = @username LIMIT 1", conn))
                    {
                        cmd.Parameters.AddWithValue("@username", normalizedUsername);
                        DataTable table = new DataTable();
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                        {
                            adapter.Fill(table);
                        }

                        if (table.Rows.Count == 0)
                        {
                            return result;
                        }

                        DataRow row = table.Rows[0];
                        bool verified = VerifyCurrentOrLegacyPassword(row, password, out bool legacyMatch);
                        if (!verified)
                        {
                            return result;
                        }

                        result.Success = true;
                        result.IsAdmin = row.Table.Columns.Contains("isAdmin") && row["isAdmin"] != DBNull.Value && Convert.ToInt32(row["isAdmin"]) == 1;

                        if (legacyMatch && HasPasswordHashColumns(table))
                        {
                            result.MigratedLegacyPassword = TryMigrateLegacyPassword(conn, normalizedUsername, password);
                        }

                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = "Unable to validate login. Check the database connection.";
                UpdateLogger.Error("Login validation failed", ex);
                return result;
            }
        }

        private static bool VerifyCurrentOrLegacyPassword(DataRow row, string password, out bool legacyMatch)
        {
            legacyMatch = false;
            if (HasPasswordHashColumns(row.Table))
            {
                string hash = row["password_hash"] == DBNull.Value ? null : Convert.ToString(row["password_hash"]);
                string salt = row["password_salt"] == DBNull.Value ? null : Convert.ToString(row["password_salt"]);
                int iterations = row["password_iterations"] == DBNull.Value ? 0 : Convert.ToInt32(row["password_iterations"]);
                bool hasAnyHashMaterial = !string.IsNullOrWhiteSpace(hash) ||
                                          !string.IsNullOrWhiteSpace(salt) ||
                                          iterations > 0;
                if (PasswordHasher.Verify(password, hash, salt, iterations))
                {
                    return true;
                }
                if (hasAnyHashMaterial)
                {
                    return false;
                }
            }

            if (!row.Table.Columns.Contains("password") || row["password"] == DBNull.Value)
            {
                return false;
            }

            legacyMatch = string.Equals(Convert.ToString(row["password"]), password, StringComparison.Ordinal);
            return legacyMatch;
        }

        private static bool HasPasswordHashColumns(DataTable table)
        {
            return table.Columns.Contains("password_hash") &&
                   table.Columns.Contains("password_salt") &&
                   table.Columns.Contains("password_iterations");
        }

        private static bool TryMigrateLegacyPassword(MySqlConnection conn, string username, string password)
        {
            try
            {
                PasswordHash hash = PasswordHasher.Create(password);
                using (MySqlCommand cmd = new MySqlCommand(
                    @"UPDATE users
                      SET password_hash = @hash,
                          password_salt = @salt,
                          password_iterations = @iterations,
                          password_migrated_at = NOW(),
                          password = NULL
                      WHERE username = @username
                        AND (password_hash IS NULL OR password_hash = '')", conn))
                {
                    cmd.Parameters.AddWithValue("@hash", hash.HashBase64);
                    cmd.Parameters.AddWithValue("@salt", hash.SaltBase64);
                    cmd.Parameters.AddWithValue("@iterations", hash.Iterations);
                    cmd.Parameters.AddWithValue("@username", username);
                    return cmd.ExecuteNonQuery() == 1;
                }
            }
            catch (Exception ex)
            {
                UpdateLogger.Error("Legacy password migration failed after successful password verification", ex);
                return false;
            }
        }
    }
}
