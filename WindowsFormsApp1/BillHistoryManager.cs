using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public static class BillHistoryManager
    {
        private static string connectionString => DatabaseConfig.ConnectionString;

        public class VoidBillResult
        {
            public string BillCode { get; set; }
            public int StockReversalCount { get; set; }
            public int CreditReversalCount { get; set; }
            public bool CreditBillWithoutLedgerMatch { get; set; }
        }

        public sealed class BillSaveResult
        {
            public string BillCode { get; set; }
            public long BillId { get; set; }
            public bool ExistingSubmission { get; set; }
        }

        public static string SaveBill(DataGridView dataGridView, string salesperson, decimal totalAmount, decimal discountAmount)
        {
            return SaveBill(dataGridView, salesperson, totalAmount, discountAmount, null, "CASH");
        }

        public static string SaveBill(DataGridView dataGridView, string salesperson, decimal totalAmount, decimal discountAmount, string clientSubmissionId)
        {
            return SaveBill(dataGridView, salesperson, totalAmount, discountAmount, clientSubmissionId, "CASH");
        }

        public static string SaveBill(DataGridView dataGridView, string salesperson, decimal totalAmount, decimal discountAmount, string clientSubmissionId, string paymentMethod)
        {
            List<BillLineRecord> items = new List<BillLineRecord>();

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                items.Add(new BillLineRecord
                {
                    ItemName = row.Cells[1].Value?.ToString() ?? string.Empty,
                    Rate = SafeToDecimal(row.Cells[2].Value),
                    Amount = SafeToDecimal(row.Cells[3].Value),
                    DiscountedPrice = SafeToDecimal(row.Cells[4].Value)
                });
            }

            return SaveBillInternal(salesperson, totalAmount, discountAmount, items, clientSubmissionId, DateTime.Now, paymentMethod ?? "CASH", 0).BillCode;
        }

        public static string SaveBillFromDataTable(string salesperson, decimal totalAmount, decimal discountAmount, DataTable itemsTable, string clientSubmissionId = null)
        {
            return SaveBillFromDataTable(salesperson, totalAmount, discountAmount, itemsTable, clientSubmissionId, "CASH");
        }

        public static string SaveBillFromDataTable(string salesperson, decimal totalAmount, decimal discountAmount, DataTable itemsTable, string clientSubmissionId, string paymentMethod)
        {
            List<BillLineRecord> items = new List<BillLineRecord>();
            foreach (DataRow row in itemsTable.Rows)
            {
                items.Add(new BillLineRecord
                {
                    ItemName = row["item_name"]?.ToString() ?? string.Empty,
                    Rate = SafeToDecimal(row["rate"]),
                    Amount = SafeToDecimal(row["amount"]),
                    DiscountedPrice = SafeToDecimal(row["discounted_price"])
                });
            }

            return SaveBillInternal(salesperson, totalAmount, discountAmount, items, clientSubmissionId, DateTime.Now, paymentMethod ?? "CASH", 0).BillCode;
        }

        public static int GetBillIdByCode(string billCode)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("SELECT bill_id FROM bill_history WHERE bill_code = @code LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@code", billCode);
                    object result = cmd.ExecuteScalar();
                    return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
                }
            }
        }

        public static string SaveBill(IList<BillLineRecord> items, string salesperson, decimal totalAmount, decimal discountAmount, string clientSubmissionId, string paymentMethod)
        {
            return SaveBillInternal(salesperson, totalAmount, discountAmount, items, clientSubmissionId, DateTime.Now, paymentMethod ?? "CASH", 0).BillCode;
        }

        public static string SaveBill(IList<BillLineRecord> items, string salesperson, decimal totalAmount, decimal discountAmount,
            string clientSubmissionId, string paymentMethod, int creditAccountId)
        {
            return SaveBillInternal(salesperson, totalAmount, discountAmount, items, clientSubmissionId, DateTime.Now, paymentMethod ?? "CASH", creditAccountId).BillCode;
        }

        public static string SaveBill(IList<BillLineRecord> items, string salesperson, decimal totalAmount, decimal discountAmount,
            string clientSubmissionId, string paymentMethod, int creditAccountId, DateTime occurredAt)
        {
            return SaveBillInternal(salesperson, totalAmount, discountAmount, items, clientSubmissionId, occurredAt, paymentMethod ?? "CASH", creditAccountId).BillCode;
        }

        private static BillSaveResult SaveBillInternal(string salesperson, decimal totalAmount, decimal discountAmount, IList<BillLineRecord> items, string clientSubmissionId, DateTime occurredAt, string paymentMethod, int creditAccountId)
        {
            if (items == null || items.Count == 0)
            {
                throw new InvalidOperationException("Cannot save an empty bill.");
            }

            decimal grandTotal = totalAmount - discountAmount;
            int itemCount = items.Count;

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();

                if (!string.IsNullOrWhiteSpace(clientSubmissionId))
                {
                    string existingBillCode = TryGetExistingBillCode(conn, clientSubmissionId);
                    if (!string.IsNullOrWhiteSpace(existingBillCode))
                    {
                        return new BillSaveResult { BillCode = existingBillCode, ExistingSubmission = true };
                    }
                }

                MySqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    string insertHeader = @"INSERT INTO bill_history
                        (bill_code, date_time, salesperson, total_amount, discount_amount, grand_total, item_count, client_submission_id, payment_method)
                        VALUES ('', @date_time, @salesperson, @total_amount, @discount_amount, @grand_total, @item_count, @client_submission_id, @payment_method)";

                    MySqlCommand cmd = new MySqlCommand(insertHeader, conn, transaction);
                    cmd.Parameters.AddWithValue("@date_time", occurredAt);
                    cmd.Parameters.AddWithValue("@salesperson", salesperson); // salesperson is always employee emp_code, never emp_name.
                    cmd.Parameters.AddWithValue("@total_amount", totalAmount);
                    cmd.Parameters.AddWithValue("@discount_amount", discountAmount);
                    cmd.Parameters.AddWithValue("@grand_total", grandTotal);
                    cmd.Parameters.AddWithValue("@item_count", itemCount);
                    cmd.Parameters.AddWithValue("@client_submission_id", string.IsNullOrWhiteSpace(clientSubmissionId) ? (object)DBNull.Value : clientSubmissionId);
                    cmd.Parameters.AddWithValue("@payment_method", string.IsNullOrWhiteSpace(paymentMethod) ? "CASH" : paymentMethod);
                    cmd.ExecuteNonQuery();

                    long billId = cmd.LastInsertedId;
                    string billCode = "STC-" + billId.ToString("D5");

                    string updateCode = "UPDATE bill_history SET bill_code = @bill_code WHERE bill_id = @bill_id";
                    MySqlCommand updateCmd = new MySqlCommand(updateCode, conn, transaction);
                    updateCmd.Parameters.AddWithValue("@bill_code", billCode);
                    updateCmd.Parameters.AddWithValue("@bill_id", billId);
                    updateCmd.ExecuteNonQuery();

                    if (items.Count > 0)
                    {
                        var itemSql = new System.Text.StringBuilder(
                            "INSERT INTO bill_history_items (bill_id, item_name, rate, amount, discounted_price) VALUES ");
                        MySqlCommand itemCmd = new MySqlCommand(null, conn, transaction);
                        for (int i = 0; i < items.Count; i++)
                        {
                            if (i > 0) itemSql.Append(',');
                            itemSql.Append($"(@bid{i},@nm{i},@rt{i},@am{i},@dp{i})");
                            itemCmd.Parameters.AddWithValue($"@bid{i}", billId);
                            itemCmd.Parameters.AddWithValue($"@nm{i}", items[i].ItemName);
                            itemCmd.Parameters.AddWithValue($"@rt{i}", items[i].Rate);
                            itemCmd.Parameters.AddWithValue($"@am{i}", items[i].Amount);
                            itemCmd.Parameters.AddWithValue($"@dp{i}", items[i].DiscountedPrice);
                        }
                        itemCmd.CommandText = itemSql.ToString();
                        itemCmd.ExecuteNonQuery();
                    }

                    InsertStockMovements(conn, transaction, billId, billCode, occurredAt, items);

                    InsertBillPayment(conn, transaction, billId, billCode, paymentMethod, grandTotal);
                    if (string.Equals(paymentMethod, "CREDIT", StringComparison.OrdinalIgnoreCase) && creditAccountId > 0)
                    {
                        CreditManager.AddTransaction(conn, transaction, creditAccountId, "BILL", grandTotal, "DEBIT",
                            "POS sale", billCode, occurredAt.Date, UserSession.Username ?? "system");
                    }

                    transaction.Commit();
                    RefreshCacheAfterCommit();
                    return new BillSaveResult { BillCode = billCode, BillId = billId };
                }
                catch
                {
                    TryRollback(transaction);
                    if (!string.IsNullOrWhiteSpace(clientSubmissionId))
                    {
                        string existingBillCode = TryGetExistingBillCode(conn, clientSubmissionId);
                        if (!string.IsNullOrWhiteSpace(existingBillCode))
                        {
                            return new BillSaveResult { BillCode = existingBillCode, ExistingSubmission = true };
                        }
                    }

                    throw;
                }
            }
        }

        private static void InsertStockMovements(MySqlConnection conn, MySqlTransaction transaction, long billId, string billCode, DateTime occurredAt, IEnumerable<BillLineRecord> items)
        {
            string createdByUsername = string.IsNullOrWhiteSpace(UserSession.Username) ? "desktop-pos" : UserSession.Username;

            Dictionary<string, int> inventoryByName = AppCache.Inventory
                .GroupBy(inv => inv.ItemName ?? string.Empty, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First().Id, StringComparer.Ordinal);

            var resolved = new List<(int itemId, BillLineRecord item)>();
            foreach (BillLineRecord item in items)
            {
                int cachedItemId;
                int? resolvedItemId = inventoryByName.TryGetValue(item.ItemName ?? string.Empty, out cachedItemId)
                    ? cachedItemId
                    : (int?)null;
                if (resolvedItemId == null)
                {
                    using (MySqlCommand lookupCmd = new MySqlCommand(
                        "SELECT id FROM inventory WHERE item_name = @name LIMIT 1", conn, transaction))
                    {
                        lookupCmd.Parameters.AddWithValue("@name", item.ItemName);
                        object lookupResult = lookupCmd.ExecuteScalar();
                        if (lookupResult != null && lookupResult != DBNull.Value)
                            resolvedItemId = Convert.ToInt32(lookupResult);
                    }
                }

                if (resolvedItemId == null)
                {
                    throw new InvalidOperationException("No inventory match was found for item '" + item.ItemName + "'.");
                }

                resolved.Add((resolvedItemId.Value, item));
            }

            if (resolved.Count == 0) return;

            using (MySqlCommand stockCmd = new MySqlCommand(
                "UPDATE inventory SET amount = COALESCE(amount, 0) + @qty_delta, stock_update_time = NOW() WHERE id = @item_id", conn, transaction))
            using (MySqlCommand movCmd = new MySqlCommand(
                @"INSERT INTO stock_movement
                  (item_id, item_name, movement_type, qty_delta, reference_type, reference_id, occurred_at, created_at, created_by_user_id, created_by_username, note)
                  VALUES
                  (@item_id, @item_name, 'sale', @qty_delta, 'bill_history', @reference_id, @occurred_at, NOW(), NULL, @created_by_username, @note)", conn, transaction))
            {
                stockCmd.Parameters.Add("@qty_delta", MySqlDbType.Decimal);
                stockCmd.Parameters.Add("@item_id", MySqlDbType.Int32);

                movCmd.Parameters.Add("@item_id", MySqlDbType.Int32);
                movCmd.Parameters.Add("@item_name", MySqlDbType.VarChar);
                movCmd.Parameters.Add("@qty_delta", MySqlDbType.Decimal);
                movCmd.Parameters.Add("@reference_id", MySqlDbType.Int64);
                movCmd.Parameters.Add("@occurred_at", MySqlDbType.DateTime);
                movCmd.Parameters.Add("@created_by_username", MySqlDbType.VarChar);
                movCmd.Parameters.Add("@note", MySqlDbType.Text);

                foreach (var row in resolved)
                {
                    decimal qtyDelta = InventoryMutationRules.SaleDelta(row.item.Amount);
                    stockCmd.Parameters["@qty_delta"].Value = qtyDelta;
                    stockCmd.Parameters["@item_id"].Value = row.itemId;
                    if (stockCmd.ExecuteNonQuery() != 1)
                    {
                        throw new InvalidOperationException("Inventory balance update failed for item '" + row.item.ItemName + "'.");
                    }

                    movCmd.Parameters["@item_id"].Value = row.itemId;
                    movCmd.Parameters["@item_name"].Value = row.item.ItemName ?? string.Empty;
                    movCmd.Parameters["@qty_delta"].Value = qtyDelta;
                    movCmd.Parameters["@reference_id"].Value = billId;
                    movCmd.Parameters["@occurred_at"].Value = occurredAt;
                    movCmd.Parameters["@created_by_username"].Value = createdByUsername;
                    movCmd.Parameters["@note"].Value = "Desktop POS sale " + billCode;
                    movCmd.ExecuteNonQuery();
                }
            }
        }

        private static void InsertBillPayment(MySqlConnection conn, MySqlTransaction transaction, long billId, string billCode, string paymentMethod, decimal amount)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                @"INSERT INTO bill_payments
                  (bill_id, bill_code, payment_method, amount, created_at)
                  VALUES (@bill_id, @bill_code, @payment_method, @amount, NOW())", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@bill_id", billId);
                cmd.Parameters.AddWithValue("@bill_code", billCode);
                cmd.Parameters.AddWithValue("@payment_method", string.IsNullOrWhiteSpace(paymentMethod) ? "CASH" : paymentMethod);
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.ExecuteNonQuery();
            }
        }

        private static string TryGetExistingBillCode(MySqlConnection conn, string clientSubmissionId)
        {
            string query = "SELECT bill_id, bill_code FROM bill_history WHERE client_submission_id = @sid LIMIT 1";
            using (MySqlCommand cmd = new MySqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@sid", clientSubmissionId);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    long billId = reader["bill_id"] != DBNull.Value ? Convert.ToInt64(reader["bill_id"]) : 0;
                    string billCode = reader["bill_code"]?.ToString();
                    return string.IsNullOrWhiteSpace(billCode) ? "STC-" + billId.ToString("D5") : billCode;
                }
            }
        }

        private static decimal SafeToDecimal(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return 0m;
            }

            decimal result;
            return decimal.TryParse(value.ToString(), out result) ? result : 0m;
        }

        public static DataTable GetBillHistory()
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT bill_id, bill_code, date_time, salesperson, item_count,
                    grand_total, total_amount, discount_amount, payment_method,
                    COALESCE(status, 'ACTIVE') AS status, voided_at, voided_by, void_action
                    FROM bill_history ORDER BY date_time DESC";

                using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        public static DataTable SearchBillHistory(DateTime fromDate, DateTime toDate, string searchText)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT bh.bill_id, bh.bill_code, bh.date_time, bh.salesperson, bh.item_count,
                    bh.grand_total, bh.total_amount, bh.discount_amount, bh.payment_method,
                    COALESCE(bh.status, 'ACTIVE') AS status, bh.voided_at, bh.voided_by, bh.void_action
                    FROM bill_history bh
                    LEFT JOIN employee e ON e.emp_code = bh.salesperson
                    WHERE bh.date_time BETWEEN @fromDate AND @toDate";

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query += " AND (bh.bill_code LIKE @search OR bh.salesperson LIKE @search OR e.emp_name LIKE @search)";
                }

                query += " ORDER BY bh.date_time DESC";

                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@fromDate", fromDate.Date);
                cmd.Parameters.AddWithValue("@toDate", toDate.Date.AddDays(1).AddSeconds(-1));

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    cmd.Parameters.AddWithValue("@search", "%" + searchText + "%");
                }

                using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        public static DataRow GetBillHeader(int billId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = "SELECT * FROM bill_history WHERE bill_id = @bill_id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@bill_id", billId);

                using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0] : null;
                }
            }
        }

        public static DataTable GetBillItems(int billId)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT id, item_name, rate, amount, discounted_price 
                    FROM bill_history_items WHERE bill_id = @bill_id ORDER BY id";
                MySqlCommand cmd = new MySqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@bill_id", billId);

                using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt;
                }
            }
        }

        public static VoidBillResult VoidBill(int billId, string reason, string action)
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    DataRow header = GetBillHeaderForUpdate(conn, transaction, billId);
                    if (header == null)
                    {
                        throw new InvalidOperationException("Bill not found.");
                    }

                    string status = header.Table.Columns.Contains("status")
                        ? Convert.ToString(header["status"])
                        : "ACTIVE";
                    if (string.Equals(status, "VOIDED", StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException("This bill has already been voided.");
                    }

                    string billCode = Convert.ToString(header["bill_code"]);
                    string paymentMethod = header.Table.Columns.Contains("payment_method")
                        ? Convert.ToString(header["payment_method"])
                        : "CASH";

                    if (HasActiveReturnsForBill(conn, transaction, billId))
                    {
                        throw new InvalidOperationException("This bill has customer returns recorded and cannot be voided.");
                    }

                    int stockReversals = InsertVoidStockReversals(conn, transaction, billId, billCode);
                    CreditVoidInfo creditInfo = ReverseCreditBillIfNeeded(conn, transaction, billCode, paymentMethod);

                    using (MySqlCommand cmd = new MySqlCommand(
                        @"UPDATE bill_history
                          SET status = 'VOIDED',
                              voided_at = NOW(),
                              voided_by = @voided_by,
                              void_reason = @reason,
                              void_action = @action
                          WHERE bill_id = @bill_id", conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@voided_by", string.IsNullOrWhiteSpace(UserSession.Username) ? "desktop-pos" : UserSession.Username);
                        cmd.Parameters.AddWithValue("@reason", string.IsNullOrWhiteSpace(reason) ? (object)DBNull.Value : reason.Trim());
                        cmd.Parameters.AddWithValue("@action", string.IsNullOrWhiteSpace(action) ? "DELETE_ONLY" : action);
                        cmd.Parameters.AddWithValue("@bill_id", billId);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    RefreshCacheAfterCommit();
                    return new VoidBillResult
                    {
                        BillCode = billCode,
                        StockReversalCount = stockReversals,
                        CreditReversalCount = creditInfo.ReversalCount,
                        CreditBillWithoutLedgerMatch = creditInfo.CreditBillWithoutLedgerMatch
                    };
                }
                catch
                {
                    TryRollback(transaction);
                    throw;
                }
            }
        }

        private static DataRow GetBillHeaderForUpdate(MySqlConnection conn, MySqlTransaction transaction, int billId)
        {
            using (MySqlCommand cmd = new MySqlCommand("SELECT * FROM bill_history WHERE bill_id = @bill_id FOR UPDATE", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@bill_id", billId);
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                {
                    DataTable dt = new DataTable();
                    adapter.Fill(dt);
                    return dt.Rows.Count > 0 ? dt.Rows[0] : null;
                }
            }
        }

        private static int InsertVoidStockReversals(MySqlConnection conn, MySqlTransaction transaction, int billId, string billCode)
        {
            DataTable movements = new DataTable();
            using (MySqlCommand cmd = new MySqlCommand(
                @"SELECT item_id, item_name, qty_delta, occurred_at
                  FROM stock_movement
                  WHERE reference_type = 'bill_history'
                    AND reference_id = @bill_id
                    AND movement_type = 'sale'", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@bill_id", billId);
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                {
                    adapter.Fill(movements);
                }
            }

            if (movements.Rows.Count == 0)
            {
                return 0;
            }

            string createdByUsername = string.IsNullOrWhiteSpace(UserSession.Username) ? "desktop-pos" : UserSession.Username;
            using (MySqlCommand stockCmd = new MySqlCommand(
                "UPDATE inventory SET amount = COALESCE(amount, 0) + @qty_delta, stock_update_time = NOW() WHERE id = @item_id", conn, transaction))
            using (MySqlCommand cmd = new MySqlCommand(
                @"INSERT INTO stock_movement
                  (item_id, item_name, movement_type, qty_delta, reference_type, reference_id,
                   occurred_at, created_at, created_by_user_id, created_by_username, note)
                  VALUES
                  (@item_id, @item_name, @movement_type, @qty_delta, 'bill_history_void', @reference_id,
                   NOW(), NOW(), NULL, @created_by_username, @note)", conn, transaction))
            {
                stockCmd.Parameters.Add("@qty_delta", MySqlDbType.Decimal);
                stockCmd.Parameters.Add("@item_id", MySqlDbType.Int32);

                cmd.Parameters.Add("@item_id", MySqlDbType.Int32);
                cmd.Parameters.Add("@item_name", MySqlDbType.VarChar);
                cmd.Parameters.Add("@movement_type", MySqlDbType.VarChar);
                cmd.Parameters.Add("@qty_delta", MySqlDbType.Decimal);
                cmd.Parameters.Add("@reference_id", MySqlDbType.Int32);
                cmd.Parameters.Add("@created_by_username", MySqlDbType.VarChar);
                cmd.Parameters.Add("@note", MySqlDbType.Text);

                foreach (DataRow row in movements.Rows)
                {
                    decimal qtyDelta = InventoryMutationRules.VoidDelta(SafeToDecimal(row["qty_delta"]));
                    int itemId = Convert.ToInt32(row["item_id"]);
                    stockCmd.Parameters["@qty_delta"].Value = qtyDelta;
                    stockCmd.Parameters["@item_id"].Value = itemId;
                    if (stockCmd.ExecuteNonQuery() != 1)
                    {
                        throw new InvalidOperationException("Inventory reversal failed for item '" + Convert.ToString(row["item_name"]) + "'.");
                    }

                    cmd.Parameters["@item_id"].Value = itemId;
                    cmd.Parameters["@item_name"].Value = row["item_name"] == DBNull.Value ? string.Empty : Convert.ToString(row["item_name"]);
                    cmd.Parameters["@movement_type"].Value = InventoryMutationRules.SaleVoid;
                    cmd.Parameters["@qty_delta"].Value = qtyDelta;
                    cmd.Parameters["@reference_id"].Value = billId;
                    cmd.Parameters["@created_by_username"].Value = createdByUsername;
                    cmd.Parameters["@note"].Value = "Voided bill " + billCode;
                    cmd.ExecuteNonQuery();
                }
            }

            return movements.Rows.Count;
        }

        private static bool HasActiveReturnsForBill(MySqlConnection conn, MySqlTransaction transaction, int billId)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM returns WHERE original_bill_id = @bill_id AND status <> 'VOIDED'", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@bill_id", billId);
                object result = cmd.ExecuteScalar();
                return result != null && result != DBNull.Value && Convert.ToInt32(result) > 0;
            }
        }

        private class CreditVoidInfo
        {
            public int ReversalCount { get; set; }
            public bool CreditBillWithoutLedgerMatch { get; set; }
        }

        private static CreditVoidInfo ReverseCreditBillIfNeeded(MySqlConnection conn, MySqlTransaction transaction, string billCode, string paymentMethod)
        {
            CreditVoidInfo info = new CreditVoidInfo();
            if (!string.Equals(paymentMethod, "CREDIT", StringComparison.OrdinalIgnoreCase))
            {
                return info;
            }

            int existingVoidCount;
            using (MySqlCommand cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM credit_transactions WHERE txn_type = 'BILL_VOID' AND bill_code = @bill_code", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@bill_code", billCode);
                existingVoidCount = Convert.ToInt32(cmd.ExecuteScalar());
            }

            if (existingVoidCount > 0)
            {
                return info;
            }

            DataTable creditRows = new DataTable();
            using (MySqlCommand cmd = new MySqlCommand(
                @"SELECT account_id, amount, txn_date
                  FROM credit_transactions
                  WHERE txn_type = 'BILL'
                    AND direction = 'DEBIT'
                    AND bill_code = @bill_code", conn, transaction))
            {
                cmd.Parameters.AddWithValue("@bill_code", billCode);
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                {
                    adapter.Fill(creditRows);
                }
            }

            if (creditRows.Rows.Count == 0)
            {
                info.CreditBillWithoutLedgerMatch = true;
                return info;
            }

            using (MySqlCommand cmd = new MySqlCommand(
                @"INSERT INTO credit_transactions
                  (account_id, txn_type, amount, direction, description, bill_code, txn_date, recorded_by, recorded_at)
                  VALUES
                  (@account_id, 'BILL_VOID', @amount, 'CREDIT', @description, @bill_code, @txn_date, @recorded_by, NOW())", conn, transaction))
            {
                cmd.Parameters.Add("@account_id", MySqlDbType.Int32);
                cmd.Parameters.Add("@amount", MySqlDbType.Decimal);
                cmd.Parameters.Add("@description", MySqlDbType.VarChar);
                cmd.Parameters.Add("@bill_code", MySqlDbType.VarChar);
                cmd.Parameters.Add("@txn_date", MySqlDbType.Date);
                cmd.Parameters.Add("@recorded_by", MySqlDbType.VarChar);

                string recordedBy = string.IsNullOrWhiteSpace(UserSession.Username) ? "desktop-pos" : UserSession.Username;
                foreach (DataRow row in creditRows.Rows)
                {
                    cmd.Parameters["@account_id"].Value = Convert.ToInt32(row["account_id"]);
                    cmd.Parameters["@amount"].Value = SafeToDecimal(row["amount"]);
                    cmd.Parameters["@description"].Value = "Void reversal for bill " + billCode;
                    cmd.Parameters["@bill_code"].Value = billCode;
                    cmd.Parameters["@txn_date"].Value = DateTime.Today;
                    cmd.Parameters["@recorded_by"].Value = recordedBy;
                    cmd.ExecuteNonQuery();
                    info.ReversalCount++;
                }
            }

            return info;
        }

        private static void RefreshCacheAfterCommit()
        {
            try
            {
                AppCache.Refresh();
            }
            catch (Exception ex)
            {
                UpdateLogger.Error("Inventory cache refresh failed after committed bill transaction", ex);
            }
        }

        private static void TryRollback(MySqlTransaction transaction)
        {
            try
            {
                transaction.Rollback();
            }
            catch (Exception ex)
            {
                UpdateLogger.Error("Bill transaction rollback failed or transaction outcome was already decided", ex);
            }
        }
    }
}
