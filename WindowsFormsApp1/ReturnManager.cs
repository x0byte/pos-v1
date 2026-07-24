using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace WindowsFormsApp1
{
    public static class ReturnManager
    {
        private static string Conn => DatabaseConfig.ConnectionString;

        public static OriginalBillLookupResult FindOriginalBill(string billCode)
        {
            if (string.IsNullOrWhiteSpace(billCode))
            {
                throw new InvalidOperationException("Enter a bill reference.");
            }

            int billId = BillHistoryManager.GetBillIdByCode(billCode.Trim());
            if (billId <= 0)
            {
                return null;
            }

            DataRow header = BillHistoryManager.GetBillHeader(billId);
            DataTable items = LoadReturnableBillItems(billId);
            return new OriginalBillLookupResult { Header = header, Items = items };
        }

        public static DataTable LoadReturnableBillItems(int billId)
        {
            using (MySqlConnection conn = new MySqlConnection(Conn))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(
                    @"SELECT bhi.id AS original_bill_item_id,
                             bhi.item_name,
                             inv.id AS product_id,
                             bhi.rate,
                             bhi.amount AS sold_quantity,
                             bhi.discounted_price,
                             COALESCE(SUM(CASE WHEN r.id IS NOT NULL THEN ri.quantity ELSE 0 END), 0) AS already_returned,
                             (bhi.amount - COALESCE(SUM(CASE WHEN r.id IS NOT NULL THEN ri.quantity ELSE 0 END), 0)) AS remaining_quantity
                      FROM bill_history_items bhi
                      LEFT JOIN inventory inv ON inv.item_name = bhi.item_name
                      LEFT JOIN return_items ri ON ri.original_bill_item_id = bhi.id
                      LEFT JOIN returns r ON r.id = ri.return_id AND r.status <> 'VOIDED'
                      WHERE bhi.bill_id = @bill_id
                      GROUP BY bhi.id, bhi.item_name, inv.id, bhi.rate, bhi.amount, bhi.discounted_price
                      ORDER BY bhi.id", conn))
                {
                    cmd.Parameters.AddWithValue("@bill_id", billId);
                    DataTable table = new DataTable();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        adapter.Fill(table);
                    }
                    return table;
                }
            }
        }

        public static ReturnResult ProcessReturn(ReturnRequest request)
        {
            ValidateRequest(request);
            string transactionId = string.IsNullOrWhiteSpace(request.LocalTransactionId)
                ? Guid.NewGuid().ToString("N")
                : request.LocalTransactionId.Trim();

            using (MySqlConnection conn = new MySqlConnection(Conn))
            {
                conn.Open();
                ReturnResult existing = GetExistingReturn(conn, transactionId);
                if (existing != null)
                {
                    existing.ExistingTransaction = true;
                    return existing;
                }

                using (MySqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        if (request.OriginalBillId.HasValue)
                        {
                            EnsureBillEligibleForReturn(conn, tx, request.OriginalBillId.Value);
                        }

                        string reference = GenerateReturnReference();
                        decimal refundTotal = 0m;
                        foreach (ReturnItemRequest item in request.Items)
                        {
                            refundTotal += item.RefundAmount;
                        }

                        long returnId = InsertReturnHeader(conn, tx, request, reference, transactionId, refundTotal);
                        foreach (ReturnItemRequest item in request.Items)
                        {
                            ProcessReturnItem(conn, tx, returnId, reference, request, item);
                        }

                        InsertRefundRecord(conn, tx, returnId, reference, request, refundTotal);
                        if (string.Equals(request.RefundMethod, "CREDIT", StringComparison.OrdinalIgnoreCase) && request.CreditAccountId > 0 && refundTotal > 0m)
                        {
                            CreditManager.AddTransaction(conn, tx, request.CreditAccountId, "RETURN", refundTotal, "CREDIT",
                                "Customer return " + reference, reference, DateTime.Today, request.Cashier ?? "desktop-pos");
                        }

                        tx.Commit();
                        RefreshCacheAfterCommit();
                        return new ReturnResult { ReturnId = returnId, ReturnReference = reference };
                    }
                    catch
                    {
                        TryRollback(tx);
                        ReturnResult committed = GetExistingReturn(conn, transactionId);
                        if (committed != null)
                        {
                            committed.ExistingTransaction = true;
                            return committed;
                        }
                        throw;
                    }
                }
            }
        }

        public static DataTable GetReturnHistory()
        {
            using (MySqlConnection conn = new MySqlConnection(Conn))
            {
                conn.Open();
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(
                    @"SELECT r.id, r.return_reference, r.return_date, r.is_bill_linked, bh.bill_code AS original_bill_code,
                             r.customer_name, r.customer_phone, r.refund_method, r.refund_total,
                             r.reason, r.cashier, r.approved_by, r.status, r.exchange_reference
                      FROM returns r
                      LEFT JOIN bill_history bh ON bh.bill_id = r.original_bill_id
                      ORDER BY r.return_date DESC, r.id DESC
                      LIMIT 500", conn))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            }
        }

        public static DataTable GetReturnItems(long returnId)
        {
            using (MySqlConnection conn = new MySqlConnection(Conn))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(
                    @"SELECT item_name, quantity, original_unit_price, entered_unit_refund, refund_amount,
                             item_condition, restock, notes
                      FROM return_items
                      WHERE return_id = @return_id
                      ORDER BY id", conn))
                {
                    cmd.Parameters.AddWithValue("@return_id", returnId);
                    DataTable table = new DataTable();
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                    {
                        adapter.Fill(table);
                    }
                    return table;
                }
            }
        }

        private static void ValidateRequest(ReturnRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.Items == null || request.Items.Count == 0)
            {
                throw new InvalidOperationException("Add at least one returned item.");
            }
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new InvalidOperationException("Return reason is required.");
            }
            if (!request.IsBillLinked && string.IsNullOrWhiteSpace(request.ApprovedBy))
            {
                throw new InvalidOperationException("Supervisor approval is required for a return without a bill.");
            }
            if (string.IsNullOrWhiteSpace(request.RefundMethod))
            {
                throw new InvalidOperationException("Refund method is required.");
            }

            foreach (ReturnItemRequest item in request.Items)
            {
                if (item.Quantity <= 0m)
                {
                    throw new InvalidOperationException("Returned quantity must be greater than zero.");
                }
                if (item.RefundAmount < 0m || item.EnteredUnitRefund < 0m)
                {
                    throw new InvalidOperationException("Refund values cannot be negative.");
                }
                if (string.IsNullOrWhiteSpace(item.Condition))
                {
                    throw new InvalidOperationException("Return condition is required for each item.");
                }
                if (item.ProductId <= 0)
                {
                    throw new InvalidOperationException("Each return item must be linked to an inventory product.");
                }
            }
        }

        private static ReturnResult GetExistingReturn(MySqlConnection conn, string transactionId)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                "SELECT id, return_reference FROM returns WHERE local_transaction_id = @tx LIMIT 1", conn))
            {
                cmd.Parameters.AddWithValue("@tx", transactionId);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    return new ReturnResult
                    {
                        ReturnId = Convert.ToInt64(reader["id"]),
                        ReturnReference = Convert.ToString(reader["return_reference"])
                    };
                }
            }
        }

        private static void EnsureBillEligibleForReturn(MySqlConnection conn, MySqlTransaction tx, int billId)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                "SELECT COALESCE(status, 'ACTIVE') FROM bill_history WHERE bill_id = @bill_id FOR UPDATE", conn, tx))
            {
                cmd.Parameters.AddWithValue("@bill_id", billId);
                object status = cmd.ExecuteScalar();
                if (status == null || status == DBNull.Value)
                {
                    throw new InvalidOperationException("Original bill was not found.");
                }
                if (string.Equals(Convert.ToString(status), "VOIDED", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Returns are not allowed against a voided bill.");
                }
            }
        }

        private static long InsertReturnHeader(MySqlConnection conn, MySqlTransaction tx, ReturnRequest request, string reference, string transactionId, decimal refundTotal)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                @"INSERT INTO returns
                  (return_reference, original_bill_id, return_date, cashier, approved_by, is_bill_linked,
                   refund_method, refund_total, reason, notes, customer_name, customer_phone,
                   status, exchange_reference, local_transaction_id, created_at)
                  VALUES
                  (@ref, @bill_id, NOW(), @cashier, @approved_by, @is_linked,
                   @refund_method, @refund_total, @reason, @notes, @customer_name, @customer_phone,
                   'ACTIVE', @exchange_reference, @local_transaction_id, NOW())", conn, tx))
            {
                cmd.Parameters.AddWithValue("@ref", reference);
                cmd.Parameters.AddWithValue("@bill_id", request.OriginalBillId.HasValue ? (object)request.OriginalBillId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@cashier", string.IsNullOrWhiteSpace(request.Cashier) ? "desktop-pos" : request.Cashier);
                cmd.Parameters.AddWithValue("@approved_by", string.IsNullOrWhiteSpace(request.ApprovedBy) ? (object)DBNull.Value : request.ApprovedBy);
                cmd.Parameters.AddWithValue("@is_linked", request.IsBillLinked ? 1 : 0);
                cmd.Parameters.AddWithValue("@refund_method", request.RefundMethod);
                cmd.Parameters.AddWithValue("@refund_total", refundTotal);
                cmd.Parameters.AddWithValue("@reason", request.Reason.Trim());
                cmd.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(request.Notes) ? (object)DBNull.Value : request.Notes.Trim());
                cmd.Parameters.AddWithValue("@customer_name", string.IsNullOrWhiteSpace(request.CustomerName) ? (object)DBNull.Value : request.CustomerName.Trim());
                cmd.Parameters.AddWithValue("@customer_phone", string.IsNullOrWhiteSpace(request.CustomerPhone) ? (object)DBNull.Value : request.CustomerPhone.Trim());
                cmd.Parameters.AddWithValue("@exchange_reference", string.IsNullOrWhiteSpace(request.ExchangeReference) ? (object)DBNull.Value : request.ExchangeReference.Trim());
                cmd.Parameters.AddWithValue("@local_transaction_id", transactionId);
                cmd.ExecuteNonQuery();
                return cmd.LastInsertedId;
            }
        }

        private static void ProcessReturnItem(MySqlConnection conn, MySqlTransaction tx, long returnId, string reference, ReturnRequest request, ReturnItemRequest item)
        {
            if (request.OriginalBillId.HasValue)
            {
                ValidateLinkedQuantity(conn, tx, request.OriginalBillId.Value, item);
            }

            long? movementId = null;
            if (item.Restock)
            {
                decimal delta = InventoryMutationRules.ReturnDelta(item.Quantity, true);
                UpdateInventoryBalance(conn, tx, item.ProductId, delta);
                movementId = InsertReturnMovement(conn, tx, item, returnId, reference, delta, InventoryMutationRules.CustomerReturnRestock);
            }
            else
            {
                movementId = InsertReturnMovement(conn, tx, item, returnId, reference,
                    InventoryMutationRules.ReturnDelta(item.Quantity, false), InventoryMutationRules.CustomerReturnNotRestocked);
            }

            InsertReturnItem(conn, tx, returnId, item, movementId);
        }

        private static void ValidateLinkedQuantity(MySqlConnection conn, MySqlTransaction tx, int originalBillId, ReturnItemRequest item)
        {
            if (!item.OriginalBillItemId.HasValue)
            {
                throw new InvalidOperationException("Linked returns require original bill item references.");
            }

            decimal sold;
            int actualBillId;
            int actualProductId;
            using (MySqlCommand cmd = new MySqlCommand(
                @"SELECT bhi.bill_id, bhi.amount, inv.id AS product_id
                  FROM bill_history_items bhi
                  LEFT JOIN inventory inv ON inv.item_name = bhi.item_name
                  WHERE bhi.id = @item_id
                  FOR UPDATE", conn, tx))
            {
                cmd.Parameters.AddWithValue("@item_id", item.OriginalBillItemId.Value);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        throw new InvalidOperationException("Original bill item was not found.");
                    }

                    actualBillId = Convert.ToInt32(reader["bill_id"]);
                    actualProductId = reader["product_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["product_id"]);
                    sold = Convert.ToDecimal(reader["amount"]);
                }
            }

            ReturnValidationRules.EnsureLinkedBillItemBelongsToBill(originalBillId, actualBillId);
            ReturnValidationRules.EnsureLinkedProductMatchesInventoryItem(item.ProductId, actualProductId);

            decimal returned;
            using (MySqlCommand cmd = new MySqlCommand(
                @"SELECT COALESCE(SUM(ri.quantity), 0)
                  FROM return_items ri
                  INNER JOIN returns r ON r.id = ri.return_id
                  WHERE ri.original_bill_item_id = @item_id
                    AND r.status <> 'VOIDED'", conn, tx))
            {
                cmd.Parameters.AddWithValue("@item_id", item.OriginalBillItemId.Value);
                returned = Convert.ToDecimal(cmd.ExecuteScalar());
            }

            ReturnValidationRules.EnsureReturnQuantityWithinSoldQuantity(sold, returned, item.Quantity);
        }

        private static void UpdateInventoryBalance(MySqlConnection conn, MySqlTransaction tx, int productId, decimal quantity)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                "UPDATE inventory SET amount = COALESCE(amount, 0) + @qty, stock_update_time = NOW() WHERE id = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@qty", quantity);
                cmd.Parameters.AddWithValue("@id", productId);
                if (cmd.ExecuteNonQuery() != 1)
                {
                    throw new InvalidOperationException("Inventory update failed for returned product.");
                }
            }
        }

        private static long InsertReturnMovement(MySqlConnection conn, MySqlTransaction tx, ReturnItemRequest item, long returnId, string reference, decimal qtyDelta, string movementType)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                @"INSERT INTO stock_movement
                  (item_id, item_name, movement_type, qty_delta, reference_type, reference_id,
                   occurred_at, created_at, created_by_user_id, created_by_username, note)
                  VALUES
                  (@item_id, @item_name, @movement_type, @qty_delta, 'customer_return', @return_id,
                   NOW(), NOW(), NULL, @created_by_username, @note)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@item_id", item.ProductId);
                cmd.Parameters.AddWithValue("@item_name", item.ItemName ?? string.Empty);
                cmd.Parameters.AddWithValue("@movement_type", movementType);
                cmd.Parameters.AddWithValue("@qty_delta", qtyDelta);
                cmd.Parameters.AddWithValue("@return_id", returnId);
                cmd.Parameters.AddWithValue("@created_by_username", UserSession.Username ?? "desktop-pos");
                cmd.Parameters.AddWithValue("@note", movementType == InventoryMutationRules.CustomerReturnRestock
                    ? "Restocked customer return " + reference
                    : "Customer return not restocked " + reference + " condition=" + item.Condition);
                cmd.ExecuteNonQuery();
                return cmd.LastInsertedId;
            }
        }

        private static void InsertReturnItem(MySqlConnection conn, MySqlTransaction tx, long returnId, ReturnItemRequest item, long? movementId)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                @"INSERT INTO return_items
                  (return_id, original_bill_item_id, product_id, item_name, quantity, original_unit_price,
                   entered_unit_refund, refund_amount, item_condition, restock, stock_movement_id, notes)
                  VALUES
                  (@return_id, @original_bill_item_id, @product_id, @item_name, @quantity, @original_unit_price,
                   @entered_unit_refund, @refund_amount, @condition, @restock, @stock_movement_id, @notes)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@return_id", returnId);
                cmd.Parameters.AddWithValue("@original_bill_item_id", item.OriginalBillItemId.HasValue ? (object)item.OriginalBillItemId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@product_id", item.ProductId);
                cmd.Parameters.AddWithValue("@item_name", item.ItemName ?? string.Empty);
                cmd.Parameters.AddWithValue("@quantity", item.Quantity);
                cmd.Parameters.AddWithValue("@original_unit_price", item.OriginalUnitPrice.HasValue ? (object)item.OriginalUnitPrice.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@entered_unit_refund", item.EnteredUnitRefund);
                cmd.Parameters.AddWithValue("@refund_amount", item.RefundAmount);
                cmd.Parameters.AddWithValue("@condition", item.Condition);
                cmd.Parameters.AddWithValue("@restock", item.Restock ? 1 : 0);
                cmd.Parameters.AddWithValue("@stock_movement_id", movementId.HasValue ? (object)movementId.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@notes", string.IsNullOrWhiteSpace(item.Notes) ? (object)DBNull.Value : item.Notes.Trim());
                cmd.ExecuteNonQuery();
            }
        }

        private static void InsertRefundRecord(MySqlConnection conn, MySqlTransaction tx, long returnId, string reference, ReturnRequest request, decimal amount)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                @"INSERT INTO return_refunds
                  (return_id, return_reference, refund_method, amount, credit_account_id, created_at)
                  VALUES
                  (@return_id, @return_reference, @refund_method, @amount, @credit_account_id, NOW())", conn, tx))
            {
                cmd.Parameters.AddWithValue("@return_id", returnId);
                cmd.Parameters.AddWithValue("@return_reference", reference);
                cmd.Parameters.AddWithValue("@refund_method", request.RefundMethod);
                cmd.Parameters.AddWithValue("@amount", amount);
                cmd.Parameters.AddWithValue("@credit_account_id", request.CreditAccountId > 0 ? (object)request.CreditAccountId : DBNull.Value);
                cmd.ExecuteNonQuery();
            }
        }

        private static string GenerateReturnReference()
        {
            return "RET-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
        }

        private static void RefreshCacheAfterCommit()
        {
            try
            {
                AppCache.Refresh();
            }
            catch (Exception ex)
            {
                UpdateLogger.Error("Inventory cache refresh failed after committed return transaction", ex);
            }
        }

        private static void TryRollback(MySqlTransaction tx)
        {
            try
            {
                tx.Rollback();
            }
            catch (Exception ex)
            {
                UpdateLogger.Error("Return transaction rollback failed or transaction outcome was already decided", ex);
            }
        }
    }
}
