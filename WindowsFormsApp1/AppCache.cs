using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;

namespace WindowsFormsApp1
{
    public static class AppCache
    {
        public static List<InventoryItem> Inventory { get; private set; } = new List<InventoryItem>();
        public static List<EmployeeItem> Employees { get; private set; } = new List<EmployeeItem>();
        public static DateTime LastSynced { get; private set; }

        public static void Load()
        {
            var inventoryItems = new List<InventoryItem>();
            var employeeItems = new List<EmployeeItem>();

            using (MySqlConnection connection = new MySqlConnection(DatabaseConfig.ConnectionString))
            {
                connection.Open();

                using (MySqlCommand inventoryCommand = new MySqlCommand(
                    "SELECT id, item_name, retail_price, cost, barcode, keywords FROM inventory", connection))
                using (MySqlDataReader reader = inventoryCommand.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var item = new InventoryItem
                        {
                            Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                            ItemName = reader["item_name"]?.ToString(),
                            RetailPrice = reader["retail_price"] != DBNull.Value ? Convert.ToDecimal(reader["retail_price"]) : 0m,
                            Cost = reader["cost"] != DBNull.Value ? (decimal?)Convert.ToDecimal(reader["cost"]) : null,
                            Barcode = reader["barcode"]?.ToString(),
                            Keywords = reader["keywords"]?.ToString()
                        };
                        // Augment in-memory keywords with auto-generated aliases so
                        // existing items benefit without requiring a DB update.
                        item.Keywords = KeywordGenerator.MergeWithGenerated(item.Keywords, item.ItemName);
                        inventoryItems.Add(item);
                    }
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

            Inventory = inventoryItems;
            Employees = employeeItems;
            LastSynced = DateTime.Now;
        }

        public static void Refresh()
        {
            Load();
        }
    }

    public class InventoryItem
    {
        public int Id { get; set; }
        public string ItemName { get; set; }
        public decimal RetailPrice { get; set; }
        public decimal? Cost { get; set; }
        public string Barcode { get; set; }
        public string Keywords { get; set; }
    }

    public class EmployeeItem
    {
        public string EmpCode { get; set; }
        public string EmpName { get; set; }
    }
}
