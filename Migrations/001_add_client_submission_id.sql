SET @column_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'bill_history'
      AND COLUMN_NAME = 'client_submission_id'
);

SET @add_column_sql := IF(
    @column_exists = 0,
    'ALTER TABLE bill_history ADD COLUMN client_submission_id VARCHAR(64) NULL',
    'SELECT 1'
);

PREPARE add_column_stmt FROM @add_column_sql;
EXECUTE add_column_stmt;
DEALLOCATE PREPARE add_column_stmt;

SET @index_exists := (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'bill_history'
      AND INDEX_NAME = 'idx_client_submission_id'
);

SET @add_index_sql := IF(
    @index_exists = 0,
    'ALTER TABLE bill_history ADD INDEX idx_client_submission_id (client_submission_id)',
    'SELECT 1'
);

PREPARE add_index_stmt FROM @add_index_sql;
EXECUTE add_index_stmt;
DEALLOCATE PREPARE add_index_stmt;
