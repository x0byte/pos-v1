using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Image;
using iText.Kernel.Geom;

using System.Drawing.Printing;
using System.Drawing.Imaging;
using Newtonsoft.Json;


namespace WindowsFormsApp1
{
    public partial class billing : Form
    {
        private string connectionString = DatabaseConfig.ConnectionString;
        private readonly List<BillItem> billItems = new List<BillItem>();
        private static readonly Dictionary<string, PausedCartRecord> pausedBillItems = new Dictionary<string, PausedCartRecord>(StringComparer.OrdinalIgnoreCase);
        private static bool pausedCartsLoaded;
        private static readonly string PausedCartsPath = System.IO.Path.Combine(Application.StartupPath, "paused_carts.json");
        private int nextRowId = 1;
        private DateTime billCreatedAt = DateTime.Now;
        private string currentClientSubmissionId;
        private string pendingSalespersonHint;
        private string pendingSnapshotSessionId;
        private bool promptedForPausedCartRestore;
        private readonly ListBox suggestionListBox;
        private readonly System.Windows.Forms.TextBox textBox;
        private Label lblStockSync;
        private System.Windows.Forms.Timer syncTimer;

        
        public billing()
        {
            InitializeComponent();

            txtItemName.TextChanged += TextBox_TextChanged;
            listBoxSuggestions.Click += SuggestionListBox_Click;
            listBoxSuggestions.KeyDown += SuggestionListBox_KeyDown;

            this.dataGridBilling.CellClick += new DataGridViewCellEventHandler(this.dataGridView1_CellClick);

            dataGridBilling.Font = new Font("Arial", 14);
            InitializeStockRefreshControls();
            this.Shown += Billing_Shown;
            this.Resize += Billing_Resize;
            ApplyResponsiveLayout();
            UpdateStockSyncLabel();
            UpdateSyncStatusLabel();
            StartSyncRetryTimer();
            EnsurePausedCartsLoaded();
            CheckForPausesInMemory();
            UpdatePauseButtonState();

        }

        private void Billing_Shown(object sender, EventArgs e)
        {
            ApplyWorkingAreaBounds();
            ApplyResponsiveLayout();
        }

        private void Billing_Resize(object sender, EventArgs e)
        {
            ApplyWorkingAreaBounds();
            ApplyResponsiveLayout();
        }

        private void ApplyWorkingAreaBounds()
        {
            Screen screen = Screen.FromControl(this);
            if (screen != null)
            {
                MaximizedBounds = screen.WorkingArea;
            }
        }

        private void ApplyResponsiveLayout()
        {
            if (dataGridBilling == null || ClientSize.Width <= 0 || ClientSize.Height <= 0)
            {
                return;
            }

            SuspendLayout();
            try
            {
                AutoScroll = false;

                int margin = 12;
                int clientWidth = ClientSize.Width;
                int clientHeight = ClientSize.Height;
                bool compact = clientHeight < 760;
                int leftWidth = Math.Min(620, Math.Max(520, (int)(clientWidth * 0.46)));
                int rightX = leftWidth + margin;
                int rightWidth = Math.Max(320, clientWidth - rightX - margin);

                int bottomMargin = compact ? 8 : 12;
                int gap = compact ? 6 : 12;
                int newBillHeight = compact ? 38 : 58;
                int checkoutHeight = compact ? 42 : 62;
                int editHeight = compact ? 38 : 56;
                int addHeight = compact ? 46 : 70;
                int newBillY = Math.Max(560, clientHeight - bottomMargin - newBillHeight);
                int checkoutY = newBillY - gap - checkoutHeight;
                int editY = checkoutY - gap - editHeight;
                int addY = editY - gap - addHeight;

                pictureBox1.SetBounds(4, 3, 63, 62);
                btnScan.SetBounds(214, 18, Math.Max(250, leftWidth - 226), 53);

                label1.SetBounds(12, compact ? 72 : 82, 130, 25);
                txtItemName.SetBounds(17, compact ? 98 : 111, leftWidth - 25, 34);

                int suggestionY = txtItemName.Bottom + 7;
                int fixedSpaceBelowSuggestions = 250;
                int suggestionHeight = Math.Max(55, Math.Min(compact ? 95 : 149, addY - suggestionY - fixedSpaceBelowSuggestions));
                listBoxSuggestions.SetBounds(17, suggestionY, leftWidth - 25, suggestionHeight);

                int priceLabelY = listBoxSuggestions.Bottom + (compact ? 8 : 14);
                int priceInputY = priceLabelY + (compact ? 34 : 41);
                int secondColumnX = Math.Min(291, leftWidth / 2);
                int helpX = Math.Min(leftWidth - 130, secondColumnX + 190);

                label2.SetBounds(22, priceLabelY, 160, 30);
                txtRetailPrice.SetBounds(64, priceInputY, 155, 41);
                label3.SetBounds(28, priceInputY + 19, 30, 16);
                label4.SetBounds(secondColumnX, priceLabelY, 115, 30);
                txtAmount.SetBounds(secondColumnX, priceInputY, 156, 41);
                label5.SetBounds(helpX, priceLabelY - 10, 120, 100);

                int discountLabelY = priceInputY + (compact ? 56 : 60);
                int discountInputY = discountLabelY + (compact ? 35 : 49);
                label6.SetBounds(25, discountLabelY, 220, 30);
                txtDisEach.SetBounds(31, discountInputY, 125, 41);
                label7.SetBounds(secondColumnX, discountLabelY, 230, 30);
                txtDisWhole.SetBounds(secondColumnX, discountInputY, 125, 41);

                int finalLabelY = Math.Min(discountInputY + 58, addY - 64);
                int finalValueY = finalLabelY + 30;
                label8.SetBounds(25, finalLabelY, 250, 25);
                label9.SetBounds(30, finalValueY + 10, 30, 16);
                lblFinalPrice.SetBounds(56, finalValueY, 260, 34);
                label14.SetBounds(secondColumnX + 62, finalLabelY, 70, 25);
                lblCost.SetBounds(secondColumnX + 69, finalValueY, 150, 34);

                button1.SetBounds(30, addY, leftWidth - 108, addHeight);
                int editButtonWidth = Math.Max(105, (leftWidth - 124) / 3);
                btnClear.SetBounds(33, editY, editButtonWidth, editHeight);
                btnUpdate.SetBounds(33 + editButtonWidth + gap, editY, editButtonWidth, editHeight);
                btnDelete.SetBounds(33 + ((editButtonWidth + gap) * 2), editY, editButtonWidth, editHeight);
                button2.SetBounds(33, checkoutY, leftWidth - 108, checkoutHeight);
                button3.SetBounds(35, newBillY, leftWidth - 110, newBillHeight);

                int topRightButtonWidth = Math.Min(307, Math.Max(190, rightWidth / 4));
                btnPauseBill.SetBounds(clientWidth - margin - topRightButtonWidth, 18, topRightButtonWidth, 53);

                Control refreshButton = Controls.Find("btnRefreshStock", false).FirstOrDefault();
                if (refreshButton != null)
                {
                    refreshButton.SetBounds(rightX + 18, 20, 140, 36);
                }
                if (lblStockSync != null)
                {
                    lblStockSync.SetBounds(rightX + 168, 20, 280, 18);
                }
                Label syncStatus = Controls.Find("lblSyncStatus", false).FirstOrDefault() as Label;
                if (syncStatus != null)
                {
                    syncStatus.SetBounds(rightX + 168, 40, Math.Max(260, rightWidth - 180), 18);
                }

                int summaryHeight = compact ? 92 : 112;
                int summaryY = Math.Max(540, clientHeight - bottomMargin - summaryHeight);
                int gridHeight = Math.Max(250, summaryY - 98 - gap);
                dataGridBilling.SetBounds(rightX, 98, rightWidth, gridHeight);

                int totalX = rightX + 5;
                int countBlockWidth = Math.Min(260, Math.Max(190, rightWidth / 3));
                int countX = rightX + rightWidth - countBlockWidth;
                label12.SetBounds(totalX, summaryY + 4, 80, 25);
                label11.SetBounds(totalX + 11, summaryY + 44, 45, 25);
                lblTotalPrice.AutoSize = false;
                lblTotalPrice.TextAlign = ContentAlignment.MiddleLeft;
                lblTotalPrice.SetBounds(totalX + 62, summaryY + 32, Math.Max(180, countX - totalX - 78), 50);

                label13.SetBounds(countX, summaryY + 4, countBlockWidth - 60, 25);
                lblCount.AutoSize = false;
                lblCount.TextAlign = ContentAlignment.MiddleRight;
                lblCount.SetBounds(countX + countBlockWidth - 72, summaryY + 32, 72, 50);
                label10.SetBounds(Math.Max(rightX, clientWidth - 420), clientHeight - 28, 405, 22);
            }
            finally
            {
                ResumeLayout(false);
            }
        }
        private void InitializeStockRefreshControls()
        {
            System.Windows.Forms.Button btnRefreshStock = new System.Windows.Forms.Button
            {
                Name = "btnRefreshStock",
                Text = "Refresh Stock",
                Size = new Size(140, 36),
                Location = new System.Drawing.Point(650, 20)
            };
            btnRefreshStock.Click += BtnRefreshStock_Click;
            this.Controls.Add(btnRefreshStock);
            btnRefreshStock.BringToFront();

            lblStockSync = new Label
            {
                Name = "lblStockSync",
                AutoSize = true,
                Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Regular, GraphicsUnit.Point, 0),
                Location = new System.Drawing.Point(475, 20)
            };
            this.Controls.Add(lblStockSync);
            lblStockSync.BringToFront();

            Label lblSyncStatus = new Label
            {
                Name = "lblSyncStatus",
                AutoSize = true,
                Font = new Font("Microsoft Sans Serif", 8F),
                Location = new System.Drawing.Point(475, 40)
            };
            this.Controls.Add(lblSyncStatus);
            lblSyncStatus.BringToFront();
        }

        private void BtnRefreshStock_Click(object sender, EventArgs e)
        {
            try
            {
                AppCache.Refresh();
                UpdateStockSyncLabel();
                MessageBox.Show("Stock cache refreshed successfully.", "Refresh Stock", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to refresh stock cache: " + ex.Message, "Refresh Stock", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateStockSyncLabel()
        {
            if (lblStockSync == null)
            {
                return;
            }

            lblStockSync.Text = AppCache.LastSynced == DateTime.MinValue
                ? "Last synced: never"
                : "Last synced: " + AppCache.LastSynced.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private void UpdateSyncStatusLabel()
        {
            var lbl = this.Controls.Find("lblSyncStatus", false).FirstOrDefault() as Label;
            if (lbl == null) return;

            if (FallbackBillLogger.IsQueueLarge())
            {
                lbl.Text = "Offline queue is large - database may be unreachable. Contact admin.";
                lbl.ForeColor = Color.Red;
            }
            else if (FallbackBillLogger.HasUnsyncedBills())
            {
                lbl.Text = "Unsynced bills pending";
                lbl.ForeColor = Color.OrangeRed;
            }
            else
            {
                lbl.Text = "All bills synced";
                lbl.ForeColor = Color.Green;
            }
        }

        private void StartSyncRetryTimer()
        {
            syncTimer = new System.Windows.Forms.Timer();
            syncTimer.Interval = 3 * 60 * 1000;
            syncTimer.Tick += (s, ev) =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        FallbackBillLogger.RetryUnsynced();
                        this.Invoke((Action)UpdateSyncStatusLabel);
                    }
                    catch { }
                });
            };
            syncTimer.Start();
        }

        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            string query = txtItemName.Text;
            if (string.IsNullOrWhiteSpace(query))
            {
                listBoxSuggestions.Visible = false;
                return;
            }

            List<string> suggestions = GetSuggestions(query);
            listBoxSuggestions.Items.Clear();
            if (suggestions.Count > 0)
            {
                listBoxSuggestions.Items.AddRange(suggestions.ToArray());
                listBoxSuggestions.Visible = true;
            }
            else
            {
                listBoxSuggestions.Visible = false;
            }
        }

        private void SuggestionListBox_Click(object sender, EventArgs e)
        {
            if (listBoxSuggestions.SelectedItem != null)
            {
                txtItemName.Text = listBoxSuggestions.SelectedItem.ToString();
                listBoxSuggestions.Visible = false;
            }

            LoadItemPrice(txtItemName.Text);
        }

        private List<string> GetSuggestions(string query)
        {
            return InventorySearch.GetMatches(query, 20)
                .Select(item => item.ItemName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .ToList();
        }

        private void SuggestionListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && listBoxSuggestions.SelectedItem != null)
            {
                txtItemName.Text = listBoxSuggestions.SelectedItem.ToString();
                listBoxSuggestions.Visible = false;
                e.Handled = true; // Mark the event as handled
                e.SuppressKeyPress = true; // Prevent the "ding" sound
            }

            LoadItemPrice(txtItemName.Text);
            loadItemCost(txtItemName.Text);
        }
        private void LoadItemPrice(string itemName)
        {
            InventoryItem item;
            AppCache.InventoryByName.TryGetValue((itemName ?? string.Empty).Trim(), out item);

            if (item != null)
            {
                txtRetailPrice.Text = item.RetailPrice.ToString();
            }
            else
            {
                txtRetailPrice.Text = "Price: Not available";
            }
        }

        private void loadItemCost(string itemName)
        {
            InventoryItem item;
            AppCache.InventoryByName.TryGetValue((itemName ?? string.Empty).Trim(), out item);

            if (item != null && item.Cost.HasValue)
            {
                lblCost.Text = item.Cost.Value.ToString();
            }
            else
            {
                lblCost.Text = "0";
            }
        }

        private void GetRowCount()
        {
            lblCount.Text = billItems.Count.ToString();
        }


        private string loadEmployeeCode(string preferredEmployeeCode = null)
        {
            string selectedEmployee = null;

            using (var empSelectionForm = new emp_selection(preferredEmployeeCode))
            {

                if (empSelectionForm.ShowDialog() == DialogResult.OK)
                {
                    // Retrieve the selected employee from the emp_selection form
                    selectedEmployee = empSelectionForm.SelectedEmployee;
                }
            }

            return selectedEmployee;
        }

        public void SetPendingBillContext(string salespersonHint, string snapshotSessionId)
        {
            pendingSalespersonHint = salespersonHint;
            pendingSnapshotSessionId = snapshotSessionId;
        }


        private void clearTexts()
        {
            txtItemName.Text = string.Empty;
            txtAmount.Text = "1";
            txtDisEach.Text = "0";
            txtDisWhole.Text = "0";
            txtRetailPrice.Text = string.Empty;
            lblFinalPrice.Text = string.Empty;


            txtItemName.Focus();
        }
        private void LoadBillingData()
        {
            DataTable dataTable = new DataTable();
            dataTable.Columns.Add("id", typeof(int));
            dataTable.Columns.Add("ítem_name", typeof(string));
            dataTable.Columns.Add("rate", typeof(decimal));
            dataTable.Columns.Add("amount", typeof(decimal));
            dataTable.Columns.Add("discounted_price", typeof(decimal));

            foreach (BillItem item in billItems)
            {
                dataTable.Rows.Add(item.RowId, item.ItemName, item.Rate, item.Amount, item.DiscountedPrice);
            }

            dataGridBilling.DataSource = dataTable;
            calculate_Total();
            CalculateGrandTotalFromMemory();
            GetRowCount();
        }

        private void billing_Load(object sender, EventArgs e)
        {
            LoadBillingData();
            UpdateSyncStatusLabel();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void calculate_Total()
        {
            decimal sum = billItems.Sum(item => item.DiscountedPrice);
            lblTotalPrice.Text = sum.ToString();
            UpdatePauseButtonState();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            txtDisEach.Text = string.IsNullOrEmpty(txtDisEach.Text) ? "0" : txtDisEach.Text;
            txtDisWhole.Text = string.IsNullOrEmpty(txtDisWhole.Text) ? "0" : txtDisWhole.Text;

            decimal retailPrice = decimal.Parse(txtRetailPrice.Text);
            decimal amount = decimal.Parse(txtAmount.Text);
            decimal each_discount = decimal.Parse(txtDisEach.Text);
            decimal whole_discount = decimal.Parse(txtDisWhole.Text);

            decimal finalPrice = (retailPrice * amount) - (each_discount * amount) - whole_discount;
            lblFinalPrice.Text = finalPrice.ToString();

            if (isTheSaleProfitable())
            {
                AddBillItem(txtItemName.Text, retailPrice, amount, finalPrice);
                MessageBox.Show("Successfully Added!", " New Item", MessageBoxButtons.OK, MessageBoxIcon.Information);
                clearTexts();
            }
            else
            {
                MessageBox.Show("This item cannot be added because there's an error with its price.");
            }
        }

        public void authenticateInputs()
        {

        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            clearTexts();
        }



        private async void button2_Click(object sender, EventArgs e)
        {
            button2.Enabled = false;
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to checkout this order?", "Confirmation", MessageBoxButtons.YesNo);
            try
            {
                if (dialogResult == DialogResult.Yes)
                {
                    ReorderBillingTable();
                    LoadBillingData();
                    EnsureCurrentSubmissionId();

                    string emp_code = loadEmployeeCode(pendingSalespersonHint);

                    // Check if the user clicked OK and entered a name
                    if (!string.IsNullOrEmpty(emp_code))
                    {
                        decimal totalAmount = CalculateGrandTotalFromMemory();
                        decimal discountAmount = totalAmount - decimal.Parse(lblTotalPrice.Text);
                        decimal grandTotal = totalAmount - discountAmount;

                        PaymentMethodDialog paymentDlg = new PaymentMethodDialog(grandTotal);
                        if (paymentDlg.ShowDialog() != DialogResult.OK)
                        {
                            paymentDlg.Dispose();
                            return;
                        }
                        string paymentMethod = paymentDlg.SelectedPaymentMethod;
                        int creditAccountId = paymentDlg.SelectedCreditAccountId;
                        string creditAccountName = paymentDlg.SelectedCreditAccountName;
                        paymentDlg.Dispose();

                        string cashierName = emp_code;

                        // Snapshot bill data so the background thread never touches UI controls
                        IList<BillLineRecord> billSnapshot = billItems.Select(x => new BillLineRecord
                        {
                            ItemName        = x.ItemName,
                            Rate            = x.Rate,
                            Amount          = x.Amount,
                            DiscountedPrice = x.DiscountedPrice
                        }).ToList();
                        string submissionId = currentClientSubmissionId;

                        // Save to DB in background so the UI stays responsive
                        string billCode;
                        button2.Text = "Processing...";
                        try
                        {
                            billCode = await Task.Run(() =>
                                BillHistoryManager.SaveBill(billSnapshot, cashierName, totalAmount, discountAmount, submissionId, paymentMethod));
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
                                button2.Text = "Checkout";
                                throw;
                            }

                            FallbackBillLogger.LogFailedBill(dataGridBilling, cashierName, totalAmount, discountAmount, currentClientSubmissionId);
                            billCode = "LOCAL-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                        }
                        finally
                        {
                            button2.Text = "Checkout";
                        }

                        PDFConverter converter = new PDFConverter();
                        converter.ConvertPrintDocumentToPdf(dataGridBilling, cashierName, totalAmount, discountAmount, billCode, billCreatedAt);

                        if (paymentMethod == "CREDIT" && creditAccountId > 0)
                        {
                            if (billCode.StartsWith("LOCAL-"))
                            {
                                MessageBox.Show(
                                    "This credit bill was queued offline with a LOCAL bill code.\n\n"
                                    + "The bill can be synced later by the fallback mechanism, but the credit ledger was not updated automatically.\n\n"
                                    + $"Write this down for manual credit entry:\nBill: {billCode}\nAccount: {creditAccountName}\nAmount: Rs. {grandTotal:N2}",
                                    "Manual Credit Entry Required",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                            }
                            else
                            {
                                try
                                {
                                    CreditManager.AddTransaction(creditAccountId, "BILL", grandTotal, "DEBIT",
                                        "POS sale", billCode, DateTime.Today);
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(
                                        "The bill was saved, but the credit ledger could not be updated.\n\n"
                                        + "Write this down and add the credit entry manually later:\n"
                                        + $"Bill: {billCode}\nAccount: {creditAccountName}\nAmount: Rs. {grandTotal:N2}\n\n"
                                        + "Error: " + ex.Message,
                                        "Manual Credit Entry Required",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning);
                                }
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(pendingSnapshotSessionId))
                        {
                            DesktopPosLocalAudit.WritePendingDiff(pendingSnapshotSessionId, billItems.Select(item => new PendingBillSnapshotLine
                            {
                                ItemName = item.ItemName,
                                Rate = item.Rate,
                                Amount = item.Amount,
                                DiscountedPrice = item.DiscountedPrice
                            }));
                        }

                        billItems.Clear();
                        nextRowId = 1;
                        billCreatedAt = DateTime.Now;
                        currentClientSubmissionId = null;
                        pendingSalespersonHint = null;
                        pendingSnapshotSessionId = null;
                        LoadBillingData();
                        GetRowCount();
                        calculate_Total();
                        clearTexts();
                        UpdateSyncStatusLabel();
                    }
                    else
                    {
                        MessageBox.Show("Please enter the salesperson's name to proceed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
            }
            finally
            {
                button2.Enabled = true;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to cancel this bill?", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (dialogResult == DialogResult.Yes)
            {
                billItems.Clear();
                nextRowId = 1;
                billCreatedAt = DateTime.Now;
                currentClientSubmissionId = null;
                pendingSalespersonHint = null;
                pendingSnapshotSessionId = null;
                LoadBillingData();
                clearTexts();
                lblCount.Text = string.Empty;
                lblTotalPrice.Text = string.Empty;
                UpdatePauseButtonState();
            }
            else if (dialogResult == DialogResult.No)
            {

            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete this item?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                if (dataGridBilling.SelectedRows.Count > 0)
                {
                    int selectedId = Convert.ToInt32(dataGridBilling.SelectedRows[0].Cells["id"].Value);
                    billItems.RemoveAll(x => x.RowId == selectedId);
                    ReorderBillingTable();
                    LoadBillingData(); // Reload data to reflect changes
                }
                else
                {
                    MessageBox.Show("Please select a row to delete.", "Delete Entry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (dialogResult == DialogResult.No)
            {

            }

            GetRowCount();
            calculate_Total();
            ReorderBillingTable();
            LoadBillingData();
            clearTexts();

        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {

            DialogResult dialogResult = MessageBox.Show("Are you sure you want to update this item?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                txtDisEach.Text = string.IsNullOrEmpty(txtDisEach.Text) ? "0" : txtDisEach.Text;
                txtDisWhole.Text = string.IsNullOrEmpty(txtDisWhole.Text) ? "0" : txtDisWhole.Text;

                decimal retailPrice = decimal.Parse(txtRetailPrice.Text);
                decimal amount = decimal.Parse(txtAmount.Text);
                decimal each_discount = decimal.Parse(txtDisEach.Text);
                decimal whole_discount = decimal.Parse(txtDisWhole.Text);

                decimal finalPrice = (retailPrice * amount) - (each_discount * amount) - whole_discount;
                lblFinalPrice.Text = finalPrice.ToString();

                if (dataGridBilling.SelectedRows.Count > 0)
                {
                    int selectedId = Convert.ToInt32(dataGridBilling.SelectedRows[0].Cells["id"].Value);
                    BillItem billItem = billItems.FirstOrDefault(x => x.RowId == selectedId);
                    if (billItem != null)
                    {
                        billItem.ItemName = txtItemName.Text;
                        billItem.Amount = decimal.Parse(txtAmount.Text);
                        billItem.Rate = decimal.Parse(txtRetailPrice.Text);
                        billItem.DiscountedPrice = finalPrice;
                        MessageBox.Show("Record updated successfully!");
                        LoadBillingData();
                    }
                }

                calculate_Total();
                clearTexts();
            }
            else if (dialogResult == DialogResult.No)
            {

            }


        }

        private void dataGridBilling_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // Ensure the row index is valid
            {
                DataGridViewRow row = dataGridBilling.Rows[e.RowIndex];

                // Assuming the columns in the DataGridView are named "Column1", "Column2", "Column3", etc.
                txtItemName.Text = row.Cells["ítem_name"].Value?.ToString() ?? string.Empty;
                txtAmount.Text = row.Cells["amount"].Value?.ToString() ?? string.Empty;
                txtRetailPrice.Text = row.Cells["rate"].Value?.ToString() ?? string.Empty;

                txtDisEach.Text = "0";
                txtDisWhole.Text = "0";
            }
        }

        public void ReorderBillingTable()
        {
            int counter = 1;
            foreach (BillItem billItem in billItems.OrderBy(x => x.RowId))
            {
                billItem.RowId = counter++;
            }
            nextRowId = counter;
        }

        private decimal CalculateGrandTotalFromMemory()
        {
            decimal grandTotal = 0;
            foreach (BillItem item in billItems)
            {
                grandTotal += item.Rate * item.Amount;
            }
            return grandTotal;
        }

        public int BillItemCount => billItems.Count;

        public void ClearCurrentBillForPendingLoad()
        {
            billItems.Clear();
            nextRowId = 1;
            billCreatedAt = DateTime.Now;
            currentClientSubmissionId = null;
            pendingSalespersonHint = null;
            pendingSnapshotSessionId = null;
            LoadBillingData();
            clearTexts();
            lblCount.Text = string.Empty;
            lblTotalPrice.Text = string.Empty;
            UpdatePauseButtonState();
        }

        public void RefreshBillingView()
        {
            LoadBillingData();
        }

        public void AddBillItem(string itemName, decimal rate, decimal amount, decimal discountedPrice)
        {
            EnsureCurrentSubmissionId();
            billItems.Add(new BillItem
            {
                RowId = nextRowId++,
                ItemName = itemName,
                Rate = rate,
                Amount = amount,
                DiscountedPrice = discountedPrice
            });
            LoadBillingData();
            GetRowCount();
            calculate_Total();
        }

        private void BtnPauseBillInMemory_Click(object sender, EventArgs e)
        {
            if (billItems.Count > 0)
            {
                if (pausedBillItems.Count >= 5)
                {
                    MessageBox.Show("Please resume or cancel an existing paused bill first.", "Paused Bills", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string label = PromptForPauseLabel();
                if (string.IsNullOrWhiteSpace(label))
                {
                    return;
                }

                pausedBillItems[label] = new PausedCartRecord
                {
                    Label = label,
                    PausedAt = DateTime.Now,
                    Items = billItems.Select(item => new BillLineRecord
                    {
                        ItemName = item.ItemName,
                        Rate = item.Rate,
                        Amount = item.Amount,
                        DiscountedPrice = item.DiscountedPrice
                    }).ToList()
                };
                SavePausedCarts();

                billItems.Clear();
                nextRowId = 1;
                billCreatedAt = DateTime.Now;
                currentClientSubmissionId = null;
                LoadBillingData();
                clearTexts();
                UpdatePauseButtonState();
                return;
            }

            if (pausedBillItems.Count > 0)
                ResumePausedCart();
        }

        private void CheckForPausesInMemory()
        {
            if (promptedForPausedCartRestore || pausedBillItems.Count == 0)
            {
                return;
            }

            promptedForPausedCartRestore = true;

            DialogResult result = MessageBox.Show("There are paused bills saved in the system. Do you want to resume one now?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                ResumePausedCart();
            }
        }

        private void EnsurePausedCartsLoaded()
        {
            if (pausedCartsLoaded)
            {
                return;
            }

            pausedCartsLoaded = true;
            if (!File.Exists(PausedCartsPath))
            {
                return;
            }

            try
            {
                List<PausedCartRecord> carts = JsonConvert.DeserializeObject<List<PausedCartRecord>>(File.ReadAllText(PausedCartsPath));
                pausedBillItems.Clear();
                foreach (PausedCartRecord cart in carts ?? new List<PausedCartRecord>())
                {
                    if (!string.IsNullOrWhiteSpace(cart.Label))
                    {
                        pausedBillItems[cart.Label] = cart;
                    }
                }
            }
            catch
            {
                pausedBillItems.Clear();
            }
        }

        private void SavePausedCarts()
        {
            try
            {
                string json = JsonConvert.SerializeObject(pausedBillItems.Values.OrderBy(x => x.PausedAt).ToList(), Formatting.Indented);
                File.WriteAllText(PausedCartsPath, json);
            }
            catch
            {
                // Local convenience only. Do not block cashier flow.
            }
        }

        private string PromptForPauseLabel()
        {
            Form prompt = new Form
            {
                Width = 420,
                Height = 190,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Pause Bill",
                StartPosition = FormStartPosition.CenterParent
            };

            Label textLabel = new Label { Left = 20, Top = 20, Width = 350, Text = "Enter a short label for this paused bill:" };
            System.Windows.Forms.TextBox inputBox = new System.Windows.Forms.TextBox { Left = 20, Top = 50, Width = 350 };
            System.Windows.Forms.Button confirmation = new System.Windows.Forms.Button { Text = "Save", Left = 145, Width = 120, Top = 90, DialogResult = DialogResult.OK };

            prompt.Controls.Add(textLabel);
            prompt.Controls.Add(inputBox);
            prompt.Controls.Add(confirmation);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == DialogResult.OK ? inputBox.Text.Trim() : null;
        }

        private void ResumePausedCart()
        {
            if (pausedBillItems.Count == 0)
            {
                MessageBox.Show("There are no paused bills to resume.", "Paused Bills", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string selectedLabel = ShowPausedCartPicker();
            if (string.IsNullOrWhiteSpace(selectedLabel) || !pausedBillItems.ContainsKey(selectedLabel))
            {
                return;
            }

            PausedCartRecord cart = pausedBillItems[selectedLabel];
            billItems.Clear();
            nextRowId = 1;
            currentClientSubmissionId = null;
            EnsureCurrentSubmissionId();

            foreach (BillLineRecord item in cart.Items)
            {
                billItems.Add(new BillItem
                {
                    RowId = nextRowId++,
                    ItemName = item.ItemName,
                    Rate = item.Rate,
                    Amount = item.Amount,
                    DiscountedPrice = item.DiscountedPrice
                });
            }

            pausedBillItems.Remove(selectedLabel);
            SavePausedCarts();
            ReorderBillingTable();
            LoadBillingData();
            GetRowCount();
            calculate_Total();
            UpdatePauseButtonState();
        }

        private string ShowPausedCartPicker()
        {
            string selected = null;
            Form picker = new Form
            {
                Width = 520,
                Height = 420,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Resume Paused Bill",
                StartPosition = FormStartPosition.CenterParent
            };

            ListBox listBox = new ListBox
            {
                Left = 20,
                Top = 20,
                Width = 460,
                Height = 280,
                Font = new Font("Microsoft Sans Serif", 12F)
            };

            foreach (PausedCartRecord cart in pausedBillItems.Values.OrderBy(x => x.PausedAt))
            {
                listBox.Items.Add(cart.Label + "  |  " + cart.Items.Count + " items  |  " + cart.PausedAt.ToString("yyyy-MM-dd HH:mm"));
            }

            System.Windows.Forms.Button resumeButton = new System.Windows.Forms.Button { Text = "Resume", Left = 180, Width = 140, Top = 320, DialogResult = DialogResult.OK };
            picker.Controls.Add(listBox);
            picker.Controls.Add(resumeButton);
            picker.AcceptButton = resumeButton;

            if (picker.ShowDialog() == DialogResult.OK && listBox.SelectedIndex >= 0)
            {
                selected = pausedBillItems.Values.OrderBy(x => x.PausedAt).ElementAt(listBox.SelectedIndex).Label;
            }

            return selected;
        }

        private void UpdatePauseButtonState()
        {
            if (btnPauseBill == null)
            {
                return;
            }

            if (billItems.Count > 0)
            {
                btnPauseBill.Text = "Pause this Bill";
                btnPauseBill.BackColor = Color.FromArgb(255, 255, 128, 0);
                btnPauseBill.ForeColor = SystemColors.ControlText;
            }
            else
            {
                btnPauseBill.Text = pausedBillItems.Count > 0 ? "Resume Paused Bill" : "Pause this Bill";
                btnPauseBill.BackColor = pausedBillItems.Count > 0 ? Color.Black : Color.FromArgb(255, 255, 128, 0);
                btnPauseBill.ForeColor = pausedBillItems.Count > 0 ? SystemColors.Control : SystemColors.ControlText;
            }
        }

        private void EnsureCurrentSubmissionId()
        {
            if (string.IsNullOrWhiteSpace(currentClientSubmissionId))
            {
                currentClientSubmissionId = Guid.NewGuid().ToString();
            }
        }

        private static BillItem CloneBillItem(BillItem item)
        {
            return new BillItem
            {
                RowId = item.RowId,
                ItemName = item.ItemName,
                Rate = item.Rate,
                Amount = item.Amount,
                DiscountedPrice = item.DiscountedPrice
            };
        }

        //////////////////////////////////////////////////////////////////////////PRINTING/////////////////////////////////////////////////////////////////////////

        public void PrintReceipt(DataGridView dataGridView, string cashierName, decimal totalAmount, decimal discountAmount, DateTime billCreatedAt)
        {
            PrintDocument printDocument = new PrintDocument();
            printDocument.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("pprnm", 285, 5000);
            printDocument.PrintPage += (sender, e) =>
            {
                Graphics graphics = e.Graphics;
                Font font = new Font("Arial", 8);
                float fontHeight = font.GetHeight();
                int startX = 10;
                int startY = 10;
                int offsetY = 40;
                int printableWidth = e.MarginBounds.Width;

                // Define the company name and font settings
                string companyName = "Saman Trade Center";
                Font companyFont = new Font("Arial", 18, FontStyle.Bold);

                // Define the position to draw the text
                int textX = startX;
                int textY = startY;

                // Draw the company name on the graphics object
                graphics.DrawString(companyName, companyFont, Brushes.Black, textX, textY);

                // Update the offset
                offsetY = textY + companyFont.Height + 10;

                // Define the address and telephone details
                string address = "No.20, Matale road, Galewela";
                string telephone = "066 22 89 468";
                Font detailsFont = new Font("Arial", 9, FontStyle.Regular);

                // Draw the address on the graphics object
                graphics.DrawString(address, detailsFont, Brushes.Black, textX + 40, offsetY);

                // Update the offset for the telephone number
                offsetY += detailsFont.Height + 5;

                // Draw the telephone number on the graphics object
                graphics.DrawString(telephone, detailsFont, Brushes.Black, textX + 73, offsetY);

                // Update the offset
                offsetY += detailsFont.Height + 10;

                // Print Date and Time
                string date = billCreatedAt.ToShortDateString();
                string time = billCreatedAt.ToShortTimeString();
                graphics.DrawString($"Date: {date} Time: {time}", font, Brushes.Black, startX, startY + offsetY);
                offsetY += (int)fontHeight + 5;

                // Print Cashier Name
                graphics.DrawString($"Salesperson: {cashierName}", font, Brushes.Black, startX, startY + offsetY);
                offsetY += (int)fontHeight + 20;

                // Define column positions
                int idColWidth = 20;
                int itemNameColWidth = 100;
                int rateColWidth = 40;
                int qtyColWidth = 40;
                //int priceColWidth = 50;

                int idColPos = startX;
                int itemNameColPos = idColPos + idColWidth + 5;
                int rateColPos = itemNameColPos + itemNameColWidth + 5;
                int qtyColPos = rateColPos + rateColWidth + 5;
                int priceColPos = qtyColPos + qtyColWidth + 5;

                // Print column headers
                graphics.DrawString("ID", font, Brushes.Black, idColPos, startY + offsetY);
                graphics.DrawString("Item Name", font, Brushes.Black, itemNameColPos, startY + offsetY);
                graphics.DrawString("Rate", font, Brushes.Black, rateColPos, startY + offsetY);
                graphics.DrawString("(kg/pcs)", font, Brushes.Black, qtyColPos, startY + offsetY);
                graphics.DrawString("Price", font, Brushes.Black, priceColPos, startY + offsetY);
                offsetY += (int)fontHeight + 5;

                // Print rows
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.IsNewRow) continue;

                    int currentX = startX;
                    int rowHeight = (int)fontHeight + 5;
                    int currentOffsetY = offsetY;

                    // Print ID
                    graphics.DrawString(row.Cells[0].Value.ToString(), font, Brushes.Black, idColPos, startY + currentOffsetY);

                    // Print Item Name with word wrapping
                    string itemName = row.Cells[1].Value.ToString() + " (Rs." + row.Cells[2].Value.ToString() + ")";



                    string[] itemNameLines = SplitText(itemName, graphics, font, itemNameColWidth);
                    foreach (string line in itemNameLines)
                    {
                        graphics.DrawString(line, font, Brushes.Black, itemNameColPos, startY + currentOffsetY);
                        currentOffsetY += (int)fontHeight + 2;
                    }

                    // Adjust row height if item name has multiple lines
                    rowHeight = Math.Max(rowHeight, currentOffsetY - offsetY);

                    string total_discounted_price = row.Cells[4].Value.ToString();
                    string amount = row.Cells[3].Value.ToString();

                    decimal ourprice = decimal.Parse(total_discounted_price) / decimal.Parse(amount);


                    // Print Rate
                    graphics.DrawString(ourprice.ToString(), font, Brushes.Black, rateColPos, startY + offsetY);

                    // Print Quantity
                    graphics.DrawString(row.Cells[3].Value.ToString(), font, Brushes.Black, qtyColPos, startY + offsetY);

                    // Print Price
                    graphics.DrawString(row.Cells[4].Value.ToString(), font, Brushes.Black, priceColPos, startY + offsetY);

                    // Move to next row
                    offsetY += rowHeight;
                }

                Font discountFont = new Font("Arial", 10, FontStyle.Bold);

                Font grandTotalFont = new Font("Arial", 10, FontStyle.Regular);

                // Print Total and Discounted Amounts
                offsetY += 20;
                graphics.DrawString($"Total Rs.: {totalAmount:N2}", font, Brushes.Black, startX, startY + offsetY);
                offsetY += (int)fontHeight + 5;
                graphics.DrawString($"Discount Rs. : {discountAmount:N2}", discountFont, Brushes.Black, startX, startY + offsetY);
                offsetY += (int)fontHeight + 5;
                graphics.DrawString($"Grand Total Rs. : {(totalAmount - discountAmount):N2}", grandTotalFont, Brushes.Black, startX, startY + offsetY);

                if (discountAmount > 0)
                {
                    offsetY += 30;

                    System.Drawing.Rectangle savingsBox = new System.Drawing.Rectangle(startX + 15, startY + offsetY, 235, 48);
                    Font savingsLabelFont = new Font("Arial", 9, FontStyle.Bold);
                    Font savingsAmountFont = new Font("Arial", 11, FontStyle.Bold);

                    using (Pen savingsBorderPen = new Pen(Color.Black, 1.5f))
                    using (StringFormat centeredFormat = new StringFormat())
                    {
                        centeredFormat.Alignment = StringAlignment.Center;
                        centeredFormat.LineAlignment = StringAlignment.Center;

                        graphics.DrawRectangle(savingsBorderPen, savingsBox);
                        graphics.DrawString("You saved today", savingsLabelFont, Brushes.Black,
                            new RectangleF(savingsBox.Left, savingsBox.Top + 6, savingsBox.Width, 16), centeredFormat);
                        graphics.DrawString($"Rs. {discountAmount:N2}", savingsAmountFont, Brushes.Black,
                            new RectangleF(savingsBox.Left, savingsBox.Top + 24, savingsBox.Width, 18), centeredFormat);
                    }

                    offsetY += savingsBox.Height;
                }

                // Add Footnotes
                offsetY += 40; // Add some space before the footnotes
                string returnPolicy = "Returns accepted within 7 days with the receipt";
                string outroRemarks = "Thank you for shopping with us!";
                string softwareCompanyInfo = "BlackBox Technologies";

                Font returnFont = new Font("Arial", 8, FontStyle.Bold);
                Font footnoteFont = new Font("Arial", 8, FontStyle.Regular);
                float footnoteFontHeight = footnoteFont.GetHeight();

                graphics.DrawString(outroRemarks, discountFont, Brushes.Black, startX + 22, startY + offsetY);
                offsetY += (int)footnoteFontHeight + 5;

                graphics.DrawString(returnPolicy, returnFont, Brushes.Black, startX, startY + offsetY);
                offsetY += (int)footnoteFontHeight + 5;
                offsetY += 25;

                graphics.DrawString(softwareCompanyInfo, footnoteFont, Brushes.Black, startX + 10, startY + offsetY);

            };

            /*PrintPreviewDialog printPreviewDialog = new PrintPreviewDialog();
            printPreviewDialog.Document = printDocument;
            printPreviewDialog.ShowDialog();*/

            

            /*try
            {
                printDocument.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while printing the receipt: " + ex.Message);
            }*/
        }

        // Helper method to split text into multiple lines
        private string[] SplitText(string text, Graphics graphics, Font font, int maxWidth)
        {
            List<string> lines = new List<string>();
            string[] words = text.Split(' ');
            StringBuilder currentLine = new StringBuilder();

            foreach (string word in words)
            {
                if (graphics.MeasureString(currentLine + word, font).Width > maxWidth)
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                }
                currentLine.Append(word + " ");
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
            }

            return lines.ToArray();
        }



        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Home home = new Home();
            home.Show();
            this.Hide();
        }

        private void txtItemName_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            item_scan item = new item_scan(this);
            item.Show();

        }

        private bool isTheSaleProfitable()
        {
            lblCost.Text = string.IsNullOrEmpty(lblCost.Text) ? "0" : lblCost.Text;
            txtDisEach.Text = string.IsNullOrEmpty(txtDisEach.Text) ? "0" : txtDisEach.Text;
            txtDisWhole.Text = string.IsNullOrEmpty(txtDisWhole.Text) ? "0" : txtDisWhole.Text;

            decimal retailPrice = decimal.Parse(txtRetailPrice.Text);
            decimal qty = decimal.Parse(txtAmount.Text);
            decimal eachDiscount = decimal.Parse(txtDisEach.Text);
            decimal wholeDiscount = decimal.Parse(txtDisWhole.Text);
            decimal minimum_price = decimal.Parse(lblCost.Text) * qty;
            decimal billed_price = (retailPrice - eachDiscount) * qty - wholeDiscount;

            if (minimum_price > 0 && billed_price < minimum_price)
            {
                string configuredPassword = DatabaseConfig.OverridePassword;
                if (string.IsNullOrEmpty(configuredPassword))
                {
                    System.Windows.Forms.MessageBox.Show(
                        "This sale is below cost and no override password is configured.\nAsk an administrator to set the override password in config.json.",
                        "Override Not Available",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Error);
                    return false;
                }

                string password = PromptForPassword();
                if (password == configuredPassword)
                {
                    DesktopPosLocalAudit.AppendOverrideLog(
                        UserSession.Username ?? "desktop-pos",
                        txtItemName.Text,
                        qty,
                        retailPrice,
                        decimal.Parse(lblCost.Text),
                        billed_price,
                        "Below-cost sale override approved locally.");
                    return true;
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Incorrect password. Sale cannot proceed.", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    return false;
                }
            }

            return true;
        }

        private string PromptForPassword()
        {
            string password = null;
            System.Windows.Forms.Form prompt = new System.Windows.Forms.Form()
            {
                Width = 300,
                Height = 200,
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
                Text = "Requires an Override Password",
                StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
            };

            System.Windows.Forms.Label textLabel = new System.Windows.Forms.Label() { Left = 20, Top = 20, Text = "Enter password:" };
            System.Windows.Forms.TextBox passwordBox = new System.Windows.Forms.TextBox() { Left = 20, Top = 50, Width = 240, UseSystemPasswordChar = true };
            System.Windows.Forms.Button confirmation = new System.Windows.Forms.Button() { Text = "OK", Left = 90, Width = 100, Top = 80, DialogResult = System.Windows.Forms.DialogResult.OK };
            System.Windows.Forms.Label details = new System.Windows.Forms.Label() { Left = 20, Top = 100, Text = "Cannot proceed without an override password because this sale isn't profitable." };

            confirmation.Click += (sender, e) => { password = passwordBox.Text; prompt.Close(); };

            prompt.Controls.Add(passwordBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == System.Windows.Forms.DialogResult.OK ? password : null;
        }

        private void listBoxSuggestions_SelectedIndexChanged(object sender, EventArgs e)
        {

        }




    }
}
