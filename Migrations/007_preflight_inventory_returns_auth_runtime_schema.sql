-- Run before 007_inventory_returns_auth_runtime_schema.sql on production.
-- This reports incompatible conditions that migration 007 refuses to repair automatically.

SELECT 'duplicate bill_history.client_submission_id' AS check_name,
       client_submission_id AS offending_value,
       COUNT(*) AS row_count
FROM bill_history
WHERE client_submission_id IS NOT NULL
  AND client_submission_id <> ''
GROUP BY client_submission_id
HAVING COUNT(*) > 1;

SELECT 'unknown stock_movement.movement_type' AS check_name,
       movement_type AS offending_value,
       COUNT(*) AS row_count
FROM stock_movement
WHERE movement_type NOT IN (
    'purchase','sale','count_adjustment','manual_adjustment','return','wastage','void_reversal',
    'packaging_source','packaging_output','packaging_consumption',
    'customer_return_restock','customer_return_not_restocked',
    'purchase_receipt','manual_adjustment_in','manual_adjustment_out','stocktake_correction'
)
GROUP BY movement_type;

SELECT 'missing expected table' AS check_name,
       expected.table_name AS offending_value,
       0 AS row_count
FROM (
    SELECT 'inventory' AS table_name UNION ALL
    SELECT 'bill_history' UNION ALL
    SELECT 'bill_history_items' UNION ALL
    SELECT 'stock_movement' UNION ALL
    SELECT 'users'
) expected
LEFT JOIN information_schema.TABLES actual
  ON actual.TABLE_SCHEMA = DATABASE()
 AND actual.TABLE_NAME = expected.table_name
WHERE actual.TABLE_NAME IS NULL;

SELECT 'column type review' AS check_name,
       CONCAT(TABLE_NAME, '.', COLUMN_NAME, ' ', COLUMN_TYPE) AS offending_value,
       0 AS row_count
FROM information_schema.COLUMNS
WHERE TABLE_SCHEMA = DATABASE()
  AND (
      (TABLE_NAME = 'inventory' AND COLUMN_NAME IN ('id','amount','item_name'))
      OR (TABLE_NAME = 'bill_history' AND COLUMN_NAME IN ('bill_id','client_submission_id','payment_method'))
      OR (TABLE_NAME = 'bill_history_items' AND COLUMN_NAME IN ('id','bill_id','amount','item_name'))
      OR (TABLE_NAME = 'stock_movement' AND COLUMN_NAME IN ('id','item_id','movement_type','qty_delta','reference_type','reference_id'))
      OR (TABLE_NAME = 'users' AND COLUMN_NAME IN ('username','password','password_hash','password_salt','password_iterations'))
  )
ORDER BY TABLE_NAME, ORDINAL_POSITION;
