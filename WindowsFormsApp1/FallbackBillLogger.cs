using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public static class FallbackBillLogger
    {
        private static readonly string CsvPath = Path.Combine(Application.StartupPath, "stc_fallback_bills.csv");

        public static void LogFailedBill(DataGridView dataGridView, string salesperson, decimal totalAmount, decimal discountedAmount)
        {
            try
            {
                string billRef = "LOCAL-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                decimal grandTotal = totalAmount - discountedAmount;
                int itemCount = 0;
                List<string> lines = new List<string>();

                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (!row.IsNewRow)
                    {
                        itemCount++;
                    }
                }

                lines.Add(ToCsvLine(new[]
                {
                    "BILL",
                    billRef,
                    DateTime.Now.ToString("o"),
                    salesperson ?? string.Empty,
                    totalAmount.ToString(CultureInfo.InvariantCulture),
                    discountedAmount.ToString(CultureInfo.InvariantCulture),
                    grandTotal.ToString(CultureInfo.InvariantCulture),
                    itemCount.ToString(CultureInfo.InvariantCulture),
                    "0"
                }));

                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.IsNewRow)
                    {
                        continue;
                    }

                    lines.Add(ToCsvLine(new[]
                    {
                        "ITEM",
                        billRef,
                        row.Cells[1].Value?.ToString() ?? string.Empty,
                        row.Cells[2].Value?.ToString() ?? "0",
                        row.Cells[3].Value?.ToString() ?? "0",
                        row.Cells[4].Value?.ToString() ?? "0",
                        string.Empty,
                        string.Empty,
                        "0"
                    }));
                }

                File.AppendAllLines(CsvPath, lines);
            }
            catch
            {
                // Silent by design for cashier flow.
            }
        }

        public static bool HasUnsyncedBills()
        {
            try
            {
                if (!File.Exists(CsvPath))
                {
                    return false;
                }

                foreach (string line in File.ReadAllLines(CsvPath))
                {
                    string[] row = ParseCsvLine(line);
                    if (row.Length < 9)
                    {
                        continue;
                    }

                    if (string.Equals(row[0], "BILL", StringComparison.OrdinalIgnoreCase) && row[8] != "1")
                    {
                        return true;
                    }
                }
            }
            catch
            {
                // Ignore and treat as no pending.
            }

            return false;
        }

        public static int RetryUnsynced()
        {
            try
            {
                if (!File.Exists(CsvPath))
                {
                    return 0;
                }

                List<string[]> rows = File.ReadAllLines(CsvPath)
                    .Select(ParseCsvLine)
                    .Where(r => r.Length >= 9)
                    .ToList();

                int syncedCount = 0;
                bool modified = false;

                List<string[]> unsyncedBills = rows
                    .Where(r => string.Equals(r[0], "BILL", StringComparison.OrdinalIgnoreCase) && r[8] != "1")
                    .ToList();

                foreach (string[] billRow in unsyncedBills)
                {
                    string billRef = billRow[1];
                    List<string[]> itemRows = rows
                        .Where(r => string.Equals(r[0], "ITEM", StringComparison.OrdinalIgnoreCase) && r[1] == billRef)
                        .ToList();

                    if (TrySyncBill(billRow, itemRows))
                    {
                        foreach (string[] row in rows.Where(r => r[1] == billRef))
                        {
                            row[8] = "1";
                        }

                        syncedCount++;
                        modified = true;
                    }
                }

                if (modified)
                {
                    File.WriteAllLines(CsvPath, rows.Select(ToCsvLine));
                }

                return syncedCount;
            }
            catch
            {
                return 0;
            }
        }

        private static bool TrySyncBill(string[] billRow, List<string[]> itemRows)
        {
            try
            {
                string salesperson = billRow[3];
                decimal totalAmount = ParseDecimal(billRow[4]);
                decimal discountAmount = ParseDecimal(billRow[5]);
                decimal grandTotal = ParseDecimal(billRow[6]);
                int itemCount = ParseInt(billRow[7]);
                DateTime dateTime = ParseDateTime(billRow[2]);

                using (MySqlConnection conn = new MySqlConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();
                    MySqlTransaction transaction = conn.BeginTransaction();
                    try
                    {
                        string insertHeader = @"INSERT INTO bill_history
                        (bill_code, date_time, salesperson, total_amount, discount_amount, grand_total, item_count)
                        VALUES ('', @date_time, @salesperson, @total_amount, @discount_amount, @grand_total, @item_count)";

                        MySqlCommand cmd = new MySqlCommand(insertHeader, conn, transaction);
                        cmd.Parameters.AddWithValue("@date_time", dateTime);
                        cmd.Parameters.AddWithValue("@salesperson", salesperson);
                        cmd.Parameters.AddWithValue("@total_amount", totalAmount);
                        cmd.Parameters.AddWithValue("@discount_amount", discountAmount);
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

                        foreach (string[] itemRow in itemRows)
                        {
                            string insertItem = @"INSERT INTO bill_history_items
                            (bill_id, item_name, rate, amount, discounted_price)
                            VALUES (@bill_id, @item_name, @rate, @amount, @discounted_price)";

                            MySqlCommand itemCmd = new MySqlCommand(insertItem, conn, transaction);
                            itemCmd.Parameters.AddWithValue("@bill_id", billId);
                            itemCmd.Parameters.AddWithValue("@item_name", itemRow[2] ?? string.Empty);
                            itemCmd.Parameters.AddWithValue("@rate", ParseDecimal(itemRow[3]));
                            itemCmd.Parameters.AddWithValue("@amount", ParseDecimal(itemRow[4]));
                            itemCmd.Parameters.AddWithValue("@discounted_price", ParseDecimal(itemRow[5]));
                            itemCmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        return false;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private static decimal ParseDecimal(string value)
        {
            decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result);
            return result;
        }

        private static int ParseInt(string value)
        {
            int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out int result);
            return result;
        }

        private static DateTime ParseDateTime(string value)
        {
            if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime dt))
            {
                return dt;
            }
            return DateTime.Now;
        }

        private static string ToCsvLine(string[] values)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }
                sb.Append(EscapeCsv(values[i] ?? string.Empty));
            }
            return sb.ToString();
        }

        private static string EscapeCsv(string input)
        {
            if (input.Contains("\""))
            {
                input = input.Replace("\"", "\"\"");
            }
            if (input.Contains(",") || input.Contains("\"") || input.Contains("\n") || input.Contains("\r"))
            {
                return "\"" + input + "\"";
            }
            return input;
        }

        private static string[] ParseCsvLine(string line)
        {
            List<string> values = new List<string>();
            StringBuilder current = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            values.Add(current.ToString());
            while (values.Count < 9)
            {
                values.Add(string.Empty);
            }

            return values.ToArray();
        }
    }
}
