using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public static class FallbackBillLogger
    {
        private static readonly string CsvPath = RuntimePathProvider.GetDataFilePath("stc_fallback_bills.csv");
        private static readonly string CsvTempPath = CsvPath + ".tmp";
        private static readonly string CsvBackupPath = CsvPath + ".bak";
        private static readonly Mutex QueueMutex = new Mutex(false, "STC_POS_FallbackBillLogger_Queue");
        private const int MinColumns = 12;

        public static string LogFailedBill(DataGridView dataGridView, string salesperson, decimal totalAmount, decimal discountAmount, string clientSubmissionId)
        {
            return LogFailedBill(dataGridView, salesperson, totalAmount, discountAmount, clientSubmissionId, "CASH");
        }

        public static string LogFailedBill(DataGridView dataGridView, string salesperson, decimal totalAmount, decimal discountAmount, string clientSubmissionId, string paymentMethod)
        {
            return LogFailedBill(dataGridView, salesperson, totalAmount, discountAmount, clientSubmissionId, paymentMethod, 0);
        }

        public static string LogFailedBill(DataGridView dataGridView, string salesperson, decimal totalAmount, decimal discountAmount, string clientSubmissionId, string paymentMethod, int creditAccountId)
        {
            string billRef = GenerateLocalReference();
            string idempotencyId = string.IsNullOrWhiteSpace(clientSubmissionId)
                ? "fallback-sale-" + Guid.NewGuid().ToString("N")
                : clientSubmissionId;
            try
            {
                decimal grandTotal = totalAmount - discountAmount;
                int itemCount = 0;
                DateTime occurredAt = DateTime.Now;
                string createdByUsername = string.IsNullOrWhiteSpace(UserSession.Username) ? "desktop-pos" : UserSession.Username;
                List<string> lines = new List<string>();
                HashSet<string> knownInventoryNames = new HashSet<string>(
                    AppCache.Inventory.Select(inv => inv.ItemName ?? string.Empty),
                    StringComparer.Ordinal);

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
                    idempotencyId,
                    string.IsNullOrWhiteSpace(paymentMethod) ? "CASH" : paymentMethod,
                    creditAccountId.ToString(CultureInfo.InvariantCulture)
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

                    if (!knownInventoryNames.Contains(itemName))
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

                if (!TryEnterQueueMutex(5000))
                {
                    return billRef;
                }

                try
                {
                    File.AppendAllLines(CsvPath, lines);
                }
                finally
                {
                    ExitQueueMutex();
                }
            }
            catch
            {
                // Silent by design for cashier flow.
            }

            return billRef;
        }

        public static void LogStockMovementSkipped(string billReference, string itemName, string reason)
        {
            try
            {
                if (!TryEnterQueueMutex(5000))
                {
                    return;
                }

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
                finally
                {
                    ExitQueueMutex();
                }
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

                foreach (string line in File.ReadLines(CsvPath))
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

                if (info.Length > 10 * 1024 * 1024)
                {
                    return true;
                }

                long lineCount = 0;
                foreach (string ignored in File.ReadLines(CsvPath))
                {
                    lineCount++;
                    if (lineCount > 50000)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public static int RetryUnsynced()
        {
            if (!TryEnterQueueMutex(0))
            {
                return 0;
            }

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
                Dictionary<string, List<string[]>> rowsByReference = rows
                    .Where(r => r.Length > 1)
                    .GroupBy(r => r[1] ?? string.Empty, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

                List<string[]> unsyncedBills = rows
                    .Where(r => string.Equals(r[0], "BILL", StringComparison.OrdinalIgnoreCase) && r[8] != "1")
                    .ToList();

                foreach (string[] billRow in unsyncedBills)
                {
                    string billRef = billRow[1];
                    List<string[]> relatedRows;
                    if (!rowsByReference.TryGetValue(billRef, out relatedRows))
                    {
                        relatedRows = new List<string[]>();
                    }

                    List<string[]> itemRows = relatedRows
                        .Where(r => string.Equals(r[0], "ITEM", StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    if (TrySyncBill(billRow, itemRows))
                    {
                        foreach (string[] row in relatedRows)
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
            finally
            {
                ExitQueueMutex();
            }
        }

        private static bool TrySyncBill(string[] billRow, List<string[]> itemRows)
        {
            try
            {
                string salesperson = billRow[3];
                decimal totalAmount = ParseDecimal(billRow[4]);
                decimal discountAmount = ParseDecimal(billRow[5]);
                DateTime dateTime = ParseDateTime(billRow[2]);
                string clientSubmissionId = billRow.Length > 9 && !string.IsNullOrWhiteSpace(billRow[9])
                    ? billRow[9]
                    : billRow[1];
                string paymentMethod = billRow.Length > 10 && !string.IsNullOrWhiteSpace(billRow[10])
                    ? billRow[10]
                    : "CASH";
                int creditAccountId = billRow.Length > 11 ? ParseInt(billRow[11]) : 0;

                List<BillLineRecord> items = itemRows.Select(row => new BillLineRecord
                {
                    ItemName = row[2] ?? string.Empty,
                    Rate = ParseDecimal(row[3]),
                    Amount = ParseDecimal(row[4]),
                    DiscountedPrice = ParseDecimal(row[5])
                }).ToList();

                BillHistoryManager.SaveBill(items, salesperson, totalAmount, discountAmount, clientSubmissionId, paymentMethod, creditAccountId, dateTime);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool ExistingSubmissionExists(MySqlConnection conn, string clientSubmissionId)
        {
            string existingBillQuery = "SELECT bill_id FROM bill_history WHERE client_submission_id = @sid LIMIT 1";
            using (MySqlCommand existingCmd = new MySqlCommand(existingBillQuery, conn))
            {
                existingCmd.Parameters.AddWithValue("@sid", clientSubmissionId);
                object existing = existingCmd.ExecuteScalar();
                return existing != null && existing != DBNull.Value;
            }
        }

        private static string GenerateLocalReference()
        {
            return "LOCAL-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
        }

        private static void InsertBillItems(MySqlConnection conn, MySqlTransaction transaction, long billId, List<string[]> itemRows)
        {
            if (itemRows.Count == 0)
            {
                return;
            }

            StringBuilder sql = new StringBuilder(
                @"INSERT INTO bill_history_items
                (bill_id, item_name, rate, amount, discounted_price)
                VALUES ");
            MySqlCommand itemCmd = new MySqlCommand(null, conn, transaction);

            for (int i = 0; i < itemRows.Count; i++)
            {
                if (i > 0)
                {
                    sql.Append(',');
                }

                string[] itemRow = itemRows[i];
                sql.Append($"(@bill_id{i}, @item_name{i}, @rate{i}, @amount{i}, @discounted_price{i})");
                itemCmd.Parameters.AddWithValue($"@bill_id{i}", billId);
                itemCmd.Parameters.AddWithValue($"@item_name{i}", itemRow[2] ?? string.Empty);
                itemCmd.Parameters.AddWithValue($"@rate{i}", ParseDecimal(itemRow[3]));
                itemCmd.Parameters.AddWithValue($"@amount{i}", ParseDecimal(itemRow[4]));
                itemCmd.Parameters.AddWithValue($"@discounted_price{i}", ParseDecimal(itemRow[5]));
            }

            itemCmd.CommandText = sql.ToString();
            itemCmd.ExecuteNonQuery();
        }

        private static void InsertStockMovements(MySqlConnection conn, MySqlTransaction transaction, long billId, List<string[]> movementRows, Dictionary<string, int> inventoryByName)
        {
            if (movementRows.Count == 0)
            {
                return;
            }

            var resolved = new List<(int itemId, string[] row)>();
            foreach (string[] movementRow in movementRows)
            {
                string itemName = movementRow[2] ?? string.Empty;
                int cachedItemId;
                int? resolvedItemId = inventoryByName.TryGetValue(itemName, out cachedItemId)
                    ? cachedItemId
                    : (int?)null;

                if (resolvedItemId == null)
                {
                    using (MySqlCommand lookupCmd = new MySqlCommand(
                        "SELECT id FROM inventory WHERE item_name = @name LIMIT 1", conn, transaction))
                    {
                        lookupCmd.Parameters.AddWithValue("@name", itemName);
                        object lookupResult = lookupCmd.ExecuteScalar();
                        if (lookupResult != null && lookupResult != DBNull.Value)
                        {
                            resolvedItemId = Convert.ToInt32(lookupResult);
                            inventoryByName[itemName] = resolvedItemId.Value;
                        }
                    }
                }

                if (resolvedItemId != null)
                {
                    resolved.Add((resolvedItemId.Value, movementRow));
                }
            }

            if (resolved.Count == 0)
            {
                return;
            }

            StringBuilder sql = new StringBuilder(
                @"INSERT INTO stock_movement
                (item_id, item_name, movement_type, qty_delta, reference_type, reference_id, occurred_at, created_at, created_by_user_id, created_by_username, note)
                VALUES ");
            MySqlCommand movementCmd = new MySqlCommand(null, conn, transaction);

            for (int i = 0; i < resolved.Count; i++)
            {
                if (i > 0)
                {
                    sql.Append(',');
                }

                string[] movementRow = resolved[i].row;
                sql.Append($"(@item_id{i}, @item_name{i}, @movement_type{i}, @qty_delta{i}, 'bill_history', @reference_id{i}, @occurred_at{i}, NOW(), NULL, @created_by_username{i}, @note{i})");
                movementCmd.Parameters.AddWithValue($"@item_id{i}", resolved[i].itemId);
                movementCmd.Parameters.AddWithValue($"@item_name{i}", movementRow[2] ?? string.Empty);
                movementCmd.Parameters.AddWithValue($"@movement_type{i}", movementRow[4] ?? "sale");
                movementCmd.Parameters.AddWithValue($"@qty_delta{i}", ParseDecimal(movementRow[3]));
                movementCmd.Parameters.AddWithValue($"@reference_id{i}", billId);
                movementCmd.Parameters.AddWithValue($"@occurred_at{i}", ParseDateTime(movementRow[5]));
                movementCmd.Parameters.AddWithValue($"@created_by_username{i}", movementRow[6] ?? "desktop-pos");
                movementCmd.Parameters.AddWithValue($"@note{i}", movementRow[7] ?? string.Empty);
            }

            movementCmd.CommandText = sql.ToString();
            movementCmd.ExecuteNonQuery();
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

        private static bool TryEnterQueueMutex(int millisecondsTimeout)
        {
            try
            {
                return QueueMutex.WaitOne(millisecondsTimeout);
            }
            catch (AbandonedMutexException)
            {
                return true;
            }
        }

        private static void ExitQueueMutex()
        {
            try
            {
                QueueMutex.ReleaseMutex();
            }
            catch
            {
                // Best-effort cleanup only.
            }
        }
    }
}
