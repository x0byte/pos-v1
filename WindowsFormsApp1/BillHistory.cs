using MySql.Data.MySqlClient;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class BillHistory : Form
    {
        public BillHistory()
        {
            InitializeComponent();

            dtpFrom.Value = DateTime.Today.AddDays(-2);
            dtpTo.Value = DateTime.Today;

            dataGridHistory.ReadOnly = true;
            dataGridHistory.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridHistory.AllowUserToAddRows = false;
            dataGridHistory.MultiSelect = false;
            dataGridHistory.Font = new Font("Arial", 12);
            dataGridHistory.RowTemplate.Height = 35;

            dataGridHistory.CellDoubleClick += DataGridHistory_CellDoubleClick;
        }

        private void LoadHistory()
        {
            try
            {
                DataTable dt = BillHistoryManager.GetBillHistory();
                dataGridHistory.DataSource = dt;
                FormatGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while loading bill history: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatGrid()
        {
            if (dataGridHistory.Columns.Count == 0) return;

            SetColumnHidden(dataGridHistory, "bill_id");
            SetColumnHidden(dataGridHistory, "total_amount");
            SetColumnHidden(dataGridHistory, "discount_amount");

            FormatColumn(dataGridHistory, "bill_code", "Bill ID", 160);
            FormatColumn(dataGridHistory, "date_time", "Date & Time", 220, format: "yyyy-MM-dd  hh:mm tt");
            FormatColumn(dataGridHistory, "salesperson", "Salesperson", 180);
            FormatColumn(dataGridHistory, "item_count", "Items", 90, alignment: DataGridViewContentAlignment.MiddleCenter);
            FormatColumn(dataGridHistory, "payment_method", "Payment", 110, alignment: DataGridViewContentAlignment.MiddleCenter);
            FormatColumn(dataGridHistory, "status", "Status", 100, alignment: DataGridViewContentAlignment.MiddleCenter);
            SetColumnHidden(dataGridHistory, "voided_at");
            SetColumnHidden(dataGridHistory, "voided_by");
            SetColumnHidden(dataGridHistory, "void_action");

            var colGrandTotal = dataGridHistory.Columns["grand_total"];
            if (colGrandTotal != null)
            {
                colGrandTotal.HeaderText = "Grand Total (Rs.)";
                colGrandTotal.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                colGrandTotal.DefaultCellStyle.Format = "N2";
                colGrandTotal.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                colGrandTotal.DefaultCellStyle.Font = new Font("Arial", 12, FontStyle.Bold);
            }

            foreach (DataGridViewColumn col in dataGridHistory.Columns)
            {
                col.SortMode = DataGridViewColumnSortMode.Automatic;
            }

            foreach (DataGridViewRow row in dataGridHistory.Rows)
            {
                string status = row.Cells["status"]?.Value?.ToString();
                if (string.Equals(status, "VOIDED", StringComparison.OrdinalIgnoreCase))
                {
                    row.DefaultCellStyle.BackColor = Color.MistyRose;
                    row.DefaultCellStyle.ForeColor = Color.DarkRed;
                }
            }
        }

        private static void SetColumnHidden(DataGridView grid, string name)
        {
            var col = grid.Columns[name];
            if (col != null) col.Visible = false;
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

        private void DataGridHistory_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var cell = dataGridHistory.Rows[e.RowIndex].Cells["bill_id"];
            if (cell == null || cell.Value == null) return;
            int billId = Convert.ToInt32(cell.Value);
            using (BillDetailView detailView = new BillDetailView(billId))
            {
                detailView.ShowDialog();
                if (detailView.BillWasVoided)
                {
                    LoadHistory();
                }
            }
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            try
            {
                DataTable dt = BillHistoryManager.SearchBillHistory(
                    dtpFrom.Value, dtpTo.Value, txtSearch.Text.Trim());
                dataGridHistory.DataSource = dt;
                FormatGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Search failed: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnClearFilter_Click(object sender, EventArgs e)
        {
            dtpFrom.Value = DateTime.Today.AddDays(-2);
            dtpTo.Value = DateTime.Today;
            txtSearch.Text = "";
            LoadHistory();
        }


        private void BillHistory_Load(object sender, EventArgs e)
        {
            LoadHistory();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Home home = Application.OpenForms.OfType<Home>().FirstOrDefault() ?? new Home();
            home.Show();
            this.Hide();
        }
    }
}
