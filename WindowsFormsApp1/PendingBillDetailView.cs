using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    /// <summary>
    /// Detail dialog for a single pending bill.
    /// Opened by double-clicking a row in PendingBillsInbox.
    /// After closing, check BillWasRemoved / NavigatedToBilling.
    /// </summary>
    public class PendingBillDetailView : Form
    {
        // ── State ─────────────────────────────────────────────────────────────────
        private readonly int pendingBillId;
        private readonly string sessionId;
        private readonly string cashierCode;
        private readonly DateTime createdAt;
        private readonly string note;
        private readonly string cancelledStatus;
        private readonly string connectionString = DatabaseConfig.ConnectionString;
        private readonly List<PendingBillItemRow> items = new List<PendingBillItemRow>();
        private string _printSubmissionId;

        /// <summary>True when the bill was printed or cancelled (list should refresh).</summary>
        public bool BillWasRemoved { get; private set; }

        /// <summary>True when the user chose "Move to Billing" (inbox should close too).</summary>
        public bool NavigatedToBilling { get; private set; }

        // ── UI controls (need field access for data binding) ──────────────────────
        private DataGridView dataGridItems;
        private Label lblTotal;
        private Label lblDiscount;
        private Label lblGrandTotal;

        // ── Constructor ───────────────────────────────────────────────────────────
        public PendingBillDetailView(int pendingBillId, string sessionId, string cashierCode, DateTime createdAt, string note, string cancelledStatus)
        {
            this.pendingBillId = pendingBillId;
            this.sessionId = sessionId;
            this.cashierCode = cashierCode;
            this.createdAt = createdAt;
            this.note = note;
            this.cancelledStatus = cancelledStatus;

            BuildUi();
            Load += (s, e) => LoadItems();
        }

        // ── UI construction (mirrors BillDetailView layout & fonts) ───────────────
        private void BuildUi()
        {
            Text = "Pending Bill Details";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1000, 700);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // ── Header row 1: Cashier ─────────────────────────────────────────────
            AddLabel("Cashier", new Font("Microsoft Sans Serif", 10F), SystemColors.GrayText, new Point(25, 15));

            AddLabel(
                string.IsNullOrWhiteSpace(cashierCode) ? "(unknown)" : cashierCode,
                new Font("Microsoft Sans Serif", 22F, FontStyle.Bold),
                SystemColors.ControlText,
                new Point(22, 38));

            // ── Header row 2: Created At ──────────────────────────────────────────
            AddLabel("Created At", new Font("Microsoft Sans Serif", 10F), SystemColors.GrayText, new Point(330, 15));

            AddLabel(
                createdAt.ToString("yyyy-MM-dd  hh:mm tt"),
                new Font("Microsoft Sans Serif", 14F),
                SystemColors.ControlText,
                new Point(328, 42));

            // ── Header row 3: Note ────────────────────────────────────────────────
            AddLabel("Note", new Font("Microsoft Sans Serif", 10F), SystemColors.GrayText, new Point(700, 15));

            AddLabel(
                string.IsNullOrWhiteSpace(note) ? "\u2014" : note,
                new Font("Microsoft Sans Serif", 14F, FontStyle.Bold),
                SystemColors.ControlText,
                new Point(698, 42));

            // ── Items grid ────────────────────────────────────────────────────────
            dataGridItems = new DataGridView
            {
                BorderStyle = BorderStyle.Fixed3D,
                Location = new Point(25, 95),
                Size = new Size(940, 400),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                MultiSelect = false,
                Font = new Font("Arial", 12),
                RowTemplate = { Height = 32 }
            };
            Controls.Add(dataGridItems);

            // ── Summary area (identical positions to BillDetailView) ──────────────

            // Total
            AddLabel("Total",  new Font("Microsoft Sans Serif", 12F), SystemColors.ControlText, new Point(25, 510));
            AddLabel("Rs.",    new Font("Microsoft Sans Serif", 12F), SystemColors.ControlText, new Point(25, 540));
            lblTotal = new Label { Font = new Font("Microsoft Sans Serif", 16F, FontStyle.Bold), AutoSize = true, Location = new Point(75, 535) };
            Controls.Add(lblTotal);

            // Discount
            AddLabel("Discount", new Font("Microsoft Sans Serif", 12F, FontStyle.Bold), SystemColors.ControlText, new Point(310, 510));
            AddLabel("Rs.",      new Font("Microsoft Sans Serif", 12F),                SystemColors.ControlText, new Point(310, 540));
            lblDiscount = new Label { Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold), AutoSize = true, Location = new Point(360, 537) };
            Controls.Add(lblDiscount);

            // Grand Total
            AddLabel("Grand Total", new Font("Microsoft Sans Serif", 13.8F, FontStyle.Bold), SystemColors.ControlText, new Point(600, 508));
            AddLabel("Rs.",         new Font("Microsoft Sans Serif", 14F),                   SystemColors.ControlText, new Point(600, 542));
            lblGrandTotal = new Label { Font = new Font("Microsoft Sans Serif", 22F, FontStyle.Bold), AutoSize = true, Location = new Point(655, 533) };
            Controls.Add(lblGrandTotal);

            // ── Action buttons ────────────────────────────────────────────────────
            // Print Now  (green — like Reprint in BillDetailView)
            Button btnPrint = MakeButton("Print Now", Color.LimeGreen, Color.Black,
                new Point(25, 610), new Size(210, 65));
            btnPrint.Click += BtnPrint_Click;
            Controls.Add(btnPrint);

            // Move to Billing  (blue)
            Button btnMoveToBilling = MakeButton("Move to Billing", Color.DodgerBlue, Color.White,
                new Point(248, 610), new Size(210, 65));
            btnMoveToBilling.Click += BtnMoveToBilling_Click;
            Controls.Add(btnMoveToBilling);

            // Cancel Bill  (red)
            Button btnCancel = MakeButton("Cancel Bill", Color.FromArgb(198, 42, 42), Color.White,
                new Point(471, 610), new Size(210, 65));
            btnCancel.Click += BtnCancel_Click;
            Controls.Add(btnCancel);

            // Close
            Button btnClose = new Button
            {
                Text = "Close",
                Font = new Font("Microsoft Sans Serif", 13.8F, FontStyle.Bold),
                Location = new Point(735, 610),
                Size = new Size(210, 65),
                UseVisualStyleBackColor = true
            };
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);
        }

        private void AddLabel(string text, Font font, Color foreColor, Point location)
        {
            Controls.Add(new Label
            {
                Text = text,
                Font = font,
                ForeColor = foreColor,
                AutoSize = true,
                Location = location
            });
        }

        private static Button MakeButton(string text, Color backColor, Color foreColor, Point location, Size size)
        {
            return new Button
            {
                Text = text,
                BackColor = backColor,
                ForeColor = foreColor,
                Font = new Font("Microsoft Sans Serif", 13.8F, FontStyle.Bold),
                Location = location,
                Size = size,
                UseVisualStyleBackColor = false
            };
        }

        // ── Load items from DB ────────────────────────────────────────────────────
        private void LoadItems()
        {
            try
            {
                DataTable itemsTable = new DataTable();
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT item_name, rate, amount, discounted_price " +
                                   "FROM pending_bill_items WHERE pending_bill_id = @id ORDER BY id ASC";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", pendingBillId);
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                            adapter.Fill(itemsTable);
                    }
                }

                foreach (DataRow row in itemsTable.Rows)
                {
                    items.Add(new PendingBillItemRow
                    {
                        ItemName = row["item_name"]?.ToString() ?? string.Empty,
                        Rate     = ToDecimal(row["rate"]),
                        Qty      = ToDecimal(row["amount"]),
                        Price    = ToDecimal(row["discounted_price"])
                    });
                }

                BindGrid();
                UpdateSummary();
                Text = "Pending Bill — " + (string.IsNullOrWhiteSpace(cashierCode) ? "#" + pendingBillId : cashierCode);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading bill items: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BindGrid()
        {
            DataTable table = new DataTable();
            table.Columns.Add("Item Name",  typeof(string));
            table.Columns.Add("Rate",       typeof(decimal));
            table.Columns.Add("Qty",        typeof(decimal));
            table.Columns.Add("Price",      typeof(decimal));

            foreach (PendingBillItemRow item in items)
                table.Rows.Add(item.ItemName, item.Rate, item.Qty, item.Price);

            dataGridItems.DataSource = table;
            FormatGrid();
        }

        private void FormatGrid()
        {
            if (dataGridItems.Columns.Count == 0) return;

            var colName = dataGridItems.Columns["Item Name"];
            if (colName != null)
            {
                colName.HeaderText = "Item Name";
                colName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            FormatCol("Rate", "Rate (Rs.)",   140, "N2", DataGridViewContentAlignment.MiddleRight);
            FormatCol("Qty",  "Qty (kg/pcs)", 130, null, DataGridViewContentAlignment.MiddleCenter);

            var colPrice = dataGridItems.Columns["Price"];
            if (colPrice != null)
            {
                colPrice.HeaderText = "Price (Rs.)";
                colPrice.Width = 150;
                colPrice.DefaultCellStyle.Format = "N2";
                colPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                colPrice.DefaultCellStyle.Font = new Font("Arial", 12, FontStyle.Bold);
            }
        }

        private void FormatCol(string name, string header, int width, string format, DataGridViewContentAlignment alignment)
        {
            var col = dataGridItems.Columns[name];
            if (col == null) return;
            col.HeaderText = header;
            col.Width = width;
            if (format != null) col.DefaultCellStyle.Format = format;
            col.DefaultCellStyle.Alignment = alignment;
        }

        private void UpdateSummary()
        {
            decimal total      = items.Sum(i => i.Rate * i.Qty);
            decimal grandTotal = items.Sum(i => i.Price);
            decimal discount   = total - grandTotal;

            lblTotal.Text      = total.ToString("N2");
            lblDiscount.Text   = discount.ToString("N2");
            lblGrandTotal.Text = grandTotal.ToString("N2");
        }

        // ── Button handlers ───────────────────────────────────────────────────────
        private void BtnPrint_Click(object sender, EventArgs e)
        {
            if (items.Count == 0)
            {
                MessageBox.Show("No items to print.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string code = cashierCode;
            if (string.IsNullOrWhiteSpace(code)) code = PromptForEmployeeCode(cashierCode);
            if (string.IsNullOrWhiteSpace(code))
            {
                MessageBox.Show("Please enter the salesperson's name to proceed.",
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            decimal grandTotalPreview = items.Sum(i => i.Price);
            PaymentMethodDialog paymentDlg = new PaymentMethodDialog(grandTotalPreview);
            if (paymentDlg.ShowDialog() != DialogResult.OK)
            {
                paymentDlg.Dispose();
                return;
            }
            string paymentMethod  = paymentDlg.SelectedPaymentMethod;
            int creditAccountId   = paymentDlg.SelectedCreditAccountId;
            paymentDlg.Dispose();

            if (_printSubmissionId == null)
                _printSubmissionId = Guid.NewGuid().ToString();

            try
            {
                DesktopPosLocalAudit.SavePendingSnapshot(GetSnapshotSessionId(), items.Select(item => new PendingBillSnapshotLine
                {
                    ItemName = item.ItemName,
                    Rate = item.Rate,
                    Amount = item.Qty,
                    DiscountedPrice = item.Price
                }));

                DataGridView printGrid = BuildTemporaryGrid();
                decimal totalAmount    = items.Sum(i => i.Rate * i.Qty);
                decimal discountAmount = totalAmount - items.Sum(i => i.Price);
                decimal grandTotal     = totalAmount - discountAmount;

                string billCode;
                try
                {
                    DataTable dt = BuildItemsDataTable();
                    billCode = BillHistoryManager.SaveBillFromDataTable(code, totalAmount, discountAmount, dt, _printSubmissionId, paymentMethod);
                }
                catch (Exception ex)
                {
                    DialogResult fallbackChoice = MessageBox.Show(
                        "Bill save failed.\n\n"
                        + ex.Message
                        + "\n\nQueue this bill offline and print with a LOCAL code instead?",
                        "Database Save Failed",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error);

                    if (fallbackChoice != DialogResult.Yes)
                    {
                        throw;
                    }

                    FallbackBillLogger.LogFailedBill(printGrid, code, totalAmount, discountAmount, null);
                    billCode = "LOCAL-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                }

                if (paymentMethod == "CREDIT" && creditAccountId > 0 && !billCode.StartsWith("LOCAL-"))
                {
                    try
                    {
                        CreditManager.AddTransaction(creditAccountId, "BILL", grandTotal, "DEBIT",
                            "POS sale (pending bill)", billCode, DateTime.Today);
                    }
                    catch { }
                }

                new PDFConverter().ConvertPrintDocumentToPdf(printGrid, code, totalAmount, discountAmount, billCode, createdAt);

                RemovePendingBill();
                BillWasRemoved = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to print: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnMoveToBilling_Click(object sender, EventArgs e)
        {
            if (items.Count == 0)
            {
                MessageBox.Show("No items to load.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            billing billingForm = Application.OpenForms.OfType<billing>().FirstOrDefault();
            if (billingForm == null || billingForm.IsDisposed)
            {
                billingForm = new billing();
                billingForm.Show();
            }
            else if (!billingForm.Visible)
            {
                billingForm.Show();
            }

            if (billingForm.BillItemCount > 0)
            {
                if (MessageBox.Show(
                    "Current bill has items. Loading this pending bill will clear them. Continue?",
                    "Pending Bills", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.No)
                    return;
            }

            try
            {
                DesktopPosLocalAudit.SavePendingSnapshot(GetSnapshotSessionId(), items.Select(item => new PendingBillSnapshotLine
                {
                    ItemName = item.ItemName,
                    Rate = item.Rate,
                    Amount = item.Qty,
                    DiscountedPrice = item.Price
                }));

                billingForm.ClearCurrentBillForPendingLoad();
                foreach (PendingBillItemRow item in items)
                    billingForm.AddBillItem(item.ItemName, item.Rate, item.Qty, item.Price);
                billingForm.SetPendingBillContext(cashierCode, GetSnapshotSessionId());

                RemovePendingBill();

                billingForm.WindowState = FormWindowState.Normal;
                billingForm.BringToFront();
                billingForm.Focus();

                Home homeForm = Application.OpenForms.OfType<Home>().FirstOrDefault();
                if (homeForm != null) homeForm.Hide();

                NavigatedToBilling = true;
                BillWasRemoved = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load bill into billing: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show(
                "Cancel this bill? The cashier will need to re-enter it.",
                "Pending Bills", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "UPDATE pending_bill SET status = @status, updated_at = NOW() WHERE id = @id";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@status", cancelledStatus);
                        cmd.Parameters.AddWithValue("@id", pendingBillId);
                        cmd.ExecuteNonQuery();
                    }
                }

                BillWasRemoved = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to cancel bill: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────────
        private void RemovePendingBill()
        {
            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                using (MySqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        using (MySqlCommand cmd = new MySqlCommand(
                            "DELETE FROM pending_bill_items WHERE pending_bill_id = @id", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", pendingBillId);
                            cmd.ExecuteNonQuery();
                        }
                        using (MySqlCommand cmd = new MySqlCommand(
                            "DELETE FROM pending_bill WHERE id = @id", conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@id", pendingBillId);
                            cmd.ExecuteNonQuery();
                        }
                        tx.Commit();
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }

        private string GetSnapshotSessionId()
        {
            return string.IsNullOrWhiteSpace(sessionId) ? "pending-" + pendingBillId : sessionId;
        }

        private string PromptForEmployeeCode(string preferredEmployeeCode)
        {
            using (emp_selection form = new emp_selection(preferredEmployeeCode))
            {
                if (form.ShowDialog() == DialogResult.OK)
                    return form.SelectedEmployee;
            }
            return null;
        }

        private DataGridView BuildTemporaryGrid()
        {
            DataGridView grid = new DataGridView();
            grid.Columns.Add("id", "id");
            grid.Columns.Add("\u00edtem_name", "\u00edtem_name");
            grid.Columns.Add("rate", "rate");
            grid.Columns.Add("amount", "amount");
            grid.Columns.Add("discounted_price", "discounted_price");

            int rowId = 1;
            foreach (PendingBillItemRow item in items)
                grid.Rows.Add(rowId++, item.ItemName, (float)item.Rate, (float)item.Qty, (float)item.Price);

            return grid;
        }

        private DataTable BuildItemsDataTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("item_name",        typeof(string));
            table.Columns.Add("rate",              typeof(decimal));
            table.Columns.Add("amount",            typeof(decimal));
            table.Columns.Add("discounted_price",  typeof(decimal));

            foreach (PendingBillItemRow item in items)
                table.Rows.Add(item.ItemName, item.Rate, item.Qty, item.Price);

            return table;
        }

        private static decimal ToDecimal(object value)
        {
            if (value == null || value == DBNull.Value) return 0m;
            return decimal.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal d) ? d : 0m;
        }

        private class PendingBillItemRow
        {
            public string  ItemName { get; set; }
            public decimal Rate     { get; set; }
            public decimal Qty      { get; set; }
            public decimal Price    { get; set; }
        }
    }
}
