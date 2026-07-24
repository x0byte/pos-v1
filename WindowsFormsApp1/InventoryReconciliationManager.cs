using MySql.Data.MySqlClient;
using System.Data;

namespace WindowsFormsApp1
{
    public static class InventoryReconciliationManager
    {
        public static DataTable BuildReport()
        {
            using (MySqlConnection conn = new MySqlConnection(DatabaseConfig.ConnectionString))
            {
                conn.Open();
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(
                    @"SELECT inv.id,
                             inv.item_name,
                             inv.amount AS current_inventory_amount,
                             COALESCE(SUM(CASE WHEN sm.movement_type = 'customer_return_not_restocked' THEN 0 ELSE sm.qty_delta END), 0) AS movement_derived_amount,
                             inv.amount - COALESCE(SUM(CASE WHEN sm.movement_type = 'customer_return_not_restocked' THEN 0 ELSE sm.qty_delta END), 0) AS difference,
                             inv.stock_update_time AS last_inventory_update,
                             MAX(sm.occurred_at) AS last_movement,
                             MAX(CASE WHEN sm.movement_type = 'sale' THEN sm.occurred_at ELSE NULL END) AS last_sale,
                             MAX(CASE WHEN sm.movement_type IN ('customer_return_restock','customer_return_not_restocked') THEN sm.occurred_at ELSE NULL END) AS last_return,
                             MAX(CASE WHEN sm.movement_type IN ('packaging_consumption','packaging_output','packaging_source') THEN sm.occurred_at ELSE NULL END) AS last_packaging
                      FROM inventory inv
                      LEFT JOIN stock_movement sm ON sm.item_id = inv.id
                      GROUP BY inv.id, inv.item_name, inv.amount, inv.stock_update_time
                      ORDER BY ABS(inv.amount - COALESCE(SUM(CASE WHEN sm.movement_type = 'customer_return_not_restocked' THEN 0 ELSE sm.qty_delta END), 0)) DESC, inv.item_name", conn))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            }
        }
    }
}
