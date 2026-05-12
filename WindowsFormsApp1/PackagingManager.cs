using MySql.Data.MySqlClient;
using System;
using System.Data;

namespace WindowsFormsApp1
{
    public class PackagingRequest
    {
        public int SourceItemId { get; set; }
        public decimal SourceQtyUsed { get; set; }
        public int? PackagedItemId { get; set; }
        public string PackagedItemName { get; set; }
        public decimal PackagedRetailPrice { get; set; }
        public decimal PacketSizeQty { get; set; }
        public string PacketSizeLabel { get; set; }
        public int PacketsCreated { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string BusinessRegistrationText { get; set; }
        public string Note { get; set; }
    }

    public class PackagingBatchResult
    {
        public long BatchId { get; set; }
        public int PackagedItemId { get; set; }
        public string ProductName { get; set; }
        public string PacketSizeLabel { get; set; }
        public int PacketsCreated { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Barcode { get; set; }
        public string BusinessRegistrationText { get; set; }
    }

    public static class PackagingManager
    {
        private static string ConnectionString => DatabaseConfig.ConnectionString;

        public static DataTable LoadInventoryItems()
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(
                    "SELECT id, item_name, retail_price, amount, barcode, cost FROM inventory ORDER BY item_name", conn))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            }
        }

        public static DataTable LoadRecentBatches()
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                using (MySqlDataAdapter adapter = new MySqlDataAdapter(
                    @"SELECT id, packaged_item_name, packet_size_label, packets_created, expiry_date, barcode, created_at
                      FROM packaging_batch
                      ORDER BY id DESC
                      LIMIT 100", conn))
                {
                    DataTable table = new DataTable();
                    adapter.Fill(table);
                    return table;
                }
            }
        }

        public static PackagingBatchResult GetBatchForPrint(long batchId)
        {
            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand(
                    @"SELECT id, packaged_item_id, packaged_item_name, packet_size_label, packets_created, expiry_date, barcode, business_registration_text
                      FROM packaging_batch
                      WHERE id = @id
                      LIMIT 1", conn))
                {
                    cmd.Parameters.AddWithValue("@id", batchId);
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return null;
                        }

                        return new PackagingBatchResult
                        {
                            BatchId = Convert.ToInt64(reader["id"]),
                            PackagedItemId = Convert.ToInt32(reader["packaged_item_id"]),
                            ProductName = reader["packaged_item_name"].ToString(),
                            PacketSizeLabel = reader["packet_size_label"].ToString(),
                            PacketsCreated = Convert.ToInt32(reader["packets_created"]),
                            ExpiryDate = reader["expiry_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["expiry_date"]),
                            Barcode = reader["barcode"].ToString(),
                            BusinessRegistrationText = reader["business_registration_text"] == DBNull.Value ? string.Empty : reader["business_registration_text"].ToString()
                        };
                    }
                }
            }
        }

        public static PackagingBatchResult CreatePackagingBatch(PackagingRequest request)
        {
            ValidateRequest(request);
            string createdByUsername = string.IsNullOrWhiteSpace(UserSession.Username) ? "desktop-pos" : UserSession.Username;

            using (MySqlConnection conn = new MySqlConnection(ConnectionString))
            {
                conn.Open();
                using (MySqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        InventorySnapshot source = GetInventoryForUpdate(conn, tx, request.SourceItemId);
                        if (source == null)
                        {
                            throw new InvalidOperationException("Source item was not found.");
                        }

                        if (source.Amount < request.SourceQtyUsed)
                        {
                            throw new InvalidOperationException("Not enough source stock. Available: " + source.Amount.ToString("N4"));
                        }

                        int packagedItemId;
                        string packagedItemName;
                        string barcode;
                        decimal? packagedCost = CalculatePackagedCost(source.Cost, request.PacketSizeQty);

                        if (request.PackagedItemId.HasValue)
                        {
                            InventorySnapshot packaged = GetInventoryForUpdate(conn, tx, request.PackagedItemId.Value);
                            if (packaged == null)
                            {
                                throw new InvalidOperationException("Packaged item was not found.");
                            }

                            packagedItemId = packaged.Id;
                            packagedItemName = packaged.ItemName;
                            barcode = packaged.Barcode;

                            if (string.IsNullOrWhiteSpace(barcode))
                            {
                                barcode = GenerateBarcodeForItem(conn, tx, packagedItemId);
                                UpdateInventoryBarcode(conn, tx, packagedItemId, barcode);
                            }

                            UpdateInventoryCost(conn, tx, packagedItemId, packagedCost);
                        }
                        else
                        {
                            packagedItemName = request.PackagedItemName.Trim();
                            packagedItemId = InsertPackagedItem(conn, tx, request, packagedCost, createdByUsername);
                            barcode = GenerateBarcodeForItem(conn, tx, packagedItemId);
                            UpdateInventoryBarcode(conn, tx, packagedItemId, barcode);
                        }

                        UpdateInventoryAmount(conn, tx, source.Id, -request.SourceQtyUsed);
                        UpdateInventoryAmount(conn, tx, packagedItemId, request.PacketsCreated);

                        long batchId = InsertPackagingBatch(conn, tx, request, source, packagedItemId, packagedItemName, barcode, createdByUsername);
                        InsertStockMovement(conn, tx, source.Id, source.ItemName, "packaging_source", -request.SourceQtyUsed, batchId, createdByUsername,
                            "Used for packaging batch " + batchId);
                        InsertStockMovement(conn, tx, packagedItemId, packagedItemName, "packaging_output", request.PacketsCreated, batchId, createdByUsername,
                            "Created by packaging batch " + batchId);

                        tx.Commit();
                        AppCache.Refresh();

                        return new PackagingBatchResult
                        {
                            BatchId = batchId,
                            PackagedItemId = packagedItemId,
                            ProductName = packagedItemName,
                            PacketSizeLabel = request.PacketSizeLabel.Trim(),
                            PacketsCreated = request.PacketsCreated,
                            ExpiryDate = request.ExpiryDate,
                            Barcode = barcode,
                            BusinessRegistrationText = request.BusinessRegistrationText
                        };
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        private static void ValidateRequest(PackagingRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (request.SourceItemId <= 0) throw new InvalidOperationException("Select a source item.");
            if (request.SourceQtyUsed <= 0) throw new InvalidOperationException("Source quantity must be greater than 0.");
            if (request.PacketSizeQty <= 0) throw new InvalidOperationException("Packet size must be greater than 0.");
            if (request.PacketsCreated <= 0) throw new InvalidOperationException("Packets created must be greater than 0.");
            if (!request.PackagedItemId.HasValue && string.IsNullOrWhiteSpace(request.PackagedItemName))
                throw new InvalidOperationException("Enter a packaged item name or select an existing packaged item.");
            if (string.IsNullOrWhiteSpace(request.PacketSizeLabel))
                throw new InvalidOperationException("Enter a packet size label.");
        }

        private static InventorySnapshot GetInventoryForUpdate(MySqlConnection conn, MySqlTransaction tx, int itemId)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                "SELECT id, item_name, amount, barcode, cost FROM inventory WHERE id = @id FOR UPDATE", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", itemId);
                using (MySqlDataReader reader = cmd.ExecuteReader())
                {
                    if (!reader.Read()) return null;
                    return new InventorySnapshot
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        ItemName = reader["item_name"].ToString(),
                        Amount = reader["amount"] == DBNull.Value ? 0m : Convert.ToDecimal(reader["amount"]),
                        Barcode = reader["barcode"] == DBNull.Value ? null : reader["barcode"].ToString(),
                        Cost = reader["cost"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(reader["cost"])
                    };
                }
            }
        }

        private static decimal? CalculatePackagedCost(decimal? sourceCost, decimal packetSizeQty)
        {
            return sourceCost.HasValue ? (decimal?)(sourceCost.Value * packetSizeQty) : null;
        }

        private static int InsertPackagedItem(MySqlConnection conn, MySqlTransaction tx, PackagingRequest request, decimal? packagedCost, string createdByUsername)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                @"INSERT INTO inventory (item_name, retail_price, amount, added_by, keywords, barcode, cost)
                  VALUES (@item_name, @retail_price, 0, @added_by, @keywords, '', @cost)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@item_name", request.PackagedItemName.Trim());
                cmd.Parameters.AddWithValue("@retail_price", request.PackagedRetailPrice);
                cmd.Parameters.AddWithValue("@added_by", createdByUsername);
                cmd.Parameters.AddWithValue("@keywords", KeywordGenerator.MergeWithGenerated(request.PackagedItemName, request.PackagedItemName));
                cmd.Parameters.AddWithValue("@cost", packagedCost.HasValue ? (object)packagedCost.Value : DBNull.Value);
                cmd.ExecuteNonQuery();
                return Convert.ToInt32(cmd.LastInsertedId);
            }
        }

        private static string GenerateBarcodeForItem(MySqlConnection conn, MySqlTransaction tx, int itemId)
        {
            string barcode = "PK" + itemId.ToString("D8");
            int suffix = 1;
            while (BarcodeExists(conn, tx, barcode, itemId))
            {
                barcode = "PK" + itemId.ToString("D8") + suffix.ToString("D2");
                suffix++;
            }
            return barcode;
        }

        private static bool BarcodeExists(MySqlConnection conn, MySqlTransaction tx, string barcode, int currentItemId)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                "SELECT COUNT(*) FROM inventory WHERE barcode = @barcode AND id <> @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@barcode", barcode);
                cmd.Parameters.AddWithValue("@id", currentItemId);
                return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
            }
        }

        private static void UpdateInventoryBarcode(MySqlConnection conn, MySqlTransaction tx, int itemId, string barcode)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                "UPDATE inventory SET barcode = @barcode WHERE id = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@barcode", barcode);
                cmd.Parameters.AddWithValue("@id", itemId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdateInventoryAmount(MySqlConnection conn, MySqlTransaction tx, int itemId, decimal qtyDelta)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                "UPDATE inventory SET amount = amount + @qty_delta WHERE id = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@qty_delta", qtyDelta);
                cmd.Parameters.AddWithValue("@id", itemId);
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdateInventoryCost(MySqlConnection conn, MySqlTransaction tx, int itemId, decimal? cost)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                "UPDATE inventory SET cost = @cost WHERE id = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@cost", cost.HasValue ? (object)cost.Value : DBNull.Value);
                cmd.Parameters.AddWithValue("@id", itemId);
                cmd.ExecuteNonQuery();
            }
        }

        private static long InsertPackagingBatch(
            MySqlConnection conn,
            MySqlTransaction tx,
            PackagingRequest request,
            InventorySnapshot source,
            int packagedItemId,
            string packagedItemName,
            string barcode,
            string createdByUsername)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                @"INSERT INTO packaging_batch
                  (source_item_id, source_item_name, source_qty_used, packaged_item_id, packaged_item_name,
                   packet_size_qty, packet_size_label, packets_created, expiry_date, barcode, business_registration_text, created_at, created_by_username, note)
                  VALUES
                  (@source_item_id, @source_item_name, @source_qty_used, @packaged_item_id, @packaged_item_name,
                   @packet_size_qty, @packet_size_label, @packets_created, @expiry_date, @barcode, @business_registration_text, NOW(), @created_by_username, @note)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@source_item_id", source.Id);
                cmd.Parameters.AddWithValue("@source_item_name", source.ItemName);
                cmd.Parameters.AddWithValue("@source_qty_used", request.SourceQtyUsed);
                cmd.Parameters.AddWithValue("@packaged_item_id", packagedItemId);
                cmd.Parameters.AddWithValue("@packaged_item_name", packagedItemName);
                cmd.Parameters.AddWithValue("@packet_size_qty", request.PacketSizeQty);
                cmd.Parameters.AddWithValue("@packet_size_label", request.PacketSizeLabel.Trim());
                cmd.Parameters.AddWithValue("@packets_created", request.PacketsCreated);
                cmd.Parameters.AddWithValue("@expiry_date", request.ExpiryDate.HasValue ? (object)request.ExpiryDate.Value.Date : DBNull.Value);
                cmd.Parameters.AddWithValue("@barcode", barcode);
                cmd.Parameters.AddWithValue("@business_registration_text", string.IsNullOrWhiteSpace(request.BusinessRegistrationText) ? (object)DBNull.Value : request.BusinessRegistrationText.Trim());
                cmd.Parameters.AddWithValue("@created_by_username", createdByUsername);
                cmd.Parameters.AddWithValue("@note", string.IsNullOrWhiteSpace(request.Note) ? (object)DBNull.Value : request.Note.Trim());
                cmd.ExecuteNonQuery();
                return cmd.LastInsertedId;
            }
        }

        private static void InsertStockMovement(
            MySqlConnection conn,
            MySqlTransaction tx,
            int itemId,
            string itemName,
            string movementType,
            decimal qtyDelta,
            long batchId,
            string createdByUsername,
            string note)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                @"INSERT INTO stock_movement
                  (item_id, item_name, movement_type, qty_delta, reference_type, reference_id,
                   occurred_at, created_at, created_by_user_id, created_by_username, note)
                  VALUES
                  (@item_id, @item_name, @movement_type, @qty_delta, 'packaging_batch', @reference_id,
                   NOW(), NOW(), NULL, @created_by_username, @note)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@item_id", itemId);
                cmd.Parameters.AddWithValue("@item_name", itemName);
                cmd.Parameters.AddWithValue("@movement_type", movementType);
                cmd.Parameters.AddWithValue("@qty_delta", qtyDelta);
                cmd.Parameters.AddWithValue("@reference_id", batchId);
                cmd.Parameters.AddWithValue("@created_by_username", createdByUsername);
                cmd.Parameters.AddWithValue("@note", note);
                cmd.ExecuteNonQuery();
            }
        }

        private class InventorySnapshot
        {
            public int Id { get; set; }
            public string ItemName { get; set; }
            public decimal Amount { get; set; }
            public string Barcode { get; set; }
            public decimal? Cost { get; set; }
        }
    }
}
