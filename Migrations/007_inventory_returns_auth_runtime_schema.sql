-- Idempotent production correction schema for STC POS.
-- Safe for existing databases: creates missing tables, adds missing columns, preserves rows.

CREATE TABLE IF NOT EXISTS schema_migrations (
    version VARCHAR(32) NOT NULL PRIMARY KEY,
    description VARCHAR(255) NOT NULL,
    applied_at DATETIME NOT NULL
);

CREATE TABLE IF NOT EXISTS inventory (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    item_name VARCHAR(255) NOT NULL,
    retail_price DECIMAL(14,2) NOT NULL DEFAULT 0,
    amount DECIMAL(14,4) NOT NULL DEFAULT 0,
    added_by VARCHAR(255) NULL,
    keywords TEXT NULL,
    barcode VARCHAR(100) NULL,
    cost DECIMAL(14,2) NULL,
    stock_update_time DATETIME NULL,
    updated_at DATETIME NULL,
    INDEX idx_inventory_item_name (item_name),
    INDEX idx_inventory_barcode (barcode)
);

CREATE TABLE IF NOT EXISTS employee (
    emp_code VARCHAR(100) NOT NULL PRIMARY KEY,
    emp_name VARCHAR(255) NULL
);

CREATE TABLE IF NOT EXISTS users (
    id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    username VARCHAR(255) NOT NULL UNIQUE,
    password VARCHAR(255) NULL,
    isAdmin TINYINT NOT NULL DEFAULT 0
);

CREATE TABLE IF NOT EXISTS bill_history (
    bill_id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    bill_code VARCHAR(64) NULL,
    date_time DATETIME NOT NULL,
    salesperson VARCHAR(255) NULL,
    total_amount DECIMAL(14,2) NOT NULL DEFAULT 0,
    discount_amount DECIMAL(14,2) NOT NULL DEFAULT 0,
    grand_total DECIMAL(14,2) NOT NULL DEFAULT 0,
    item_count INT NOT NULL DEFAULT 0,
    client_submission_id VARCHAR(128) NULL,
    payment_method VARCHAR(30) NOT NULL DEFAULT 'CASH',
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    voided_at DATETIME NULL,
    voided_by VARCHAR(255) NULL,
    void_reason TEXT NULL,
    void_action VARCHAR(50) NULL,
    INDEX idx_bill_history_code (bill_code),
    INDEX idx_bill_history_date (date_time),
    UNIQUE INDEX uq_bill_history_client_submission_id (client_submission_id)
);

CREATE TABLE IF NOT EXISTS bill_history_items (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    bill_id BIGINT NOT NULL,
    item_name VARCHAR(255) NOT NULL,
    rate DECIMAL(14,2) NOT NULL DEFAULT 0,
    amount DECIMAL(14,4) NOT NULL DEFAULT 0,
    discounted_price DECIMAL(14,2) NOT NULL DEFAULT 0,
    INDEX idx_bill_history_items_bill (bill_id)
);

CREATE TABLE IF NOT EXISTS bill_payments (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    bill_id BIGINT NOT NULL,
    bill_code VARCHAR(64) NOT NULL,
    payment_method VARCHAR(30) NOT NULL,
    amount DECIMAL(14,2) NOT NULL DEFAULT 0,
    created_at DATETIME NOT NULL,
    INDEX idx_bill_payments_bill (bill_id),
    INDEX idx_bill_payments_code (bill_code)
);

CREATE TABLE IF NOT EXISTS credit_accounts (
    account_id INT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    customer_name VARCHAR(255) NOT NULL,
    label VARCHAR(255) NULL,
    is_active TINYINT NOT NULL DEFAULT 1,
    created_by VARCHAR(255) NULL,
    created_at DATETIME NOT NULL,
    INDEX idx_credit_accounts_customer (customer_name)
);

CREATE TABLE IF NOT EXISTS credit_transactions (
    txn_id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    account_id INT NOT NULL,
    txn_type VARCHAR(30) NOT NULL,
    amount DECIMAL(14,2) NOT NULL,
    direction VARCHAR(20) NOT NULL,
    description VARCHAR(255) NULL,
    bill_code VARCHAR(100) NULL,
    txn_date DATE NOT NULL,
    recorded_by VARCHAR(255) NULL,
    recorded_at DATETIME NOT NULL,
    INDEX idx_credit_transactions_account (account_id),
    INDEX idx_credit_transactions_bill (bill_code)
);

CREATE TABLE IF NOT EXISTS pending_bill (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    session_id VARCHAR(128) NULL,
    cashier_code VARCHAR(100) NULL,
    salesperson_code VARCHAR(100) NULL,
    status VARCHAR(50) NULL,
    created_at DATETIME NOT NULL,
    updated_at DATETIME NULL,
    INDEX idx_pending_bill_status (status),
    INDEX idx_pending_bill_created (created_at)
);

CREATE TABLE IF NOT EXISTS pending_bill_items (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    pending_bill_id BIGINT NOT NULL,
    item_name VARCHAR(255) NOT NULL,
    rate DECIMAL(14,2) NOT NULL DEFAULT 0,
    amount DECIMAL(14,4) NOT NULL DEFAULT 0,
    discounted_price DECIMAL(14,2) NOT NULL DEFAULT 0,
    INDEX idx_pending_bill_items_bill (pending_bill_id)
);

CREATE TABLE IF NOT EXISTS stock_movement (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    item_id INT NULL,
    item_name VARCHAR(255) NULL,
    movement_type ENUM(
        'purchase','sale','count_adjustment','manual_adjustment','return','wastage','void_reversal',
        'packaging_source','packaging_output','packaging_consumption',
        'customer_return_restock','customer_return_not_restocked',
        'purchase_receipt','manual_adjustment_in','manual_adjustment_out','stocktake_correction'
    ) NOT NULL,
    qty_delta DECIMAL(14,4) NOT NULL,
    reference_type VARCHAR(100) NULL,
    reference_id BIGINT NULL,
    occurred_at DATETIME NOT NULL,
    created_at DATETIME NOT NULL,
    created_by_user_id INT NULL,
    created_by_username VARCHAR(255) NULL,
    note TEXT NULL,
    INDEX idx_stock_movement_item (item_id),
    INDEX idx_stock_movement_reference (reference_type, reference_id),
    INDEX idx_stock_movement_type (movement_type),
    INDEX idx_stock_movement_occurred (occurred_at)
);

CREATE TABLE IF NOT EXISTS returns (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    return_reference VARCHAR(80) NOT NULL,
    original_bill_id BIGINT NULL,
    return_date DATETIME NOT NULL,
    cashier VARCHAR(255) NOT NULL,
    approved_by VARCHAR(255) NULL,
    is_bill_linked TINYINT NOT NULL DEFAULT 0,
    refund_method VARCHAR(30) NOT NULL,
    refund_total DECIMAL(14,2) NOT NULL DEFAULT 0,
    reason TEXT NOT NULL,
    notes TEXT NULL,
    customer_name VARCHAR(255) NULL,
    customer_phone VARCHAR(100) NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'ACTIVE',
    exchange_reference VARCHAR(100) NULL,
    local_transaction_id VARCHAR(128) NOT NULL,
    created_at DATETIME NOT NULL,
    UNIQUE INDEX uq_returns_reference (return_reference),
    UNIQUE INDEX uq_returns_local_transaction (local_transaction_id),
    INDEX idx_returns_bill (original_bill_id),
    INDEX idx_returns_date (return_date)
);

CREATE TABLE IF NOT EXISTS return_items (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    return_id BIGINT NOT NULL,
    original_bill_item_id BIGINT NULL,
    product_id INT NOT NULL,
    item_name VARCHAR(255) NOT NULL,
    quantity DECIMAL(14,4) NOT NULL,
    original_unit_price DECIMAL(14,2) NULL,
    entered_unit_refund DECIMAL(14,2) NOT NULL DEFAULT 0,
    refund_amount DECIMAL(14,2) NOT NULL DEFAULT 0,
    item_condition VARCHAR(30) NOT NULL,
    restock TINYINT NOT NULL DEFAULT 0,
    stock_movement_id BIGINT NULL,
    notes TEXT NULL,
    INDEX idx_return_items_return (return_id),
    INDEX idx_return_items_original_item (original_bill_item_id),
    INDEX idx_return_items_product (product_id)
);

CREATE TABLE IF NOT EXISTS return_refunds (
    id BIGINT NOT NULL AUTO_INCREMENT PRIMARY KEY,
    return_id BIGINT NOT NULL,
    return_reference VARCHAR(80) NOT NULL,
    refund_method VARCHAR(30) NOT NULL,
    amount DECIMAL(14,2) NOT NULL DEFAULT 0,
    credit_account_id INT NULL,
    created_at DATETIME NOT NULL,
    INDEX idx_return_refunds_return (return_id),
    INDEX idx_return_refunds_reference (return_reference)
);

SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'inventory' AND COLUMN_NAME = 'stock_update_time');
SET @sql := IF(@col_exists = 0, 'ALTER TABLE inventory ADD COLUMN stock_update_time DATETIME NULL', 'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'bill_history' AND COLUMN_NAME = 'payment_method');
SET @sql := IF(@col_exists = 0, 'ALTER TABLE bill_history ADD COLUMN payment_method VARCHAR(30) NOT NULL DEFAULT ''CASH''', 'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'users' AND COLUMN_NAME = 'password_hash');
SET @sql := IF(@col_exists = 0, 'ALTER TABLE users ADD COLUMN password_hash VARCHAR(255) NULL', 'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'users' AND COLUMN_NAME = 'password_salt');
SET @sql := IF(@col_exists = 0, 'ALTER TABLE users ADD COLUMN password_salt VARCHAR(255) NULL', 'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'users' AND COLUMN_NAME = 'password_iterations');
SET @sql := IF(@col_exists = 0, 'ALTER TABLE users ADD COLUMN password_iterations INT NULL', 'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'users' AND COLUMN_NAME = 'password_migrated_at');
SET @sql := IF(@col_exists = 0, 'ALTER TABLE users ADD COLUMN password_migrated_at DATETIME NULL', 'SELECT 1');
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @movement_col_exists := (SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'stock_movement' AND COLUMN_NAME = 'movement_type');
SET @unknown_movement_count := (
    SELECT COUNT(*)
    FROM stock_movement
    WHERE movement_type NOT IN (
        'purchase','sale','count_adjustment','manual_adjustment','return','wastage','void_reversal',
        'packaging_source','packaging_output','packaging_consumption',
        'customer_return_restock','customer_return_not_restocked',
        'purchase_receipt','manual_adjustment_in','manual_adjustment_out','stocktake_correction'
    )
);
SET @sql := IF(@movement_col_exists > 0 AND @unknown_movement_count > 0,
    'SIGNAL SQLSTATE ''45000'' SET MESSAGE_TEXT = ''Migration 007 blocked: stock_movement has movement_type values not recognised by this release''',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @sql := IF(
    @movement_col_exists > 0,
    'ALTER TABLE stock_movement MODIFY COLUMN movement_type ENUM(''purchase'',''sale'',''count_adjustment'',''manual_adjustment'',''return'',''wastage'',''void_reversal'',''packaging_source'',''packaging_output'',''packaging_consumption'',''customer_return_restock'',''customer_return_not_restocked'',''purchase_receipt'',''manual_adjustment_in'',''manual_adjustment_out'',''stocktake_correction'') NOT NULL',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

UPDATE bill_history SET payment_method = 'CASH' WHERE payment_method IS NULL OR payment_method = '';

SET @duplicate_submission_count := (
    SELECT COUNT(*)
    FROM (
        SELECT client_submission_id
        FROM bill_history
        WHERE client_submission_id IS NOT NULL
          AND client_submission_id <> ''
        GROUP BY client_submission_id
        HAVING COUNT(*) > 1
    ) dupes
);
SET @sql := IF(@duplicate_submission_count > 0,
    'SIGNAL SQLSTATE ''45000'' SET MESSAGE_TEXT = ''Migration 007 blocked: duplicate bill_history.client_submission_id values must be resolved before enforcing fallback idempotency''',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

SET @unique_exists := (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'bill_history'
      AND INDEX_NAME = 'uq_bill_history_client_submission_id'
);
SET @sql := IF(@unique_exists = 0,
    'ALTER TABLE bill_history ADD UNIQUE INDEX uq_bill_history_client_submission_id (client_submission_id)',
    'SELECT 1'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

INSERT INTO schema_migrations (version, description, applied_at)
VALUES ('007', 'inventory returns auth runtime schema', NOW())
ON DUPLICATE KEY UPDATE applied_at = applied_at;
