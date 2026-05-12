SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'bill_history'
      AND COLUMN_NAME = 'status'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE bill_history ADD COLUMN status VARCHAR(20) NOT NULL DEFAULT ''ACTIVE''',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'bill_history'
      AND COLUMN_NAME = 'voided_at'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE bill_history ADD COLUMN voided_at DATETIME NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'bill_history'
      AND COLUMN_NAME = 'voided_by'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE bill_history ADD COLUMN voided_by VARCHAR(255) NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'bill_history'
      AND COLUMN_NAME = 'void_reason'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE bill_history ADD COLUMN void_reason TEXT NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'bill_history'
      AND COLUMN_NAME = 'void_action'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE bill_history ADD COLUMN void_action VARCHAR(50) NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @idx_exists := (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'bill_history'
      AND INDEX_NAME = 'idx_bill_history_status'
);

SET @sql := IF(
    @idx_exists = 0,
    'ALTER TABLE bill_history ADD INDEX idx_bill_history_status (status)',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @stock_movement_type_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'stock_movement'
      AND COLUMN_NAME = 'movement_type'
);

SET @sql := IF(
    @stock_movement_type_exists > 0,
    'ALTER TABLE stock_movement MODIFY COLUMN movement_type ENUM(''purchase'',''sale'',''count_adjustment'',''manual_adjustment'',''return'',''wastage'',''void_reversal'') NOT NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @credit_txn_type_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'credit_transactions'
      AND COLUMN_NAME = 'txn_type'
);

SET @sql := IF(
    @credit_txn_type_exists > 0,
    'ALTER TABLE credit_transactions MODIFY COLUMN txn_type VARCHAR(30) NOT NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
