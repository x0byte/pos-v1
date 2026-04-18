using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class BillDetailView : Form
    {
        private int billId;
        private DataRow billHeader;

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

                this.Text = "Bill Details — " + billHeader["bill_code"].ToString();
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

        private void BillDetailView_Load(object sender, EventArgs e)
        {
            LoadBillData();
        }
    }
}
