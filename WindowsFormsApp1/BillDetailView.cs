using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class BillDetailView : Form
    {
        private int billId;
        private DataRow billHeader;
        private bool isVoided;

        public bool BillWasVoided { get; private set; }

        public BillDetailView(int billId)
        {
            InitializeComponent();
            this.billId = billId;

            dataGridItems.ReadOnly = true;
            dataGridItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridItems.AllowUserToAddRows = false;
            dataGridItems.MultiSelect = false;
            dataGridItems.Font = new Font("Arial", 12);
            dataGridItems.RowTemplate.Height = 32;
        }

        private void LoadBillData()
        {
            try
            {
                billHeader = BillHistoryManager.GetBillHeader(billId);
                if (billHeader == null)
                {
                    MessageBox.Show("Bill not found.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.Close();
                    return;
                }

                lblBillCode.Text = billHeader["bill_code"].ToString();
                lblDateTime.Text = Convert.ToDateTime(billHeader["date_time"]).ToString("yyyy-MM-dd  hh:mm tt");
                lblSalesperson.Text = billHeader["salesperson"].ToString();
                isVoided = billHeader.Table.Columns.Contains("status")
                    && string.Equals(billHeader["status"]?.ToString(), "VOIDED", StringComparison.OrdinalIgnoreCase);

                decimal totalAmount = Convert.ToDecimal(billHeader["total_amount"]);
                decimal discountAmount = Convert.ToDecimal(billHeader["discount_amount"]);
                decimal grandTotal = Convert.ToDecimal(billHeader["grand_total"]);

                lblTotal.Text = totalAmount.ToString("N2");
                lblDiscount.Text = discountAmount.ToString("N2");
                lblGrandTotal.Text = grandTotal.ToString("N2");

                if (discountAmount == 0)
                {
                    lblDiscountLabel.Visible = false;
                    lblDiscountRs.Visible = false;
                    lblDiscount.Visible = false;
                }

                DataTable items = BillHistoryManager.GetBillItems(billId);
                dataGridItems.DataSource = items;
                FormatItemsGrid();

                string paymentMethod = billHeader.Table.Columns.Contains("payment_method")
                    ? billHeader["payment_method"]?.ToString() ?? "CASH"
                    : "CASH";
                this.Text = $"Bill Details - {billHeader["bill_code"]}  [{paymentMethod}]";
                if (isVoided)
                {
                    this.Text += " [VOIDED]";
                    lblBillCode.ForeColor = Color.DarkRed;
                    btnReprint.Enabled = false;
                    btnDeleteBill.Enabled = false;
                    btnDeleteAndMoveToBilling.Enabled = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading bill: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatItemsGrid()
        {
            if (dataGridItems.Columns.Count == 0) return;

            FormatColumn(dataGridItems, "id", "#", 60, alignment: DataGridViewContentAlignment.MiddleCenter);

            var colName = dataGridItems.Columns["item_name"];
            if (colName != null)
            {
                colName.HeaderText = "Item Name";
                colName.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            FormatColumn(dataGridItems, "rate", "Rate (Rs.)", 140,
                format: "N2", alignment: DataGridViewContentAlignment.MiddleRight);

            FormatColumn(dataGridItems, "amount", "Qty (kg/pcs)", 130,
                alignment: DataGridViewContentAlignment.MiddleCenter);

            var colPrice = dataGridItems.Columns["discounted_price"];
            if (colPrice != null)
            {
                colPrice.HeaderText = "Price (Rs.)";
                colPrice.Width = 150;
                colPrice.DefaultCellStyle.Format = "N2";
                colPrice.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                colPrice.DefaultCellStyle.Font = new Font("Arial", 12, FontStyle.Bold);
            }
        }

        private static void FormatColumn(DataGridView grid, string name, string header, int width,
            string format = null, DataGridViewContentAlignment? alignment = null)
        {
            var col = grid.Columns[name];
            if (col == null) return;
            col.HeaderText = header;
            col.Width = width;
            if (format != null) col.DefaultCellStyle.Format = format;
            if (alignment.HasValue) col.DefaultCellStyle.Alignment = alignment.Value;
        }

        private void btnReprint_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to reprint this bill?",
                "Reprint Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    decimal totalAmount = Convert.ToDecimal(billHeader["total_amount"]);
                    decimal discountAmount = Convert.ToDecimal(billHeader["discount_amount"]);
                    string salesperson = billHeader["salesperson"].ToString();
                    string billCodeStr = billHeader["bill_code"].ToString();
                    DateTime billDateTimeVal = Convert.ToDateTime(billHeader["date_time"]);

                    PDFConverter converter = new PDFConverter();
                    converter.ConvertPrintDocumentToPdf(dataGridItems, salesperson, totalAmount, discountAmount, billCodeStr, billDateTimeVal);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Reprint failed: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btnDeleteBill_Click(object sender, EventArgs e)
        {
            VoidBill("DELETE_ONLY", false);
        }

        private void btnDeleteAndMoveToBilling_Click(object sender, EventArgs e)
        {
            VoidBill("DELETE_AND_MOVE_TO_BILLING", true);
        }

        private void VoidBill(string action, bool moveToBilling)
        {
            if (billHeader == null)
            {
                return;
            }

            string billCode = billHeader["bill_code"].ToString();
            string message = moveToBilling
                ? "This will void bill " + billCode + ", reverse its stock movement, and load the items into Billing.\n\nA new bill will be created when checkout is completed.\n\nContinue?"
                : "This will void bill " + billCode + " and reverse its stock movement.\n\nThis cannot be undone.\n\nContinue?";

            if (MessageBox.Show(message, "Void Bill", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                return;
            }

            DataTable itemsToMove = null;
            billing billingForm = null;
            if (moveToBilling)
            {
                itemsToMove = BillHistoryManager.GetBillItems(billId);
                if (itemsToMove.Rows.Count == 0)
                {
                    MessageBox.Show("This bill has no items to load.", "Void Bill", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                billingForm = Application.OpenForms.OfType<billing>().FirstOrDefault();
                if (billingForm == null || billingForm.IsDisposed)
                {
                    billingForm = new billing();
                    billingForm.Show();
                }
                else if (!billingForm.Visible)
                {
                    billingForm.Show();
                }

                if (billingForm.BillItemCount > 0 &&
                    MessageBox.Show("Current billing screen has items. Loading this bill will clear them. Continue?",
                        "Void Bill", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    return;
                }
            }

            string reason = "Moved back to billing for correction";
            if (!moveToBilling)
            {
                using (VoidBillReasonDialog reasonDialog = new VoidBillReasonDialog())
                {
                    if (reasonDialog.ShowDialog(this) != DialogResult.OK)
                    {
                        return;
                    }

                    reason = reasonDialog.SelectedReason;
                }
            }

            try
            {
                BillHistoryManager.VoidBillResult result = BillHistoryManager.VoidBill(billId, reason, action);

                if (moveToBilling)
                {
                    billingForm.ClearCurrentBillForPendingLoad();
                    foreach (DataRow row in itemsToMove.Rows)
                    {
                        billingForm.AddBillItem(
                            row["item_name"]?.ToString() ?? string.Empty,
                            ToDecimal(row["rate"]),
                            ToDecimal(row["amount"]),
                            ToDecimal(row["discounted_price"]));
                    }
                    billingForm.SetPendingBillContext(billHeader["salesperson"]?.ToString(), null);
                    HideNavigationForms();
                    billingForm.WindowState = FormWindowState.Maximized;
                    billingForm.Show();
                    billingForm.BringToFront();
                    billingForm.Activate();
                    billingForm.Focus();
                }

                BillWasVoided = true;

                string warning = "";
                if (result.StockReversalCount == 0)
                {
                    warning += "\n\nNo stock movement rows were found to reverse. This can happen if stock movement was skipped when the bill was created.";
                }
                if (result.CreditBillWithoutLedgerMatch)
                {
                    warning += "\n\nThis was a CREDIT bill, but no matching credit ledger BILL row was found. Check the credit account manually.";
                }

                MessageBox.Show("Bill " + result.BillCode + " was voided." + warning,
                    "Void Bill", MessageBoxButtons.OK,
                    string.IsNullOrWhiteSpace(warning) ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to void bill: " + ex.Message, "Void Bill", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static decimal ToDecimal(object value)
        {
            if (value == null || value == DBNull.Value)
            {
                return 0m;
            }

            decimal result;
            return decimal.TryParse(value.ToString(), out result) ? result : 0m;
        }

        private void HideNavigationForms()
        {
            foreach (Form form in Application.OpenForms.Cast<Form>().ToList())
            {
                if (form == this)
                {
                    continue;
                }

                if (form is Home || form is BillHistory)
                {
                    form.Hide();
                }
            }
        }

        private void BillDetailView_Load(object sender, EventArgs e)
        {
            LoadBillData();
        }
    }
}
