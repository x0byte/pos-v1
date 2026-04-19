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
        private static readonly string CsvTempPath = CsvPath + ".tmp";
        private static readonly string CsvBackupPath = CsvPath + ".bak";
        private const int MinColumns = 9;

        public static void LogFailedBill(DataGridView dataGridView, string salesperson, decimal totalAmount, decimal discountAmount, string clientSubmissionId)
        {
            try
            {
                string billRef = "LOCAL-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                decimal grandTotal = totalAmount - discountAmount;
                int itemCount = 0;
                DateTime occurredAt = DateTime.Now;
                string createdByUsername = string.IsNullOrWhiteSpace(UserSession.Username) ? "desktop-pos" : UserSession.Username;
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
                    occurredAt.ToString("o"),
                    salesperson ?? string.Empty, // salesperson is always employee emp_code, never emp_name.
                    totalAmount.ToString(CultureInfo.InvariantCulture),
                    discountAmount.ToString(CultureInfo.InvariantCulture),
                    grandTotal.ToString(CultureInfo.InvariantCulture),
                    itemCount.ToString(CultureInfo.InvariantCulture),
                    "0",
                    clientSubmissionId ?? string.Empty
                }));

                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.IsNewRow)
                    {
                        continue;
                    }

                    string itemName = row.Cells[1].Value?.ToString() ?? string.Empty;
                    decimal amount = ParseDecimal(row.Cells[3].Value?.ToString());

                    lines.Add(ToCsvLine(new[]
                    {
                        "ITEM",
                        billRef,
                        itemName,
                        row.Cells[2].Value?.ToString() ?? "0",
                        row.Cells[3].Value?.ToString() ?? "0",
                        row.Cells[4].Value?.ToString() ?? "0",
                        string.Empty,
                        string.Empty,
                        "0"
                    }));

                    InventoryItem inventoryItem = AppCache.Inventory.FirstOrDefault(inv =>
                        string.Equals(inv.ItemName, itemName, StringComparison.Ordinal));

                    if (inventoryItem == null)
                    {
                        lines.Add(ToCsvLine(new[]
                        {
                            "STOCK_MOVEMENT_SKIPPED",
                            billRef,
                            itemName,
                            "No exact inventory.item_name match was found.",
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            string.Empty,
                            "0"
                        }));
                        continue;
                    }

                    lines.Add(ToCsvLine(new[]
                    {
                        "MOVEMENT",
                        billRef,
                        itemName,
                        (-amount).ToString(CultureInfo.InvariantCulture),
                        "sale",
                        occurredAt.ToString("o"),
                        createdByUsername,
                        "Desktop POS sale",
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

        public static void LogStockMovementSkipped(string billReference, string itemName, string reason)
        {
            try
            {
                File.AppendAllLines(CsvPath, new[]
                {
                    ToCsvLine(new[]
                    {
                        "STOCK_MOVEMENT_SKIPPED",
                        billReference ?? string.Empty,
                        itemName ?? string.Empty,
                        reason ?? string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        string.Empty,
                        "1"
                    })
                });
            }
            catch
            {
                // Local warning only.
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

        public static bool IsQueueLarge()
        {
            try
            {
                FileInfo info = new FileInfo(CsvPath);
                if (!info.Exists)
                {
                    return false;
                }

                long lineCount = File.ReadLines(CsvPath).LongCount();
                return info.Length > 10 * 1024 * 1024 || lineCount > 50000;
            }
            catch
            {
                return false;
            }
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
                    .ToList();

                foreach (string[] row in rows)
                {
                    string rowType = row[0] ?? string.Empty;
                    if (!string.Equals(rowType, "BILL", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(rowType, "ITEM", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(rowType, "MOVEMENT", StringComparison.OrdinalIgnoreCase)
                        && !string.Equals(rowType, "STOCK_MOVEMENT_SKIPPED", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("WARNING: Unknown fallback CSV row type skipped: " + rowType);
                    }
                }

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
                    List<string[]> movementRows = rows
                        .Where(r => string.Equals(r[0], "MOVEMENT", StringComparison.OrdinalIgnoreCase) && r[1] == billRef)
                        .ToList();

                    if (TrySyncBill(billRow, itemRows, movementRows))
                    {
                        foreach (string[] row in rows.Where(r => r[1] == billRef))
                        {
                            EnsureLength(row, MinColumns);
                            row[8] = "1";
                        }

                        syncedCount++;
                        modified = true;
                    }
                }

                if (modified)
                {
                    AtomicRewrite(rows.Select(ToCsvLine).ToList());
                }

                return syncedCount;
            }
            catch
            {
                return 0;
            }
        }

        private static bool TrySyncBill(string[] billRow, List<string[]> itemRows, List<string[]> movementRows)
        {
            try
            {
                string salesperson = billRow[3];
                decimal totalAmount = ParseDecimal(billRow[4]);
                decimal discountAmount = ParseDecimal(billRow[5]);
                decimal grandTotal = ParseDecimal(billRow[6]);
                int itemCount = ParseInt(billRow[7]);
                DateTime dateTime = ParseDateTime(billRow[2]);
                string clientSubmissionId = billRow.Length > 9 ? billRow[9] : null;

                using (MySqlConnection conn = new MySqlConnection(DatabaseConfig.ConnectionString))
                {
                    conn.Open();

                    if (!string.IsNullOrWhiteSpace(clientSubmissionId))
                    {
                        string existingBillQuery = "SELECT bill_id FROM bill_history WHERE client_submission_id = @sid LIMIT 1";
                        using (MySqlCommand existingCmd = new MySqlCommand(existingBillQuery, conn))
                        {
                            existingCmd.Parameters.AddWithValue("@sid", clientSubmissionId);
                            object existing = existingCmd.ExecuteScalar();
                            if (existing != null && existing != DBNull.Value)
                            {
                                return true;
                            }
                        }
                    }

                    MySqlTransaction transaction = conn.BeginTransaction();
                    try
                    {
                        string insertHeader = @"INSERT INTO bill_history
                        (bill_code, date_time, salesperson, total_amount, discount_amount, grand_total, item_count, client_submission_id)
                        VALUES ('', @date_time, @salesperson, @total_amount, @discount_amount, @grand_total, @item_count, @client_submission_id)";

                        MySqlCommand cmd = new MySqlCommand(insertHeader, conn, transaction);
                        cmd.Parameters.AddWithValue("@date_time", dateTime);
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

                        foreach (string[] movementRow in movementRows)
                        {
                            InventoryItem cached = AppCache.Inventory.FirstOrDefault(inv =>
                                string.Equals(inv.ItemName, movementRow[2], StringComparison.Ordinal));

                            int? resolvedItemId = cached?.Id;
                            if (resolvedItemId == null)
                            {
                                using (MySqlCommand lookupCmd = new MySqlCommand(
                                    "SELECT id FROM inventory WHERE item_name = @name LIMIT 1", conn, transaction))
                                {
                                    lookupCmd.Parameters.AddWithValue("@name", movementRow[2] ?? string.Empty);
                                    object lookupResult = lookupCmd.ExecuteScalar();
                                    if (lookupResult != null && lookupResult != DBNull.Value)
                                        resolvedItemId = Convert.ToInt32(lookupResult);
                                }
                            }

                            if (resolvedItemId == null) continue;

                            string insertMovement = @"INSERT INTO stock_movement
                                (item_id, item_name, movement_type, qty_delta, reference_type, reference_id, occurred_at, created_at, created_by_user_id, created_by_username, note)
                                VALUES
                                (@item_id, @item_name, @movement_type, @qty_delta, 'bill_history', @reference_id, @occurred_at, NOW(), NULL, @created_by_username, @note)";

                            MySqlCommand movementCmd = new MySqlCommand(insertMovement, conn, transaction);
                            movementCmd.Parameters.AddWithValue("@item_id", resolvedItemId.Value);
                            movementCmd.Parameters.AddWithValue("@item_name", movementRow[2] ?? string.Empty);
                            movementCmd.Parameters.AddWithValue("@movement_type", movementRow[4] ?? "sale");
                            movementCmd.Parameters.AddWithValue("@qty_delta", ParseDecimal(movementRow[3]));
                            movementCmd.Parameters.AddWithValue("@reference_id", billId);
                            movementCmd.Parameters.AddWithValue("@occurred_at", ParseDateTime(movementRow[5]));
                            movementCmd.Parameters.AddWithValue("@created_by_username", movementRow[6] ?? "desktop-pos");
                            movementCmd.Parameters.AddWithValue("@note", movementRow[7] ?? string.Empty);
                            movementCmd.ExecuteNonQuery();
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

        private static void AtomicRewrite(List<string> lines)
        {
            File.WriteAllLines(CsvTempPath, lines);

            if (File.Exists(CsvPath))
            {
                File.Replace(CsvTempPath, CsvPath, CsvBackupPath, true);
            }
            else
            {
                File.Move(CsvTempPath, CsvPath);
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
            string value = input ?? string.Empty;
            if (value.Contains("\""))
            {
                value = value.Replace("\"", "\"\"");
            }
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n") || value.Contains("\r"))
            {
                return "\"" + value + "\"";
            }
            return value;
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
            while (values.Count < MinColumns)
            {
                values.Add(string.Empty);
            }

            return values.ToArray();
        }

        private static void EnsureLength(string[] row, int minLength)
        {
            if (row.Length >= minLength)
            {
                return;
            }

            throw new InvalidOperationException("Row length was shorter than expected.");
        }
    }
}
