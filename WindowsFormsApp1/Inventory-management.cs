using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Inventory_management : Form
    {
        private static readonly Regex SafeSearchPattern = new Regex(@"^[a-zA-Z0-9\s]*$", RegexOptions.Compiled);
        private string connectionString = DatabaseConfig.ConnectionString;
        public Inventory_management()
        {
            InitializeComponent();
            dataGridInventory.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            InitializeStockRefreshControls();
            LoadInventoryData();
            txtSearch.TextChanged += TxtSearch_TextChanged;

            this.dataGridInventory.CellClick += new DataGridViewCellEventHandler(this.dataGridInventory_CellClick);


        }

        private void InitializeStockRefreshControls()
        {
            txtSearch.Width = Math.Max(250, button1.Left - txtSearch.Left - 120);

            Button btnRefreshStock = new Button
            {
                Name = "btnRefreshStock",
                Text = "Refresh",
                Font = new Font("Microsoft Sans Serif", 10.8F, FontStyle.Regular, GraphicsUnit.Point, 0),
                Location = new Point(button1.Left - 105, button1.Top),
                Size = new Size(95, button1.Height),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnRefreshStock.Click += BtnRefreshStock_Click;
            Controls.Add(btnRefreshStock);
            btnRefreshStock.BringToFront();
        }

        private void BtnRefreshStock_Click(object sender, EventArgs e)
        {
            LoadInventoryData(true);
        }

        private void dataGridInventory_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // Ensure the row index is valid
            {
                DataGridViewRow row = dataGridInventory.Rows[e.RowIndex];

                // Assuming the columns in the DataGridView are named "Column1", "Column2", "Column3", etc.
                txtItemName.Text = row.Cells["item_name"].Value?.ToString() ?? string.Empty;
                txtAmount.Text = row.Cells["amount"].Value?.ToString() ?? string.Empty;
                txtPrice.Text = row.Cells["retail_price"].Value?.ToString() ?? string.Empty;
                txtAddedBy.Text = row.Cells["added_by"].Value?.ToString() ?? string.Empty;
                txtKeywords.Text = row.Cells["keywords"].Value?.ToString() ?? string.Empty;
                txtBarcode.Text = row.Cells["barcode"].Value?.ToString() ?? string.Empty;
                txtCost.Text = row.Cells["cost"].Value?.ToString() ?? string.Empty;

            }
        }

        private void Inventory_management_Load(object sender, EventArgs e)
        {
            
        }
        private void LoadInventoryData(bool refreshCache = false)
        {
            try
            {
                if (refreshCache)
                {
                    AppCache.Refresh();
                }

                dataGridInventory.DataSource = AppCache.GetInventoryDataTable();
                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while fetching data: " + ex.Message);
            }
        }
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            ApplySearchFilter();
        }

        private void ApplySearchFilter()
        {
            string filterExpression = txtSearch.Text.Trim();
            DataTable inventoryTable = dataGridInventory.DataSource as DataTable;
            if (inventoryTable == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(filterExpression))
            {
                inventoryTable.DefaultView.RowFilter = "";
                return;
            }

            if (!SafeSearchPattern.IsMatch(filterExpression))
            {
                MessageBox.Show("Invalid input. Please use only letters, numbers, and spaces.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            filterExpression = filterExpression.Replace("'", "''");
            inventoryTable.DefaultView.RowFilter =
                string.Format("item_name LIKE '%{0}%' OR keywords LIKE '%{0}%' OR barcode LIKE '%{0}%'", filterExpression);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
        }

        private void dataGridInventory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Home home = System.Windows.Forms.Application.OpenForms.OfType<Home>().FirstOrDefault() ?? new Home();
            home.Show();
            this.Hide();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            string url = "https://www.helakuru.lk/keyboard";

            try
            {
                Process.Start(url);
            }
            catch (Exception ex) {
                MessageBox.Show($"Failed to open Helakuru. Error: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            decimal price, amount, cost = 0m;
            if (!PosNumberParser.TryParseMoney(txtPrice.Text, out price) ||
                !PosNumberParser.TryParseQuantity(txtAmount.Text, out amount, allowZero: true) ||
                (!string.IsNullOrWhiteSpace(txtCost.Text) && !PosNumberParser.TryParseMoney(txtCost.Text, out cost)))
            {
                MessageBox.Show("Enter valid price, stock amount, and cost values.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (MySqlTransaction tx = connection.BeginTransaction())
                    {
                        string query = "INSERT INTO inventory (item_name, retail_price, amount, added_by, keywords, barcode, cost, stock_update_time) VALUES (@name, @price, @amount, @added_by, @keywords, @barcode, @cost, NOW())";
                        using (MySqlCommand command = new MySqlCommand(query, connection, tx))
                        {
                            command.Parameters.AddWithValue("@name", txtItemName.Text);
                            command.Parameters.AddWithValue("@price", price);
                            command.Parameters.AddWithValue("@amount", amount);
                            command.Parameters.AddWithValue("@added_by", txtAddedBy.Text);
                            command.Parameters.AddWithValue("@keywords", KeywordGenerator.MergeWithGenerated(txtKeywords.Text, txtItemName.Text));
                            command.Parameters.AddWithValue("@barcode", txtBarcode.Text);
                            command.Parameters.AddWithValue("@cost", string.IsNullOrWhiteSpace(txtCost.Text) ? (object)DBNull.Value : cost);
                            command.ExecuteNonQuery();

                            if (amount != 0m)
                            {
                                InsertInventoryMovement(connection, tx, Convert.ToInt32(command.LastInsertedId), txtItemName.Text, amount,
                                    amount > 0m ? InventoryMutationRules.PurchaseReceipt : InventoryMutationRules.ManualAdjustmentOut, "Initial inventory quantity");
                            }
                        }

                        tx.Commit();
                        MessageBox.Show("Successfully Added!", " New Item", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }

            LoadInventoryData(true);
            clearTexts();
                
        }

        private void clearTexts()
        {
            txtItemName.Text = string.Empty;
            txtPrice.Text = string.Empty;   
            txtAmount.Text = string.Empty;
            txtAddedBy.Text = string.Empty;
            txtKeywords.Text = string.Empty;
            txtBarcode.Text = string.Empty;
            txtCost.Text = string.Empty;

        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            clearTexts();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to update this item?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        using (MySqlTransaction tx = conn.BeginTransaction())
                        {
                            int itemId = Convert.ToInt32(dataGridInventory.SelectedRows[0].Cells["id"].Value);
                            decimal? oldCost = null;
                            decimal oldAmount = 0m;
                            using (MySqlCommand oldCostCmd = new MySqlCommand("SELECT cost FROM inventory WHERE id = @id", conn, tx))
                            {
                                oldCostCmd.Parameters.AddWithValue("@id", itemId);
                                object oldCostObj = oldCostCmd.ExecuteScalar();
                                if (oldCostObj != null && oldCostObj != DBNull.Value)
                                {
                                    oldCost = Convert.ToDecimal(oldCostObj);
                                }
                            }
                            using (MySqlCommand oldAmountCmd = new MySqlCommand("SELECT amount FROM inventory WHERE id = @id FOR UPDATE", conn, tx))
                            {
                                oldAmountCmd.Parameters.AddWithValue("@id", itemId);
                                object oldAmountObj = oldAmountCmd.ExecuteScalar();
                                if (oldAmountObj != null && oldAmountObj != DBNull.Value)
                                {
                                    oldAmount = Convert.ToDecimal(oldAmountObj);
                                }
                            }

                            decimal? newCost = string.IsNullOrWhiteSpace(txtCost.Text) ? (decimal?)null : PosNumberParser.ParseRequiredMoney(txtCost.Text, "Cost");
                            decimal newAmount = PosNumberParser.ParseRequiredQuantity(txtAmount.Text, "Stock amount", allowZero: true);
                            decimal? changePct = null;
                            bool warningFlagged = false;
                            if (oldCost.HasValue && oldCost.Value != 0m && newCost.HasValue)
                            {
                                changePct = ((newCost.Value - oldCost.Value) / oldCost.Value) * 100m;
                                warningFlagged = Math.Abs(changePct.Value) > 2m;
                            }

                            if (warningFlagged)
                            {
                                DialogResult costWarning = MessageBox.Show(
                                    "Cost change of " + changePct.Value.ToString("N2") + "% detected. Continue?",
                                    "Cost Change Warning",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Warning);
                                if (costWarning != DialogResult.Yes)
                                {
                                    tx.Rollback();
                                    return;
                                }
                            }

                            string sql = "UPDATE `inventory` SET `item_name`=@item_name,`retail_price`=@price,`amount`=@amount,`added_by`=@added_by,`keywords`=@keywords, `barcode`=@barcode, `cost`=@cost  WHERE id = @id";
                            MySqlCommand cmd = new MySqlCommand(sql, conn, tx);
                            cmd.Parameters.AddWithValue("@item_name", txtItemName.Text);
                            cmd.Parameters.AddWithValue("@amount", newAmount);
                            cmd.Parameters.AddWithValue("@price", PosNumberParser.ParseRequiredMoney(txtPrice.Text, "Retail price"));
                            cmd.Parameters.AddWithValue("@added_by", txtAddedBy.Text);
                            cmd.Parameters.AddWithValue("@keywords", KeywordGenerator.MergeWithGenerated(txtKeywords.Text, txtItemName.Text));
                            cmd.Parameters.AddWithValue("@barcode", txtBarcode.Text);
                            cmd.Parameters.AddWithValue("@cost", newCost.HasValue ? (object)newCost.Value : DBNull.Value);
                            cmd.Parameters.AddWithValue("@id", itemId);

                            using (MySqlCommand historyCmd = new MySqlCommand(
                                @"INSERT INTO inventory_cost_history
                                  (item_id, item_name, old_cost, new_cost, change_pct, source,
                                   source_reference_id, changed_at, changed_by_user_id,
                                   changed_by_username, warning_flagged)
                                  VALUES
                                  (@item_id, @item_name, @old_cost, @new_cost, @change_pct, 'desktop_inventory_edit',
                                   NULL, NOW(), NULL, @changed_by_username, @warning_flagged)", conn, tx))
                            {
                                historyCmd.Parameters.AddWithValue("@item_id", itemId);
                                historyCmd.Parameters.AddWithValue("@item_name", txtItemName.Text);
                                historyCmd.Parameters.AddWithValue("@old_cost", oldCost.HasValue ? (object)oldCost.Value : DBNull.Value);
                                historyCmd.Parameters.AddWithValue("@new_cost", newCost.HasValue ? (object)newCost.Value : DBNull.Value);
                                historyCmd.Parameters.AddWithValue("@change_pct", changePct.HasValue ? (object)changePct.Value : DBNull.Value);
                                historyCmd.Parameters.AddWithValue("@changed_by_username", UserSession.Username ?? "desktop-pos");
                                historyCmd.Parameters.AddWithValue("@warning_flagged", warningFlagged ? 1 : 0);
                                historyCmd.ExecuteNonQuery();
                            }

                            cmd.ExecuteNonQuery();
                            decimal delta = newAmount - oldAmount;
                            if (delta != 0m)
                            {
                                InsertInventoryMovement(conn, tx, itemId, txtItemName.Text, delta,
                                    InventoryMutationRules.ManualAdjustmentType(delta), "Desktop inventory edit");
                            }
                            tx.Commit();
                        }

                        MessageBox.Show("Record updated successfully!");
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }

            LoadInventoryData(true);
            clearTexts();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to delete this item?", "Confirmation", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            if (dataGridInventory.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a row to delete.", "Delete Entry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int selectedId = Convert.ToInt32(dataGridInventory.SelectedRows[0].Cells["id"].Value);
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlCommand cmd = new MySqlCommand("DELETE FROM inventory WHERE id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("@id", selectedId);
                    cmd.ExecuteNonQuery();
                }
            }
            LoadInventoryData(true);
            clearTexts();
        }

        private static void InsertInventoryMovement(MySqlConnection conn, MySqlTransaction tx, int itemId, string itemName, decimal qtyDelta, string movementType, string note)
        {
            using (MySqlCommand cmd = new MySqlCommand(
                @"INSERT INTO stock_movement
                  (item_id, item_name, movement_type, qty_delta, reference_type, reference_id,
                   occurred_at, created_at, created_by_user_id, created_by_username, note)
                  VALUES
                  (@item_id, @item_name, @movement_type, @qty_delta, 'inventory_adjustment', @reference_id,
                   NOW(), NOW(), NULL, @created_by_username, @note)", conn, tx))
            {
                cmd.Parameters.AddWithValue("@item_id", itemId);
                cmd.Parameters.AddWithValue("@item_name", itemName ?? string.Empty);
                cmd.Parameters.AddWithValue("@movement_type", movementType);
                cmd.Parameters.AddWithValue("@qty_delta", qtyDelta);
                cmd.Parameters.AddWithValue("@reference_id", itemId);
                cmd.Parameters.AddWithValue("@created_by_username", UserSession.Username ?? "desktop-pos");
                cmd.Parameters.AddWithValue("@note", note ?? string.Empty);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
