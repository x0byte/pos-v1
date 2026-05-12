using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace WindowsFormsApp1
{
    public static class CreditManager
    {
        public class CreditTransactionEntry
        {
            public string TxnType { get; set; }
            public decimal Amount { get; set; }
            public string Direction { get; set; }
            public string Description { get; set; }
            public string BillCode { get; set; }
            public DateTime TransactionDate { get; set; }
        }

        private static string Conn => DatabaseConfig.ConnectionString;
        private static readonly object AccountCacheLock = new object();
        private static DataTable accountSummaryCache;
        private static DateTime accountSummaryLoadedAt = DateTime.MinValue;

        public static Task WarmAccountCacheAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    RefreshAccountSummaryCache();
                }
                catch
                {
                    // Startup warmup should never block login.
                }
            });
        }

        public static DataTable GetAccounts(string search = null)
        {
            DataTable source = GetAccountSummarySnapshot();
            DataTable result = source.Clone();

            IEnumerable<DataRow> rows = source.AsEnumerable()
                .Where(row => Convert.ToInt32(row["is_active"]) == 1);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string q = search.Trim();
                rows = rows.Where(row =>
                    Convert.ToString(row["customer_name"]).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    Convert.ToString(row["label"]).IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            foreach (DataRow row in rows.OrderBy(row => Convert.ToString(row["customer_name"]))
                                        .ThenBy(row => Convert.ToString(row["label"])))
            {
                result.ImportRow(row);
            }

            return result;
        }

        public static DataTable GetActiveAccountsForSelection()
        {
            DataTable source = GetAccountSummarySnapshot();
            DataTable result = new DataTable();
            result.Columns.Add("account_id", typeof(int));
            result.Columns.Add("customer_name", typeof(string));
            result.Columns.Add("label", typeof(string));
            result.Columns.Add("outstanding_balance", typeof(decimal));

            foreach (DataRow row in source.AsEnumerable()
                         .Where(row => Convert.ToInt32(row["is_active"]) == 1)
                         .OrderBy(row => Convert.ToString(row["customer_name"]))
                         .ThenBy(row => Convert.ToString(row["label"])))
            {
                result.Rows.Add(
                    Convert.ToInt32(row["account_id"]),
                    Convert.ToString(row["customer_name"]),
                    Convert.ToString(row["label"]),
                    Convert.ToDecimal(row["outstanding_balance"]));
            }

            return result;
        }

        public static void RefreshAccountSummaryCache()
        {
            using (var conn = new MySqlConnection(Conn))
            {
                conn.Open();
                const string query = @"
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
                    GROUP BY ca.account_id
                    ORDER BY ca.customer_name ASC, ca.label ASC";

                var cmd = new MySqlCommand(query, conn);

                var dt = new DataTable();
                new MySqlDataAdapter(cmd).Fill(dt);

                lock (AccountCacheLock)
                {
                    accountSummaryCache = dt;
                    accountSummaryLoadedAt = DateTime.Now;
                }
            }
        }

        private static DataTable GetAccountSummarySnapshot()
        {
            lock (AccountCacheLock)
            {
                if (accountSummaryCache != null &&
                    DateTime.Now - accountSummaryLoadedAt < TimeSpan.FromMinutes(5))
                {
                    return accountSummaryCache.Copy();
                }
            }

            RefreshAccountSummaryCache();
            lock (AccountCacheLock)
            {
                return accountSummaryCache.Copy();
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
                int accountId = (int)cmd.LastInsertedId;
                InvalidateAccountSummaryCache();
                WarmAccountCacheAsync();
                return accountId;
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

            ApplyBalanceDelta(accountId, direction, amount);
        }

        public static void AddTransactions(int accountId, IEnumerable<CreditTransactionEntry> entries)
        {
            List<CreditTransactionEntry> entryList = entries?.ToList() ?? new List<CreditTransactionEntry>();
            using (var conn = new MySqlConnection(Conn))
            {
                conn.Open();
                using (var tx = conn.BeginTransaction())
                using (var cmd = new MySqlCommand(
                    @"INSERT INTO credit_transactions
                      (account_id, txn_type, amount, direction, description, bill_code, txn_date, recorded_by, recorded_at)
                      VALUES (@account_id, @txn_type, @amount, @direction, @description, @bill_code, @txn_date, @recorded_by, NOW())",
                    conn, tx))
                {
                    cmd.Parameters.Add("@account_id",  MySqlDbType.Int32);
                    cmd.Parameters.Add("@txn_type",    MySqlDbType.VarChar);
                    cmd.Parameters.Add("@amount",      MySqlDbType.Decimal);
                    cmd.Parameters.Add("@direction",   MySqlDbType.VarChar);
                    cmd.Parameters.Add("@description", MySqlDbType.VarChar);
                    cmd.Parameters.Add("@bill_code",   MySqlDbType.VarChar);
                    cmd.Parameters.Add("@txn_date",    MySqlDbType.Date);
                    cmd.Parameters.Add("@recorded_by", MySqlDbType.VarChar);
                    cmd.Prepare();

                    string recordedBy = UserSession.Username ?? "system";
                    foreach (var entry in entryList)
                    {
                        cmd.Parameters["@account_id"].Value  = accountId;
                        cmd.Parameters["@txn_type"].Value    = entry.TxnType;
                        cmd.Parameters["@amount"].Value      = entry.Amount;
                        cmd.Parameters["@direction"].Value   = entry.Direction;
                        cmd.Parameters["@description"].Value = entry.Description ?? "";
                        cmd.Parameters["@bill_code"].Value   = string.IsNullOrWhiteSpace(entry.BillCode) ? (object)DBNull.Value : entry.BillCode;
                        cmd.Parameters["@txn_date"].Value    = entry.TransactionDate.Date;
                        cmd.Parameters["@recorded_by"].Value = recordedBy;
                        cmd.ExecuteNonQuery();
                    }

                    tx.Commit();
                }
            }

            decimal debitTotal = 0m;
            decimal creditTotal = 0m;
            foreach (var entry in entryList)
            {
                if (string.Equals(entry.Direction, "DEBIT", StringComparison.OrdinalIgnoreCase))
                    debitTotal += entry.Amount;
                else
                    creditTotal += entry.Amount;
            }
            ApplyBalanceDelta(accountId, "DEBIT", debitTotal - creditTotal);
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

            MarkAccountInactive(accountId);
        }

        private static void InvalidateAccountSummaryCache()
        {
            lock (AccountCacheLock)
            {
                accountSummaryLoadedAt = DateTime.MinValue;
            }
        }

        private static void ApplyBalanceDelta(int accountId, string direction, decimal amount)
        {
            decimal delta = string.Equals(direction, "DEBIT", StringComparison.OrdinalIgnoreCase)
                ? amount
                : -amount;

            lock (AccountCacheLock)
            {
                if (accountSummaryCache == null)
                {
                    return;
                }

                DataRow row = accountSummaryCache.AsEnumerable()
                    .FirstOrDefault(r => Convert.ToInt32(r["account_id"]) == accountId);
                if (row == null)
                {
                    accountSummaryLoadedAt = DateTime.MinValue;
                    return;
                }

                row["outstanding_balance"] = Convert.ToDecimal(row["outstanding_balance"]) + delta;
            }
        }

        private static void MarkAccountInactive(int accountId)
        {
            lock (AccountCacheLock)
            {
                if (accountSummaryCache == null)
                {
                    return;
                }

                DataRow row = accountSummaryCache.AsEnumerable()
                    .FirstOrDefault(r => Convert.ToInt32(r["account_id"]) == accountId);
                if (row != null)
                {
                    row["is_active"] = 0;
                }
            }
        }
    }
}
