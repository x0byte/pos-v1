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
    public class PendingBillsInbox : Form
    {
        private readonly string connectionString = DatabaseConfig.ConnectionString;
        private const int DesiredLeftMinSize = 520;
        private const int DesiredRightMinSize = 360;
        private SplitContainer mainSplitContainer;

        private readonly DataGridView dgvPendingBills = new DataGridView();
        private readonly DataGridView dgvItems = new DataGridView();
        private readonly Button btnRefresh = new Button();
        private readonly Button btnLoadToBilling = new Button();
        private readonly Button btnPrintNow = new Button();
        private readonly Button btnCancelBill = new Button();
        private readonly Label lblTotalValue = new Label();
        private readonly Label lblGrandTotalValue = new Label();
        private readonly Label lblDiscountValue = new Label();

        private readonly List<PendingBillItemRow> currentItems = new List<PendingBillItemRow>();
        private Label lblWaitingHeading = new Label();

        private List<string> waitingStatuses = new List<string> { "pending" };
        private List<string> fallbackVisibleStatuses = new List<string>();
        private string cancelledStatus;

        public PendingBillsInbox()
        {
            InitializeUi();
            Load += PendingBillsInbox_Load;
            Shown += PendingBillsInbox_Shown;
            Resize += PendingBillsInbox_Resize;
        }

        private void PendingBillsInbox_Load(object sender, EventArgs e)
        {
            DetectStatusConventions();
            LoadPendingBills();
        }

        private void InitializeUi()
        {
            Text = "Pending Bills Inbox";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1280, 760);
            MinimumSize = new Size(1080, 680);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            MinimizeBox = true;
            BackColor = Color.FromArgb(240, 242, 246);
            Font = new Font("Segoe UI", 9.5F);

            // ── Header bar ─────────────────────────────────────────────────────
            Panel headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 62,
                BackColor = Color.FromArgb(26, 43, 74)
            };
            Controls.Add(headerPanel);

            TableLayoutPanel headerLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Color.Transparent
            };
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
            headerPanel.Controls.Add(headerLayout);

            Label lblTitle = new Label
            {
                Text = "   Pending Bills",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            headerLayout.Controls.Add(lblTitle, 0, 0);

            StyleButton(btnRefresh, "⟳   Refresh", Color.FromArgb(50, 70, 108), Color.White);
            btnRefresh.Dock = DockStyle.Fill;
            btnRefresh.Margin = new Padding(6, 12, 12, 12);
            btnRefresh.Click += (s, e) => LoadPendingBills();
            headerLayout.Controls.Add(btnRefresh, 1, 0);

            // ── Main split container ────────────────────────────────────────────
            mainSplitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterWidth = 6,
                BackColor = Color.FromArgb(200, 205, 218)
            };
            Controls.Add(mainSplitContainer);

            // ── Left panel: bill list ───────────────────────────────────────────
            mainSplitContainer.Panel1.BackColor = Color.FromArgb(240, 242, 246);
            mainSplitContainer.Panel1.Padding = new Padding(14, 12, 7, 14);

            lblWaitingHeading.Text = "Waiting Bills";
            lblWaitingHeading.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblWaitingHeading.ForeColor = Color.FromArgb(26, 43, 74);
            lblWaitingHeading.Dock = DockStyle.Top;
            lblWaitingHeading.Height = 40;
            lblWaitingHeading.TextAlign = ContentAlignment.MiddleLeft;
            mainSplitContainer.Panel1.Controls.Add(lblWaitingHeading);

            StyleDataGrid(dgvPendingBills, isMainGrid: true);
            dgvPendingBills.Dock = DockStyle.Fill;
            dgvPendingBills.SelectionChanged += DgvPendingBills_SelectionChanged;
            mainSplitContainer.Panel1.Controls.Add(dgvPendingBills);
            dgvPendingBills.BringToFront();

            // ── Right panel: detail + summary + actions ─────────────────────────
            mainSplitContainer.Panel2.BackColor = Color.FromArgb(240, 242, 246);
            mainSplitContainer.Panel2.Padding = new Padding(7, 12, 14, 14);

            TableLayoutPanel rightLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4
            };
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));   // heading
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // items grid
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 122F));  // summary card
            rightLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 188F));  // action buttons
            mainSplitContainer.Panel2.Controls.Add(rightLayout);

            Label lblDetails = new Label
            {
                Text = "Bill Details",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(26, 43, 74),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            rightLayout.Controls.Add(lblDetails, 0, 0);

            StyleDataGrid(dgvItems, isMainGrid: false);
            dgvItems.Dock = DockStyle.Fill;
            rightLayout.Controls.Add(dgvItems, 0, 1);

            rightLayout.Controls.Add(BuildSummaryCard(), 0, 2);
            rightLayout.Controls.Add(BuildActionPanel(), 0, 3);

            ResetDetailPanel();
        }

        private Panel BuildSummaryCard()
        {
            Panel card = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(18, 10, 18, 10),
                Margin = new Padding(0, 6, 0, 0)
            };
            card.Paint += (s, pe) =>
            {
                using (Pen pen = new Pen(Color.FromArgb(215, 220, 235), 1))
                    pe.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            };

            TableLayoutPanel tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                BackColor = Color.Transparent
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            for (int i = 0; i < 3; i++)
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
            card.Controls.Add(tbl);

            Font labelFont = new Font("Segoe UI", 10.5F);
            Font valueFont = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            Color labelColor = Color.FromArgb(100, 108, 125);

            tbl.Controls.Add(MakeSummaryLabel("Total:", labelFont, labelColor), 0, 0);
            lblTotalValue.Dock = DockStyle.Fill;
            lblTotalValue.TextAlign = ContentAlignment.MiddleLeft;
            lblTotalValue.Font = valueFont;
            lblTotalValue.ForeColor = Color.FromArgb(35, 40, 55);
            tbl.Controls.Add(lblTotalValue, 1, 0);

            tbl.Controls.Add(MakeSummaryLabel("Discount:", labelFont, labelColor), 0, 1);
            lblDiscountValue.Dock = DockStyle.Fill;
            lblDiscountValue.TextAlign = ContentAlignment.MiddleLeft;
            lblDiscountValue.Font = valueFont;
            lblDiscountValue.ForeColor = Color.FromArgb(195, 95, 10);
            tbl.Controls.Add(lblDiscountValue, 1, 1);

            tbl.Controls.Add(MakeSummaryLabel("Grand Total:", new Font("Segoe UI", 11.5F, FontStyle.Bold), Color.FromArgb(26, 43, 74)), 0, 2);
            lblGrandTotalValue.Dock = DockStyle.Fill;
            lblGrandTotalValue.TextAlign = ContentAlignment.MiddleLeft;
            lblGrandTotalValue.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblGrandTotalValue.ForeColor = Color.FromArgb(18, 128, 70);
            tbl.Controls.Add(lblGrandTotalValue, 1, 2);

            return card;
        }

        private Panel BuildActionPanel()
        {
            Panel panel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0),
                BackColor = Color.FromArgb(240, 242, 246)
            };

            TableLayoutPanel tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
            tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 33.3F));
            panel.Controls.Add(tbl);

            StyleButton(btnLoadToBilling, "   Load & Edit in Billing", Color.FromArgb(33, 100, 200), Color.White);
            btnLoadToBilling.Dock = DockStyle.Fill;
            btnLoadToBilling.Margin = new Padding(0, 0, 0, 6);
            btnLoadToBilling.Click += BtnLoadToBilling_Click;
            tbl.Controls.Add(btnLoadToBilling, 0, 0);

            StyleButton(btnPrintNow, "   Print Now", Color.FromArgb(34, 145, 85), Color.White);
            btnPrintNow.Dock = DockStyle.Fill;
            btnPrintNow.Margin = new Padding(0, 0, 0, 6);
            btnPrintNow.Click += BtnPrintNow_Click;
            tbl.Controls.Add(btnPrintNow, 0, 1);

            StyleButton(btnCancelBill, "   Cancel This Bill", Color.FromArgb(198, 42, 42), Color.White);
            btnCancelBill.Dock = DockStyle.Fill;
            btnCancelBill.Click += BtnCancelBill_Click;
            tbl.Controls.Add(btnCancelBill, 0, 2);

            return panel;
        }

        private static Label MakeSummaryLabel(string text, Font font, Color color)
        {
            return new Label
            {
                Text = text,
                Font = font,
                ForeColor = color,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
        }

        private static void StyleButton(Button btn, string text, Color backColor, Color foreColor)
        {
            btn.Text = text;
            btn.BackColor = backColor;
            btn.ForeColor = foreColor;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 11.5F, FontStyle.Bold);
            btn.Cursor = Cursors.Hand;
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.UseVisualStyleBackColor = false;
        }

        private static void StyleDataGrid(DataGridView dgv, bool isMainGrid)
        {
            dgv.ReadOnly = true;
            dgv.MultiSelect = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.AllowUserToAddRows = false;
            dgv.AllowUserToDeleteRows = false;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgv.RowHeadersVisible = false;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Color.FromArgb(228, 232, 240);
            dgv.BackgroundColor = Color.White;

            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(26, 43, 74);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            dgv.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
            dgv.ColumnHeadersHeight = 40;
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            dgv.DefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.ForeColor = Color.FromArgb(35, 40, 55);
            dgv.DefaultCellStyle.Font = new Font("Segoe UI", 10F);
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 226, 255);
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(26, 43, 74);
            dgv.DefaultCellStyle.Padding = new Padding(10, 0, 0, 0);
            dgv.RowTemplate.Height = 40;

            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(246, 248, 253);
            dgv.AlternatingRowsDefaultCellStyle.ForeColor = Color.FromArgb(35, 40, 55);
            dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(210, 226, 255);
            dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(26, 43, 74);
        }

        private void PendingBillsInbox_Shown(object sender, EventArgs e)
        {
            ApplySafeSplitterDistance();
        }

        private void PendingBillsInbox_Resize(object sender, EventArgs e)
        {
            ApplySafeSplitterDistance();
        }

        private void ApplySafeSplitterDistance()
        {
            if (mainSplitContainer == null)
            {
                return;
            }

            int availableWidth = mainSplitContainer.ClientSize.Width;
            if (availableWidth <= 0)
            {
                return;
            }

            int splitterWidth = mainSplitContainer.SplitterWidth;
            int minLeft = DesiredLeftMinSize;
            int minRight = DesiredRightMinSize;

            // If the window is too narrow, relax mins to keep SplitContainer valid.
            int minimumRequired = minLeft + minRight + splitterWidth;
            if (minimumRequired > availableWidth)
            {
                int slack = Math.Max(availableWidth - splitterWidth, 200);
                minLeft = Math.Max(120, (int)(slack * 0.58));
                minRight = Math.Max(120, slack - minLeft);
            }

            int maxLeft = availableWidth - minRight - splitterWidth;

            if (maxLeft < minLeft)
            {
                return;
            }

            int targetLeft = (int)(availableWidth * 0.64);
            if (targetLeft < minLeft) targetLeft = minLeft;
            if (targetLeft > maxLeft) targetLeft = maxLeft;

            // Set constraints in a safe order to avoid WinForms invalid range exceptions.
            mainSplitContainer.Panel1MinSize = 0;
            mainSplitContainer.Panel2MinSize = 0;
            mainSplitContainer.SplitterDistance = targetLeft;
            mainSplitContainer.Panel1MinSize = minLeft;
            mainSplitContainer.Panel2MinSize = minRight;
        }

        private void DetectStatusConventions()
        {
            try
            {
                HashSet<string> statuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT DISTINCT LOWER(TRIM(CAST(status AS CHAR))) AS normalized_status FROM pending_bill WHERE status IS NOT NULL";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string value = reader["normalized_status"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                statuses.Add(value);
                            }
                        }
                    }
                }

                string[] waitingCandidates = { "pending", "waiting", "awaiting_admin", "awaiting", "new" };
                waitingStatuses = waitingCandidates.Where(statuses.Contains).ToList();
                if (waitingStatuses.Count == 0)
                {
                    waitingStatuses.Add("pending");
                }

                string[] terminalStatuses = { "cancelled", "canceled", "inactive", "completed", "complete", "done", "printed", "closed", "in_review", "claimed", "loaded", "loaded_for_edit" };
                fallbackVisibleStatuses = statuses
                    .Where(status => !terminalStatuses.Contains(status, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (statuses.Contains("cancelled"))
                {
                    cancelledStatus = "cancelled";
                }
                else if (statuses.Contains("canceled"))
                {
                    cancelledStatus = "canceled";
                }
                else if (statuses.Contains("inactive"))
                {
                    cancelledStatus = "inactive";
                }
                else
                {
                    cancelledStatus = "cancelled";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to detect pending bill statuses: " + ex.Message, "Pending Bills", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadPendingBills()
        {
            try
            {
                DataTable table = new DataTable();
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string inClause = string.Join(", ", waitingStatuses.Select((_, index) => "@status" + index));
                    string query =
                        "SELECT pb.id, pb.session_id, pb.cashier_code, pb.created_at, " +
                        "COALESCE(COUNT(pbi.id), 0) AS items_count, pb.note " +
                        "FROM pending_bill pb " +
                        "LEFT JOIN pending_bill_items pbi ON pbi.pending_bill_id = pb.id " +
                        "WHERE (pb.status IS NULL OR TRIM(CAST(pb.status AS CHAR)) = '' OR LOWER(TRIM(CAST(pb.status AS CHAR))) IN (" + inClause + ")) " +
                        "GROUP BY pb.id, pb.session_id, pb.cashier_code, pb.created_at, pb.note " +
                        "ORDER BY pb.created_at DESC";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        for (int i = 0; i < waitingStatuses.Count; i++)
                        {
                            command.Parameters.AddWithValue("@status" + i, waitingStatuses[i]);
                        }

                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(table);
                        }
                    }
                }

                if (table.Rows.Count == 0 && fallbackVisibleStatuses.Count > 0)
                {
                    table = LoadPendingBillsWithFallbackStatuses();
                }

                dgvPendingBills.DataSource = table;
                ConfigurePendingBillsGrid();
                int count = table.Rows.Count;
                lblWaitingHeading.Text = count == 0
                    ? "Waiting Bills"
                    : (count == 1 ? "Waiting Bills  (1 bill)" : "Waiting Bills  (" + count + " bills)");

                if (dgvPendingBills.Rows.Count == 0)
                {
                    ResetDetailPanel();
                }
                else
                {
                    dgvPendingBills.Rows[0].Selected = true;
                    LoadSelectedBillDetails();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load pending bills: " + ex.Message, "Pending Bills", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataTable LoadPendingBillsWithFallbackStatuses()
        {
            DataTable table = new DataTable();
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string inClause = string.Join(", ", fallbackVisibleStatuses.Select((_, index) => "@fallbackStatus" + index));
                string query =
                    "SELECT pb.id, pb.session_id, pb.cashier_code, pb.created_at, " +
                    "COALESCE(COUNT(pbi.id), 0) AS items_count, pb.note " +
                    "FROM pending_bill pb " +
                    "LEFT JOIN pending_bill_items pbi ON pbi.pending_bill_id = pb.id " +
                    "WHERE LOWER(TRIM(CAST(pb.status AS CHAR))) IN (" + inClause + ") " +
                    "GROUP BY pb.id, pb.session_id, pb.cashier_code, pb.created_at, pb.note " +
                    "ORDER BY pb.created_at DESC";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    for (int i = 0; i < fallbackVisibleStatuses.Count; i++)
                    {
                        command.Parameters.AddWithValue("@fallbackStatus" + i, fallbackVisibleStatuses[i]);
                    }

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                    {
                        adapter.Fill(table);
                    }
                }
            }

            return table;
        }

        private void ConfigurePendingBillsGrid()
        {
            if (dgvPendingBills.Columns.Contains("id")) dgvPendingBills.Columns["id"].HeaderText = "Bill ID";
            if (dgvPendingBills.Columns.Contains("session_id")) dgvPendingBills.Columns["session_id"].HeaderText = "Session ID";
            if (dgvPendingBills.Columns.Contains("cashier_code")) dgvPendingBills.Columns["cashier_code"].HeaderText = "Cashier Code";
            if (dgvPendingBills.Columns.Contains("created_at")) dgvPendingBills.Columns["created_at"].HeaderText = "Created At";
            if (dgvPendingBills.Columns.Contains("items_count")) dgvPendingBills.Columns["items_count"].HeaderText = "Items Count";
            if (dgvPendingBills.Columns.Contains("note")) dgvPendingBills.Columns["note"].HeaderText = "Note";
        }

        private void DgvPendingBills_SelectionChanged(object sender, EventArgs e)
        {
            LoadSelectedBillDetails();
        }

        private void LoadSelectedBillDetails()
        {
            if (dgvPendingBills.SelectedRows.Count == 0 || dgvPendingBills.SelectedRows[0].Cells["id"].Value == null)
            {
                ResetDetailPanel();
                return;
            }

            int billId = Convert.ToInt32(dgvPendingBills.SelectedRows[0].Cells["id"].Value);

            try
            {
                currentItems.Clear();
                DataTable itemsTable = new DataTable();
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT item_name, rate, amount, discounted_price FROM pending_bill_items WHERE pending_bill_id = @pending_bill_id ORDER BY id ASC";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@pending_bill_id", billId);
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        {
                            adapter.Fill(itemsTable);
                        }
                    }
                }

                foreach (DataRow row in itemsTable.Rows)
                {
                    PendingBillItemRow item = new PendingBillItemRow
                    {
                        ItemName = row["item_name"]?.ToString() ?? string.Empty,
                        Rate = ConvertToFloat(row["rate"]),
                        Qty = ConvertToFloat(row["amount"]),
                        Price = ConvertToFloat(row["discounted_price"])
                    };
                    currentItems.Add(item);
                }

                BindItemsGrid(currentItems);
                UpdateSummaryValues(currentItems);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load pending bill items: " + ex.Message, "Pending Bills", MessageBoxButtons.OK, MessageBoxIcon.Error);
                ResetDetailPanel();
            }
        }

        private void BindItemsGrid(List<PendingBillItemRow> items)
        {
            DataTable table = new DataTable();
            table.Columns.Add("Item Name", typeof(string));
            table.Columns.Add("Rate", typeof(decimal));
            table.Columns.Add("Qty", typeof(decimal));
            table.Columns.Add("Price", typeof(decimal));

            foreach (PendingBillItemRow item in items)
            {
                table.Rows.Add(item.ItemName, item.Rate, item.Qty, item.Price);
            }

            dgvItems.DataSource = table;
        }

        private void UpdateSummaryValues(List<PendingBillItemRow> items)
        {
            decimal total = items.Sum(item => Convert.ToDecimal(item.Rate) * Convert.ToDecimal(item.Qty));
            decimal grandTotal = items.Sum(item => Convert.ToDecimal(item.Price));
            decimal discount = total - grandTotal;

            lblTotalValue.Text = total.ToString("0.00", CultureInfo.InvariantCulture);
            lblGrandTotalValue.Text = grandTotal.ToString("0.00", CultureInfo.InvariantCulture);
            lblDiscountValue.Text = discount.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private void ResetDetailPanel()
        {
            currentItems.Clear();
            dgvItems.DataSource = null;
            lblTotalValue.Text = "0.00";
            lblGrandTotalValue.Text = "0.00";
            lblDiscountValue.Text = "0.00";
        }

        private void BtnLoadToBilling_Click(object sender, EventArgs e)
        {
            if (dgvPendingBills.SelectedRows.Count == 0 || dgvPendingBills.SelectedRows[0].Cells["id"].Value == null)
            {
                MessageBox.Show("Please select a pending bill first.", "Pending Bills", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (currentItems.Count == 0)
            {
                MessageBox.Show("Selected pending bill has no items to load.", "Pending Bills", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int pendingBillId = Convert.ToInt32(dgvPendingBills.SelectedRows[0].Cells["id"].Value);
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
                DialogResult overwriteResult = MessageBox.Show(
                    "Current bill has items. Loading this pending bill will clear them. Continue?",
                    "Pending Bills",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (overwriteResult == DialogResult.No)
                {
                    return;
                }
            }

            try
            {
                billingForm.ClearCurrentBillForPendingLoad();

                foreach (PendingBillItemRow item in currentItems)
                {
                    billingForm.AddBillItem(item.ItemName, item.Rate, item.Qty, item.Price);
                }

                RemovePrintedPendingBill(pendingBillId);

                billingForm.WindowState = FormWindowState.Normal;
                billingForm.BringToFront();
                billingForm.Focus();
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load bill into billing: " + ex.Message, "Pending Bills", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnPrintNow_Click(object sender, EventArgs e)
        {
            if (dgvPendingBills.SelectedRows.Count == 0 || dgvPendingBills.SelectedRows[0].Cells["id"].Value == null)
            {
                MessageBox.Show("Please select a pending bill first.", "Pending Bills", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (currentItems.Count == 0)
            {
                MessageBox.Show("Selected pending bill has no items to print.", "Pending Bills", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int pendingBillId = Convert.ToInt32(dgvPendingBills.SelectedRows[0].Cells["id"].Value);
            string cashierCode = dgvPendingBills.SelectedRows[0].Cells["cashier_code"]?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(cashierCode))
            {
                cashierCode = PromptForEmployeeCode();
            }

            if (string.IsNullOrWhiteSpace(cashierCode))
            {
                MessageBox.Show("Please enter the salesperson's name to proceed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                DataGridView printGrid = BuildTemporaryBillingGrid();
                decimal totalAmount = currentItems.Sum(item => Convert.ToDecimal(item.Rate) * Convert.ToDecimal(item.Qty));
                decimal discountAmount = totalAmount - currentItems.Sum(item => Convert.ToDecimal(item.Price));

                PDFConverter converter = new PDFConverter();
                converter.ConvertPrintDocumentToPdf(printGrid, cashierCode, totalAmount, discountAmount);

                bool saveCompleted = false;
                try
                {
                    BillHistoryManager.SaveBill(printGrid, cashierCode, totalAmount, discountAmount);
                    saveCompleted = true;
                }
                catch
                {
                    FallbackBillLogger.LogFailedBill(printGrid, cashierCode, totalAmount, discountAmount);
                    saveCompleted = true;
                }

                if (saveCompleted)
                {
                    RemovePrintedPendingBill(pendingBillId);
                    LoadPendingBills();
                    ResetDetailPanel();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to print pending bill: " + ex.Message, "Pending Bills", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnCancelBill_Click(object sender, EventArgs e)
        {
            if (dgvPendingBills.SelectedRows.Count == 0 || dgvPendingBills.SelectedRows[0].Cells["id"].Value == null)
            {
                MessageBox.Show("Please select a pending bill first.", "Pending Bills", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            DialogResult confirmation = MessageBox.Show(
                "Cancel this bill? The cashier will need to re-enter it.",
                "Pending Bills",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            int pendingBillId = Convert.ToInt32(dgvPendingBills.SelectedRows[0].Cells["id"].Value);

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "UPDATE pending_bill SET status = @status, updated_at = NOW() WHERE id = @id";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@status", cancelledStatus);
                        command.Parameters.AddWithValue("@id", pendingBillId);
                        command.ExecuteNonQuery();
                    }
                }

                LoadPendingBills();
                ResetDetailPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to cancel pending bill: " + ex.Message, "Pending Bills", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RemovePrintedPendingBill(int pendingBillId)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                using (MySqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string deleteItemsQuery = "DELETE FROM pending_bill_items WHERE pending_bill_id = @id";
                        using (MySqlCommand deleteItems = new MySqlCommand(deleteItemsQuery, connection, transaction))
                        {
                            deleteItems.Parameters.AddWithValue("@id", pendingBillId);
                            deleteItems.ExecuteNonQuery();
                        }

                        string deleteHeaderQuery = "DELETE FROM pending_bill WHERE id = @id";
                        using (MySqlCommand deleteHeader = new MySqlCommand(deleteHeaderQuery, connection, transaction))
                        {
                            deleteHeader.Parameters.AddWithValue("@id", pendingBillId);
                            deleteHeader.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private string PromptForEmployeeCode()
        {
            string selectedEmployee = null;
            using (var empSelectionForm = new emp_selection())
            {
                if (empSelectionForm.ShowDialog() == DialogResult.OK)
                {
                    selectedEmployee = empSelectionForm.SelectedEmployee;
                }
            }
            return selectedEmployee;
        }

        private DataGridView BuildTemporaryBillingGrid()
        {
            DataGridView grid = new DataGridView();
            grid.Columns.Add("id", "id");
            grid.Columns.Add("ítem_name", "ítem_name");
            grid.Columns.Add("rate", "rate");
            grid.Columns.Add("amount", "amount");
            grid.Columns.Add("discounted_price", "discounted_price");

            int rowId = 1;
            foreach (PendingBillItemRow item in currentItems)
            {
                grid.Rows.Add(rowId++, item.ItemName, item.Rate, item.Qty, item.Price);
            }

            return grid;
        }

        private float ConvertToFloat(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return 0f;
            }

            float parsed;
            return float.TryParse(value.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out parsed) ? parsed : 0f;
        }

        private class PendingBillItemRow
        {
            public string ItemName { get; set; }
            public float Rate { get; set; }
            public float Qty { get; set; }
            public float Price { get; set; }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // PendingBillsInbox
            // 
            this.ClientSize = new System.Drawing.Size(1358, 721);
            this.Name = "PendingBillsInbox";
            this.ResumeLayout(false);

        }
    }
}
