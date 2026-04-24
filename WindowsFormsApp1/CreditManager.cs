using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace WindowsFormsApp1
{
    public static class CreditManager
    {
        private static string Conn => DatabaseConfig.ConnectionString;

        public static DataTable GetAccounts(string search = null)
        {
            using (var conn = new MySqlConnection(Conn))
            {
                conn.Open();
                string query = @"
                    SELECT
                        ca.account_id,
                        ca.customer_name,
                        ca.label,
                        COALESCE(SUM(CASE WHEN ct.direction = 'DEBIT' THEN ct.amount ELSE -ct.amount END), 0) AS outstanding_balance,
                        ca.is_active,
                        ca.created_by,
                        ca.created_at
                    FROM credit_accounts ca
                    LEFT JOIN credit_transactions ct ON ct.account_id = ca.account_id
                    WHERE ca.is_active = 1";

                if (!string.IsNullOrWhiteSpace(search))
                    query += " AND (ca.customer_name LIKE @search OR ca.label LIKE @search)";

                query += " GROUP BY ca.account_id ORDER BY ca.customer_name ASC, ca.label ASC";

                var cmd = new MySqlCommand(query, conn);
                if (!string.IsNullOrWhiteSpace(search))
                    cmd.Parameters.AddWithValue("@search", "%" + search + "%");

                var dt = new DataTable();
                new MySqlDataAdapter(cmd).Fill(dt);
                return dt;
            }
        }

        public static DataTable GetActiveAccountsForSelection()
        {
            using (var conn = new MySqlConnection(Conn))
            {
                conn.Open();
                const string query = @"
                    SELECT ca.account_id, ca.customer_name, ca.label,
                           COALESCE(SUM(CASE WHEN ct.direction = 'DEBIT' THEN ct.amount ELSE -ct.amount END), 0) AS outstanding_balance
                    FROM credit_accounts ca
                    LEFT JOIN credit_transactions ct ON ct.account_id = ca.account_id
                    WHERE ca.is_active = 1
                    GROUP BY ca.account_id
                    ORDER BY ca.customer_name ASC, ca.label ASC";
                var dt = new DataTable();
                new MySqlDataAdapter(new MySqlCommand(query, conn)).Fill(dt);
                return dt;
            }
        }

        public static int CreateAccount(string customerName, string label)
        {
            using (var conn = new MySqlConnection(Conn))
            {
                conn.Open();
                const string query = @"INSERT INTO credit_accounts (customer_name, label, created_by, created_at)
                                       VALUES (@name, @label, @created_by, NOW())";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@name", customerName.Trim());
                cmd.Parameters.AddWithValue("@label", (label ?? "").Trim());
                cmd.Parameters.AddWithValue("@created_by", UserSession.Username ?? "system");
                cmd.ExecuteNonQuery();
                return (int)cmd.LastInsertedId;
            }
        }

        public static DataRow GetAccountHeader(int accountId)
        {
            using (var conn = new MySqlConnection(Conn))
            {
                conn.Open();
                var cmd = new MySqlCommand("SELECT * FROM credit_accounts WHERE account_id = @id", conn);
                cmd.Parameters.AddWithValue("@id", accountId);
                var dt = new DataTable();
                new MySqlDataAdapter(cmd).Fill(dt);
                return dt.Rows.Count > 0 ? dt.Rows[0] : null;
            }
        }

        public static decimal GetAccountBalance(int accountId)
        {
            using (var conn = new MySqlConnection(Conn))
            {
                conn.Open();
                const string query = "SELECT COALESCE(SUM(CASE WHEN direction = 'DEBIT' THEN amount ELSE -amount END), 0) FROM credit_transactions WHERE account_id = @id";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@id", accountId);
                object result = cmd.ExecuteScalar();
                return result == null || result == DBNull.Value ? 0m : Convert.ToDecimal(result);
            }
        }

        public static DataTable GetTransactionsWithBalance(int accountId)
        {
            using (var conn = new MySqlConnection(Conn))
            {
                conn.Open();
                const string query = @"
                    SELECT txn_id, txn_type, txn_date, description, bill_code,
                           CASE WHEN direction = 'DEBIT' THEN amount ELSE 0 END AS debit,
                           CASE WHEN direction = 'CREDIT' THEN amount ELSE 0 END AS credit,
                           recorded_by, recorded_at
                    FROM credit_transactions
                    WHERE account_id = @account_id
                    ORDER BY txn_date ASC, txn_id ASC";

                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@account_id", accountId);
                var dt = new DataTable();
                new MySqlDataAdapter(cmd).Fill(dt);

                dt.Columns.Add("running_balance", typeof(decimal));
                decimal running = 0m;
                foreach (DataRow row in dt.Rows)
                {
                    running += Convert.ToDecimal(row["debit"]) - Convert.ToDecimal(row["credit"]);
                    row["running_balance"] = running;
                }
                return dt;
            }
        }

        public static void AddTransaction(int accountId, string txnType, decimal amount, string direction,
            string description, string billCode, DateTime txnDate)
        {
            using (var conn = new MySqlConnection(Conn))
            {
                conn.Open();
                const string query = @"INSERT INTO credit_transactions
                    (account_id, txn_type, amount, direction, description, bill_code, txn_date, recorded_by, recorded_at)
                    VALUES (@account_id, @txn_type, @amount, @direction, @description, @bill_code, @txn_date, @recorded_by, NOW())";
                var cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@account_id", accountId);
                cmd.Parameters.AddWithValue("@txn_type", txnType);
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@direction", direction);
                cmd.Parameters.AddWithValue("@description", description ?? "");
                cmd.Parameters.AddWithValue("@bill_code", string.IsNullOrWhiteSpace(billCode) ? (object)DBNull.Value : billCode);
                cmd.Parameters.AddWithValue("@txn_date", txnDate.Date);
                cmd.Parameters.AddWithValue("@recorded_by", UserSession.Username ?? "system");
                cmd.ExecuteNonQuery();
            }
        }

        public static void DeactivateAccount(int accountId)
        {
            using (var conn = new MySqlConnection(Conn))
            {
                conn.Open();
                var cmd = new MySqlCommand("UPDATE credit_accounts SET is_active = 0 WHERE account_id = @id", conn);
                cmd.Parameters.AddWithValue("@id", accountId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
