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

        public static string SaveBill(DataGridView dataGridView, string salesperson, decimal totalAmount, decimal discountAmount)
        {
            return SaveBill(dataGridView, salesperson, totalAmount, discountAmount, null);
        }

        public static string SaveBill(DataGridView dataGridView, string salesperson, decimal totalAmount, decimal discountAmount, string clientSubmissionId)
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

            return SaveBillInternal(salesperson, totalAmount, discountAmount, items, clientSubmissionId, DateTime.Now);
        }

        public static string SaveBillFromDataTable(string salesperson, decimal totalAmount, decimal discountAmount, DataTable itemsTable, string clientSubmissionId = null)
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

            return SaveBillInternal(salesperson, totalAmount, discountAmount, items, clientSubmissionId, DateTime.Now);
        }

        private static string SaveBillInternal(string salesperson, decimal totalAmount, decimal discountAmount, List<BillLineRecord> items, string clientSubmissionId, DateTime occurredAt)
        {
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
                        return existingBillCode;
                    }
                }

                MySqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    string insertHeader = @"INSERT INTO bill_history 
                        (bill_code, date_time, salesperson, total_amount, discount_amount, grand_total, item_count, client_submission_id) 
                        VALUES ('', @date_time, @salesperson, @total_amount, @discount_amount, @grand_total, @item_count, @client_submission_id)";

                    MySqlCommand cmd = new MySqlCommand(insertHeader, conn, transaction);
                    cmd.Parameters.AddWithValue("@date_time", occurredAt);
                    cmd.Parameters.AddWithValue("@salesperson", salesperson); // salesperson is always employee emp_code, never emp_name.
                    cmd.Parameters.AddWithValue("@total_amount", totalAmount);
                    cmd.Parameters.AddWithValue("@discount_amount", discountAmount);
                    cmd.Parameters.AddWithValue("@grand_total", grandTotal);
                    cmd.Parameters.AddWithValue("@item_count", itemCount);
                    cmd.Parameters.AddWithValue("@client_submission_id", string.IsNullOrWhiteSpace(clientSubmissionId) ? (object)DBNull.Value : clientSubmissionId);
                    cmd.ExecuteNonQuery();

                    long billId = cmd.LastInsertedId;
                    string billCode = "STC-" + billId.ToString("D5");

                    string updateCode = "UPDATE bill_history SET bill_code = @bill_code WHERE bill_id = @bill_id";
                    MySqlCommand updateCmd = new MySqlCommand(updateCode, conn, transaction);
                    updateCmd.Parameters.AddWithValue("@bill_code", billCode);
                    updateCmd.Parameters.AddWithValue("@bill_id", billId);
                    updateCmd.ExecuteNonQuery();

                    foreach (BillLineRecord item in items)
                    {
                        string insertItem = @"INSERT INTO bill_history_items 
                            (bill_id, item_name, rate, amount, discounted_price) 
                            VALUES (@bill_id, @item_name, @rate, @amount, @discounted_price)";

                        MySqlCommand itemCmd = new MySqlCommand(insertItem, conn, transaction);
                        itemCmd.Parameters.AddWithValue("@bill_id", billId);
                        itemCmd.Parameters.AddWithValue("@item_name", item.ItemName);
                        itemCmd.Parameters.AddWithValue("@rate", item.Rate);
                        itemCmd.Parameters.AddWithValue("@amount", item.Amount);
                        itemCmd.Parameters.AddWithValue("@discounted_price", item.DiscountedPrice);
                        itemCmd.ExecuteNonQuery();
                    }

                    InsertStockMovements(conn, transaction, billId, billCode, occurredAt, items);

                    transaction.Commit();
                    return billCode;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private static void InsertStockMovements(MySqlConnection conn, MySqlTransaction transaction, long billId, string billCode, DateTime occurredAt, IEnumerable<BillLineRecord> items)
        {
            string createdByUsername = string.IsNullOrWhiteSpace(UserSession.Username) ? "desktop-pos" : UserSession.Username;

            foreach (BillLineRecord item in items)
            {
                InventoryItem cached = AppCache.Inventory.FirstOrDefault(inv =>
                    string.Equals(inv.ItemName, item.ItemName, StringComparison.Ordinal));

                int? resolvedItemId = cached?.Id;
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
                    FallbackBillLogger.LogStockMovementSkipped(billCode, item.ItemName, "No inventory.item_name match (cache or DB).");
                    continue;
                }

                string insertMovement = @"INSERT INTO stock_movement
                    (item_id, item_name, movement_type, qty_delta, reference_type, reference_id, occurred_at, created_at, created_by_user_id, created_by_username, note)
                    VALUES
                    (@item_id, @item_name, 'sale', @qty_delta, 'bill_history', @reference_id, @occurred_at, NOW(), NULL, @created_by_username, @note)";

                MySqlCommand movementCmd = new MySqlCommand(insertMovement, conn, transaction);
                movementCmd.Parameters.AddWithValue("@item_id", resolvedItemId.Value);
                movementCmd.Parameters.AddWithValue("@item_name", item.ItemName);
                movementCmd.Parameters.AddWithValue("@qty_delta", -item.Amount);
                movementCmd.Parameters.AddWithValue("@reference_id", billId);
                movementCmd.Parameters.AddWithValue("@occurred_at", occurredAt);
                movementCmd.Parameters.AddWithValue("@created_by_username", createdByUsername);
                movementCmd.Parameters.AddWithValue("@note", "Desktop POS sale");
                movementCmd.ExecuteNonQuery();
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
                    grand_total, total_amount, discount_amount 
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
                    bh.grand_total, bh.total_amount, bh.discount_amount
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
    }
}
