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
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.IO.Image;
using iText.Kernel.Geom;

using System.Drawing.Printing;
using System.Drawing.Imaging;


namespace WindowsFormsApp1
{
    public partial class billing : Form
    {
        private string connectionString = DatabaseConfig.ConnectionString;
        private readonly List<BillItem> billItems = new List<BillItem>();
        private static List<BillItem> pausedBillItems = new List<BillItem>();
        private int nextRowId = 1;
        private DateTime billCreatedAt = DateTime.Now;
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
            UpdateStockSyncLabel();
            UpdateSyncStatusLabel();
            StartSyncRetryTimer();

            CheckForPausesInMemory();


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

            if (FallbackBillLogger.HasUnsyncedBills())
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
            string normalized = (query ?? string.Empty).Trim().ToLowerInvariant();
            string noSpace = normalized.Replace(" ", string.Empty);

            return AppCache.Inventory
                .Where(item =>
                    (!string.IsNullOrWhiteSpace(item.ItemName) && item.ItemName.ToLowerInvariant().Contains(normalized)) ||
                    (!string.IsNullOrWhiteSpace(item.ItemName) && item.ItemName.Replace(" ", string.Empty).ToLowerInvariant().Contains(noSpace)) ||
                    (!string.IsNullOrWhiteSpace(item.Keywords) && item.Keywords.ToLowerInvariant().Contains(normalized)))
                .Select(item => item.ItemName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct()
                .Take(20)
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
            InventoryItem item = AppCache.Inventory.FirstOrDefault(i =>
                string.Equals(i.ItemName, itemName, StringComparison.OrdinalIgnoreCase));

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
            InventoryItem item = AppCache.Inventory.FirstOrDefault(i =>
                string.Equals(i.ItemName, itemName, StringComparison.OrdinalIgnoreCase));

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


        private string loadEmployeeCode()
        {
            string selectedEmployee = null;

            using (var empSelectionForm = new emp_selection())
            {

                if (empSelectionForm.ShowDialog() == DialogResult.OK)
                {
                    // Retrieve the selected employee from the emp_selection form
                    selectedEmployee = empSelectionForm.SelectedEmployee;
                }
            }

            return selectedEmployee;
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
            // Keep float columns so PDFConverter cell parsing is unchanged
            dataTable.Columns.Add("rate", typeof(float));
            dataTable.Columns.Add("amount", typeof(float));
            dataTable.Columns.Add("discounted_price", typeof(float));

            foreach (BillItem item in billItems)
            {
                dataTable.Rows.Add(item.RowId, item.ItemName, (float)item.Rate, (float)item.Amount, (float)item.DiscountedPrice);
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



        private void button2_Click(object sender, EventArgs e)
        {
           
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to checkout this order?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                ReorderBillingTable();
                LoadBillingData();
 
                string emp_code = loadEmployeeCode();

                // Check if the user clicked OK and entered a name
                if (!string.IsNullOrEmpty(emp_code))
                {
                    // Proceed with printing the bill, including the salesperson's name
                    


                    string cashierName = emp_code;
                    decimal totalAmount = CalculateGrandTotalFromMemory();
                    decimal discountedAmount = totalAmount - decimal.Parse(lblTotalPrice.Text);

                    // Save first so the bill code is available for the receipt
                    string billCode;
                    try
                    {
                        billCode = BillHistoryManager.SaveBill(dataGridBilling, cashierName, totalAmount, discountedAmount);
                    }
                    catch
                    {
                        FallbackBillLogger.LogFailedBill(dataGridBilling, cashierName, totalAmount, discountedAmount);
                        billCode = "LOCAL-" + DateTime.Now.ToString("yyyyMMddHHmmss");
                    }

                    PDFConverter converter = new PDFConverter();
                    converter.ConvertPrintDocumentToPdf(dataGridBilling, cashierName, totalAmount, discountedAmount, billCode, billCreatedAt);

                    billItems.Clear();
                    nextRowId = 1;
                    billCreatedAt = DateTime.Now;
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
            else if (dialogResult == DialogResult.No)
            {

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
                LoadBillingData();
                clearTexts();
                lblCount.Text = string.Empty;
                lblTotalPrice.Text = string.Empty;
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
            LoadBillingData();
            clearTexts();
            lblCount.Text = string.Empty;
            lblTotalPrice.Text = string.Empty;
        }

        public void RefreshBillingView()
        {
            LoadBillingData();
        }

        public void AddBillItem(string itemName, decimal rate, decimal amount, decimal discountedPrice)
        {
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
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to make that change?  ", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dialogResult != DialogResult.Yes)
            {
                return;
            }

            if (btnPauseBill.Text == "Pause this Bill")
            {
                pausedBillItems = billItems.Select(CloneBillItem).ToList();
                billItems.Clear();
                nextRowId = 1;
                LoadBillingData();
                btnPauseBill.Text = "Go to the Previous Bill";
                btnPauseBill.BackColor = Color.Black;
                btnPauseBill.ForeColor = SystemColors.Control;
                return;
            }

            billItems.Clear();
            billItems.AddRange(pausedBillItems.Select(CloneBillItem));
            pausedBillItems.Clear();
            ReorderBillingTable();
            LoadBillingData();
            btnPauseBill.Text = "Pause this Bill";
            btnPauseBill.BackColor = Color.FromArgb(255, 255, 128, 0);
            btnPauseBill.ForeColor = SystemColors.ControlText;
        }

        private void CheckForPausesInMemory()
        {
            if (pausedBillItems.Count == 0)
            {
                return;
            }

            DialogResult result = MessageBox.Show("There is another bill paused in the system. Do you want to access it ?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (result == DialogResult.Yes)
            {
                billItems.Clear();
                billItems.AddRange(pausedBillItems.Select(CloneBillItem));
                pausedBillItems.Clear();
                ReorderBillingTable();
                LoadBillingData();
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

        public void PrintReceipt(DataGridView dataGridView, string cashierName, decimal totalAmount, decimal discountedAmount)
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
                string date = DateTime.Now.ToShortDateString();
                string time = DateTime.Now.ToShortTimeString();
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
                graphics.DrawString($"Discount Rs. : {discountedAmount:N2}", discountFont, Brushes.Black, startX, startY + offsetY);
                offsetY += (int)fontHeight + 5;
                graphics.DrawString($"Grand Total Rs. : {(totalAmount - discountedAmount):N2}", grandTotalFont, Brushes.Black, startX, startY + offsetY);

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

            decimal minimum_price = decimal.Parse(lblCost.Text) * decimal.Parse(txtAmount.Text);
            decimal billed_price = decimal.Parse(lblFinalPrice.Text);

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
