SET @table_exists := (
    SELECT COUNT(*)
    FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'inventory_cost_history'
);

SET @create_table_sql := IF(
    @table_exists = 0,
    'CREATE TABLE inventory_cost_history (
        id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
        item_id INT NOT NULL,
        item_name VARCHAR(255) NOT NULL,
        old_cost DECIMAL(14,4) NULL,
        new_cost DECIMAL(14,4) NULL,
        change_pct DECIMAL(14,4) NULL,
        source VARCHAR(100) NOT NULL,
        source_reference_id BIGINT NULL,
        changed_at DATETIME NOT NULL,
        changed_by_user_id INT NULL,
        changed_by_username VARCHAR(255),
        warning_flagged TINYINT(1) NOT NULL DEFAULT 0
    )',
    'SELECT 1'
);

PREPARE create_table_stmt FROM @create_table_sql;
EXECUTE create_table_stmt;
DEALLOCATE PREPARE create_table_stmt;
