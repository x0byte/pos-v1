SET @table_exists := (
    SELECT COUNT(*)
    FROM information_schema.TABLES
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'packaging_batch'
);

SET @sql := IF(
    @table_exists = 0,
    'CREATE TABLE packaging_batch (
        id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
        source_item_id INT NOT NULL,
        source_item_name VARCHAR(255) NOT NULL,
        source_qty_used DECIMAL(14,4) NOT NULL,
        packaged_item_id INT NOT NULL,
        packaged_item_name VARCHAR(255) NOT NULL,
        packet_size_qty DECIMAL(14,4) NOT NULL,
        packet_size_label VARCHAR(50) NOT NULL,
        packets_created INT NOT NULL,
        expiry_date DATE NULL,
        barcode VARCHAR(100) NOT NULL,
        business_registration_text VARCHAR(255) NULL,
        created_at DATETIME NOT NULL,
        created_by_username VARCHAR(255) NULL,
        note TEXT NULL,
        INDEX idx_packaging_batch_source_item (source_item_id),
        INDEX idx_packaging_batch_packaged_item (packaged_item_id),
        INDEX idx_packaging_batch_created_at (created_at),
        INDEX idx_packaging_batch_barcode (barcode)
    )',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @col_exists := (
    SELECT COUNT(*)
    FROM information_schema.COLUMNS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'packaging_batch'
      AND COLUMN_NAME = 'business_registration_text'
);

SET @sql := IF(
    @col_exists = 0,
    'ALTER TABLE packaging_batch ADD COLUMN business_registration_text VARCHAR(255) NULL AFTER barcode',
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
    'ALTER TABLE stock_movement MODIFY COLUMN movement_type ENUM(''purchase'',''sale'',''count_adjustment'',''manual_adjustment'',''return'',''wastage'',''void_reversal'',''packaging_source'',''packaging_output'') NOT NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
