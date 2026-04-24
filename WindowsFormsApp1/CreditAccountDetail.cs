using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class CreditAccountDetail : Form
    {
        private readonly int accountId;
        private readonly DataGridView dataGridLedger = new DataGridView();
        private readonly Label lblAccountName = new Label();
        private readonly Label lblBalance = new Label();

        public CreditAccountDetail(int accountId)
        {
            this.accountId = accountId;
            BuildUi();
            Load += (s, e) => LoadData();
        }

        private void BuildUi()
        {
            Text = "Credit Account";
            StartPosition = FormStartPosition.CenterParent;
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;

            lblAccountName.Font = new Font("Microsoft Sans Serif", 18F, FontStyle.Bold);
            lblAccountName.AutoSize = true;
            lblAccountName.Location = new Point(20, 18);
            Controls.Add(lblAccountName);

            lblBalance.Font = new Font("Microsoft Sans Serif", 13F, FontStyle.Bold);
            lblBalance.AutoSize = true;
            lblBalance.Location = new Point(22, 58);
            Controls.Add(lblBalance);

            // Action buttons (top-right)
            Button btnAddBill = new Button
            {
                Text = "+ Bill",
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold),
                Size = new Size(120, 38),
                Location = new Point(750, 18),
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAddBill.Click += (s, e) => AddTransaction("BILL");
            Controls.Add(btnAddBill);

            Button btnReceived = new Button
            {
                Text = "Received",
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold),
                Size = new Size(130, 38),
                Location = new Point(880, 18),
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnReceived.Click += (s, e) => AddTransaction("RECEIVED");
            Controls.Add(btnReceived);

            Button btnAdjust = new Button
            {
                Text = "Adjustment",
                BackColor = Color.DarkOrange,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold),
                Size = new Size(140, 38),
                Location = new Point(1020, 18),
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnAdjust.Click += (s, e) => AddTransaction("ADJUSTMENT");
            Controls.Add(btnAdjust);

            Button btnViewBill = new Button
            {
                Text = "View Bill",
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold),
                Size = new Size(130, 38),
                Location = new Point(1170, 18),
                UseVisualStyleBackColor = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            btnViewBill.Click += BtnViewBill_Click;
            Controls.Add(btnViewBill);

            // Ledger grid
            dataGridLedger.Location = new Point(12, 105);
            dataGridLedger.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridLedger.Size = new Size(1540, 870);
            dataGridLedger.ReadOnly = true;
            dataGridLedger.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridLedger.AllowUserToAddRows = false;
            dataGridLedger.MultiSelect = false;
            dataGridLedger.Font = new Font("Arial", 12);
            dataGridLedger.RowTemplate.Height = 35;
            dataGridLedger.BorderStyle = BorderStyle.Fixed3D;
            Controls.Add(dataGridLedger);

            Controls.Add(new Label
            {
                Text = "Select a row and click \"View Bill\" to open a linked bill",
                Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Italic),
                ForeColor = SystemColors.GrayText,
                AutoSize = true,
                Location = new Point(12, 1003),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            });

            Controls.Add(new Label
            {
                Text = "Developed and Maintained by BlackBox Computers\u2122",
                Font = new Font("Microsoft Sans Serif", 10.2F),
                AutoSize = true,
                Location = new Point(1142, 1003),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            });
        }

        private void LoadData()
        {
            try
            {
                DataRow header = CreditManager.GetAccountHeader(accountId);
                if (header == null) { Close(); return; }

                string name = header["customer_name"].ToString();
                string label = header["label"].ToString();
                lblAccountName.Text = string.IsNullOrWhiteSpace(label) ? name : $"{name}  —  {label}";

                decimal balance = CreditManager.GetAccountBalance(accountId);
                if (balance > 0)
                {
                    lblBalance.Text = $"Outstanding balance:  Rs. {balance:N2}";
                    lblBalance.ForeColor = Color.Crimson;
                }
                else if (balance < 0)
                {
                    lblBalance.Text = $"In credit:  Rs. {Math.Abs(balance):N2}";
                    lblBalance.ForeColor = Color.SeaGreen;
                }
                else
                {
                    lblBalance.Text = "Balance:  Rs. 0.00  (settled)";
                    lblBalance.ForeColor = Color.DimGray;
                }

                DataTable dt = CreditManager.GetTransactionsWithBalance(accountId);
                dataGridLedger.DataSource = dt;
                FormatGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading account: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatGrid()
        {
            if (dataGridLedger.Columns.Count == 0) return;

            var colId = dataGridLedger.Columns["txn_id"];
            if (colId != null) colId.Visible = false;

            FormatCol("txn_date", "Date", 120, format: "yyyy-MM-dd");
            FormatCol("txn_type", "Type", 120);
            FormatCol("description", "Description", 300);
            FormatCol("bill_code", "Bill Code", 150);
            FormatCol("debit", "Bill / Debit (Rs.)", 170, format: "N2", align: DataGridViewContentAlignment.MiddleRight);
            FormatCol("credit", "Received / Credit (Rs.)", 195, format: "N2", align: DataGridViewContentAlignment.MiddleRight);

            var colBal = dataGridLedger.Columns["running_balance"];
            if (colBal != null)
            {
                colBal.HeaderText = "Balance (Rs.)";
                colBal.Width = 160;
                colBal.DefaultCellStyle.Format = "N2";
                colBal.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                colBal.DefaultCellStyle.Font = new Font("Arial", 12, FontStyle.Bold);
                colBal.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            FormatCol("recorded_by", "Recorded By", 150);
            FormatCol("recorded_at", "Recorded At", 190, format: "yyyy-MM-dd  hh:mm tt");

            foreach (DataGridViewColumn col in dataGridLedger.Columns)
                col.SortMode = DataGridViewColumnSortMode.Automatic;

            foreach (DataGridViewRow row in dataGridLedger.Rows)
            {
                var typeCell = row.Cells["txn_type"];
                if (typeCell?.Value == null) continue;
                switch (typeCell.Value.ToString())
                {
                    case "RECEIVED":
                        row.DefaultCellStyle.BackColor = Color.FromArgb(230, 255, 230);
                        break;
                    case "BILL":
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 240, 240);
                        break;
                    case "ADJUSTMENT":
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 220);
                        break;
                }
            }
        }

        private void FormatCol(string name, string header, int width,
            string format = null, DataGridViewContentAlignment? align = null)
        {
            var col = dataGridLedger.Columns[name];
            if (col == null) return;
            col.HeaderText = header;
            col.Width = width;
            if (format != null) col.DefaultCellStyle.Format = format;
            if (align.HasValue) col.DefaultCellStyle.Alignment = align.Value;
        }

        private void AddTransaction(string txnType)
        {
            using (var dlg = new AddCreditTransactionDialog(txnType))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    string direction = txnType == "RECEIVED" ? "CREDIT"
                                     : txnType == "BILL" ? "DEBIT"
                                     : dlg.Direction;
                    CreditManager.AddTransaction(accountId, txnType, dlg.Amount, direction,
                        dlg.Description, dlg.BillCode, dlg.TransactionDate);
                    LoadData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to save transaction: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnViewBill_Click(object sender, EventArgs e)
        {
            if (dataGridLedger.SelectedRows.Count == 0)
            {
                MessageBox.Show("Select a transaction row first.", "View Bill",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string billCode = dataGridLedger.SelectedRows[0].Cells["bill_code"]?.Value?.ToString();
            if (string.IsNullOrWhiteSpace(billCode))
            {
                MessageBox.Show("No bill is linked to this transaction.", "View Bill",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                int billId = BillHistoryManager.GetBillIdByCode(billCode);
                if (billId <= 0)
                {
                    MessageBox.Show($"Bill '{billCode}' was not found in bill history.", "View Bill",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                new BillDetailView(billId).ShowDialog(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening bill: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(1568, 1055);
            Name = "CreditAccountDetail";
            ResumeLayout(false);
        }
    }
}
