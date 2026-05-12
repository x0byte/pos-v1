-- Run this after reviewing the duplicate report below. The unique index will
-- fail if existing non-empty client_submission_id values are duplicated.

SELECT client_submission_id, COUNT(*) AS duplicate_count
FROM bill_history
WHERE client_submission_id IS NOT NULL
  AND client_submission_id <> ''
GROUP BY client_submission_id
HAVING COUNT(*) > 1;

UPDATE bill_history
SET client_submission_id = NULL
WHERE client_submission_id = '';

SET @unique_exists := (
    SELECT COUNT(*)
    FROM information_schema.STATISTICS
    WHERE TABLE_SCHEMA = DATABASE()
      AND TABLE_NAME = 'bill_history'
      AND INDEX_NAME = 'uq_bill_history_client_submission_id'
);

SET @add_unique_sql := IF(
    @unique_exists = 0,
    'ALTER TABLE bill_history ADD UNIQUE INDEX uq_bill_history_client_submission_id (client_submission_id)',
    'SELECT 1'
);

PREPARE add_unique_stmt FROM @add_unique_sql;
EXECUTE add_unique_stmt;
DEALLOCATE PREPARE add_unique_stmt;
