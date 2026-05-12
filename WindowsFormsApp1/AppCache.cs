using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace WindowsFormsApp1
{
    public static class AppCache
    {
        public static List<InventoryItem> Inventory { get; private set; } = new List<InventoryItem>();
        public static DataTable InventoryTable { get; private set; } = new DataTable();
        public static List<EmployeeItem> Employees { get; private set; } = new List<EmployeeItem>();
        public static Dictionary<string, InventoryItem> InventoryByName { get; private set; } =
            new Dictionary<string, InventoryItem>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, InventoryItem> InventoryByBarcode { get; private set; } =
            new Dictionary<string, InventoryItem>(StringComparer.OrdinalIgnoreCase);
        public static DateTime LastSynced { get; private set; }
        public static DateTime LastInventoryChangedAt { get; private set; }
        public static DateTime LastInventoryCheckedAt { get; private set; }
        private static int lastInventoryRowCount;

        public static void Load()
        {
            DataTable inventoryTable = new DataTable();
            var inventoryItems = new List<InventoryItem>();
            var employeeItems = new List<EmployeeItem>();
            DateTime inventoryChangedAt = DateTime.MinValue;

            using (MySqlConnection connection = new MySqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();

                using (MySqlDataAdapter adapter = new MySqlDataAdapter("SELECT * FROM inventory", connection))
                {
                    adapter.Fill(inventoryTable);
                }

                foreach (DataRow row in inventoryTable.Rows)
                {
                    var item = new InventoryItem
                    {
                        Id = GetInt(row, "id"),
                        ItemName = GetString(row, "item_name"),
                        RetailPrice = GetDecimal(row, "retail_price") ?? 0m,
                        Amount = GetDecimal(row, "amount"),
                        AddedBy = GetString(row, "added_by"),
                        Cost = GetDecimal(row, "cost"),
                        Barcode = GetString(row, "barcode"),
                        Keywords = GetString(row, "keywords")
                    };
                    // Augment in-memory keywords with auto-generated aliases so
                    // existing items benefit without requiring a DB update.
                    item.Keywords = KeywordGenerator.MergeWithGenerated(item.Keywords, item.ItemName);
                    item.RefreshSearchFields();
                    inventoryItems.Add(item);

                    DateTime? rowUpdatedAt = GetDateTime(row, "stock_update_time") ?? GetDateTime(row, "updated_at");
                    if (rowUpdatedAt.HasValue && rowUpdatedAt.Value > inventoryChangedAt)
                    {
                        inventoryChangedAt = rowUpdatedAt.Value;
                    }
                }

                if (inventoryItems.Count == 0)
                {
                    Console.WriteLine("WARNING: inventory query succeeded but returned zero rows.");
                }

                try
                {
                    using (MySqlCommand employeeCommand = new MySqlCommand("SELECT emp_code, emp_name FROM employee", connection))
                    using (MySqlDataReader reader = employeeCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string empCode = reader["emp_code"]?.ToString();
                            string empName = reader["emp_name"]?.ToString();
                            employeeItems.Add(new EmployeeItem
                            {
                                EmpCode = empCode,
                                EmpName = string.IsNullOrWhiteSpace(empName) ? empCode : empName
                            });
                        }
                    }
                }
                catch
                {
                    using (MySqlCommand employeeCommand = new MySqlCommand("SELECT emp_code FROM employee", connection))
                    using (MySqlDataReader reader = employeeCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string empCode = reader["emp_code"]?.ToString();
                            employeeItems.Add(new EmployeeItem
                            {
                                EmpCode = empCode,
                                EmpName = empCode
                            });
                        }
                    }
                }
            }

            InventoryTable = inventoryTable;
            Inventory = inventoryItems;
            Employees = employeeItems;
            InventoryByName = inventoryItems
                .Where(item => !string.IsNullOrWhiteSpace(item.ItemName))
                .GroupBy(item => item.ItemName.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            InventoryByBarcode = inventoryItems
                .Where(item => !string.IsNullOrWhiteSpace(item.Barcode))
                .GroupBy(item => item.Barcode.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            lastInventoryRowCount = inventoryItems.Count;
            LastInventoryChangedAt = inventoryChangedAt;
            LastSynced = DateTime.Now;
        }

        public static void Refresh()
        {
            Load();
        }

        public static DataTable GetInventoryDataTable()
        {
            return InventoryTable.Copy();
        }

        public static bool RefreshIfInventoryChanged()
        {
            DateTime latestInventoryChangedAt = DateTime.MinValue;
            int currentInventoryRowCount = 0;

            using (MySqlConnection connection = new MySqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();
                using (MySqlCommand command = new MySqlCommand(
                    "SELECT COUNT(*) AS row_count, MAX(stock_update_time) AS latest_updated_at FROM inventory", connection))
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        currentInventoryRowCount = reader["row_count"] != DBNull.Value ? Convert.ToInt32(reader["row_count"]) : 0;
                        latestInventoryChangedAt = reader["latest_updated_at"] != DBNull.Value
                            ? Convert.ToDateTime(reader["latest_updated_at"])
                            : DateTime.MinValue;
                    }
                }
            }

            if (currentInventoryRowCount == lastInventoryRowCount &&
                latestInventoryChangedAt <= LastInventoryChangedAt)
            {
                LastInventoryCheckedAt = DateTime.Now;
                return false;
            }

            Refresh();
            LastInventoryCheckedAt = DateTime.Now;
            return true;
        }

        public static bool RefreshIfInventoryChanged(TimeSpan minimumCheckInterval)
        {
            if (LastInventoryCheckedAt != DateTime.MinValue &&
                DateTime.Now - LastInventoryCheckedAt < minimumCheckInterval)
            {
                return false;
            }

            return RefreshIfInventoryChanged();
        }

        private static bool HasColumn(DataRow row, string columnName)
        {
            return row.Table.Columns.Contains(columnName);
        }

        private static string GetString(DataRow row, string columnName)
        {
            return HasColumn(row, columnName) && row[columnName] != DBNull.Value
                ? row[columnName]?.ToString()
                : null;
        }

        private static int GetInt(DataRow row, string columnName)
        {
            return HasColumn(row, columnName) && row[columnName] != DBNull.Value
                ? Convert.ToInt32(row[columnName])
                : 0;
        }

        private static decimal? GetDecimal(DataRow row, string columnName)
        {
            return HasColumn(row, columnName) && row[columnName] != DBNull.Value
                ? (decimal?)Convert.ToDecimal(row[columnName])
                : null;
        }

        private static DateTime? GetDateTime(DataRow row, string columnName)
        {
            return HasColumn(row, columnName) && row[columnName] != DBNull.Value
                ? (DateTime?)Convert.ToDateTime(row[columnName])
                : null;
        }
    }

    public class InventoryItem
    {
        public int Id { get; set; }
        public string ItemName { get; set; }
        public decimal RetailPrice { get; set; }
        public decimal? Amount { get; set; }
        public string AddedBy { get; set; }
        public decimal? Cost { get; set; }
        public string Barcode { get; set; }
        public string Keywords { get; set; }
        public string NormalizedItemName { get; private set; }
        public string CompactNormalizedItemName { get; private set; }
        public string NormalizedKeywords { get; private set; }

        public void RefreshSearchFields()
        {
            NormalizedItemName = (ItemName ?? string.Empty).ToLowerInvariant();
            CompactNormalizedItemName = NormalizedItemName.Replace(" ", string.Empty);
            NormalizedKeywords = (Keywords ?? string.Empty).ToLowerInvariant();
        }
    }

    public class EmployeeItem
    {
        public string EmpCode { get; set; }
        public string EmpName { get; set; }
    }
}
