SET @table_exists := (
    SELECT COUNT(*)
    FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'stock_movement'
);

SET @create_table_sql := IF(
    @table_exists = 0,
    'CREATE TABLE stock_movement (
        id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
        item_id INT,
        item_name VARCHAR(255),
        movement_type ENUM(''purchase'',''sale'',''count_adjustment'',''manual_adjustment'',''return'',''wastage'',''void_reversal'') NOT NULL,
        qty_delta DECIMAL(14,4) NOT NULL,
        reference_type VARCHAR(100),
        reference_id BIGINT,
        occurred_at DATETIME NOT NULL,
        created_at DATETIME NOT NULL,
        created_by_user_id INT NULL,
        created_by_username VARCHAR(255),
        note TEXT NULL
    )',
    'SELECT 1'
);

PREPARE create_table_stmt FROM @create_table_sql;
EXECUTE create_table_stmt;
DEALLOCATE PREPARE create_table_stmt;
