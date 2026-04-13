using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public static class BillHistoryManager
    {
        private static string connectionString => DatabaseConfig.ConnectionString;

        public static string SaveBill(DataGridView dataGridView, string salesperson, decimal totalAmount, decimal discountedAmount)
        {
            decimal grandTotal = totalAmount - discountedAmount;
            int itemCount = 0;

            foreach (DataGridViewRow row in dataGridView.Rows)
            {
                if (!row.IsNewRow) itemCount++;
            }

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                MySqlTransaction transaction = conn.BeginTransaction();

                try
                {
                    string insertHeader = @"INSERT INTO bill_history 
                        (bill_code, date_time, salesperson, total_amount, discount_amount, grand_total, item_count) 
                        VALUES ('', @date_time, @salesperson, @total_amount, @discount_amount, @grand_total, @item_count)";

                    MySqlCommand cmd = new MySqlCommand(insertHeader, conn, transaction);
                    cmd.Parameters.AddWithValue("@date_time", DateTime.Now);
                    cmd.Parameters.AddWithValue("@salesperson", salesperson);
                    cmd.Parameters.AddWithValue("@total_amount", totalAmount);
                    cmd.Parameters.AddWithValue("@discount_amount", discountedAmount);
                    cmd.Parameters.AddWithValue("@grand_total", grandTotal);
                    cmd.Parameters.AddWithValue("@item_count", itemCount);
                    cmd.ExecuteNonQuery();

                    long billId = cmd.LastInsertedId;

                    string billCode = "STC-" + billId.ToString("D5");
                    string updateCode = "UPDATE bill_history SET bill_code = @bill_code WHERE bill_id = @bill_id";
                    MySqlCommand updateCmd = new MySqlCommand(updateCode, conn, transaction);
                    updateCmd.Parameters.AddWithValue("@bill_code", billCode);
                    updateCmd.Parameters.AddWithValue("@bill_id", billId);
                    updateCmd.ExecuteNonQuery();

                    foreach (DataGridViewRow row in dataGridView.Rows)
                    {
                        if (row.IsNewRow) continue;

                        string insertItem = @"INSERT INTO bill_history_items 
                            (bill_id, item_name, rate, amount, discounted_price) 
                            VALUES (@bill_id, @item_name, @rate, @amount, @discounted_price)";

                        MySqlCommand itemCmd = new MySqlCommand(insertItem, conn, transaction);
                        itemCmd.Parameters.AddWithValue("@bill_id", billId);
                        itemCmd.Parameters.AddWithValue("@item_name", row.Cells[1].Value?.ToString() ?? "");
                        itemCmd.Parameters.AddWithValue("@rate", row.Cells[2].Value);
                        itemCmd.Parameters.AddWithValue("@amount", row.Cells[3].Value);
                        itemCmd.Parameters.AddWithValue("@discounted_price", row.Cells[4].Value);
                        itemCmd.ExecuteNonQuery();
                    }

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
                string query = @"SELECT bill_id, bill_code, date_time, salesperson, item_count, 
                    grand_total, total_amount, discount_amount 
                    FROM bill_history 
                    WHERE date_time BETWEEN @fromDate AND @toDate";

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query += " AND (bill_code LIKE @search OR salesperson LIKE @search)";
                }

                query += " ORDER BY date_time DESC";

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
