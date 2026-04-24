using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class AddCreditTransactionDialog : Form
    {
        public decimal Amount { get; private set; }
        public string Description { get; private set; }
        public string BillCode { get; private set; }
        public DateTime TransactionDate { get; private set; }
        public string Direction { get; private set; } = "DEBIT";

        private readonly string txnType;
        private readonly TextBox txtAmount = new TextBox();
        private readonly TextBox txtDescription = new TextBox();
        private readonly TextBox txtBillCode = new TextBox();
        private readonly DateTimePicker dtpDate = new DateTimePicker();
        private RadioButton rbDebit;
        private RadioButton rbCredit;

        public AddCreditTransactionDialog(string txnType)
        {
            this.txnType = txnType;
            BuildUi();
        }

        private void BuildUi()
        {
            bool isBill = txnType == "BILL";
            bool isAdjustment = txnType == "ADJUSTMENT";

            Text = isBill ? "Add Bill Entry" : txnType == "RECEIVED" ? "Record Payment Received" : "Add Adjustment";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            int y = 20;

            // Date
            AddLabel("Date *", ref y);
            dtpDate.Font = new Font("Microsoft Sans Serif", 11F);
            dtpDate.Format = DateTimePickerFormat.Short;
            dtpDate.Value = DateTime.Today;
            dtpDate.Location = new Point(20, y);
            dtpDate.Size = new Size(200, 28);
            Controls.Add(dtpDate);
            y += 46;

            // Amount
            AddLabel("Amount (Rs.) *", ref y);
            txtAmount.Font = new Font("Microsoft Sans Serif", 11F);
            txtAmount.Location = new Point(20, y);
            txtAmount.Size = new Size(390, 28);
            Controls.Add(txtAmount);
            y += 46;

            // Bill code — BILL type only
            if (isBill)
            {
                AddLabel("Bill Code  (optional — e.g. STC-00042)", ref y);
                txtBillCode.Font = new Font("Microsoft Sans Serif", 11F);
                txtBillCode.Location = new Point(20, y);
                txtBillCode.Size = new Size(390, 28);
                Controls.Add(txtBillCode);
                y += 46;
            }

            // Direction — ADJUSTMENT type only
            if (isAdjustment)
            {
                AddLabel("Direction *", ref y);
                rbDebit = new RadioButton
                {
                    Text = "Debit  (customer owes more)",
                    Font = new Font("Microsoft Sans Serif", 10F),
                    Location = new Point(20, y),
                    AutoSize = true,
                    Checked = true
                };
                rbCredit = new RadioButton
                {
                    Text = "Credit  (reduces balance)",
                    Font = new Font("Microsoft Sans Serif", 10F),
                    Location = new Point(260, y),
                    AutoSize = true
                };
                Controls.Add(rbDebit);
                Controls.Add(rbCredit);
                y += 40;
            }

            // Description
            AddLabel("Description / Note", ref y);
            txtDescription.Font = new Font("Microsoft Sans Serif", 11F);
            txtDescription.Location = new Point(20, y);
            txtDescription.Size = new Size(390, 28);
            Controls.Add(txtDescription);
            y += 56;

            // Buttons
            Button btnSave = new Button
            {
                Text = "Save",
                Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                Size = new Size(140, 44),
                Location = new Point(270, y),
                UseVisualStyleBackColor = false
            };
            btnSave.Click += BtnSave_Click;

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Microsoft Sans Serif", 12F),
                Size = new Size(120, 44),
                Location = new Point(20, y),
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(btnSave);
            Controls.Add(btnCancel);
            AcceptButton = btnSave;
            CancelButton = btnCancel;

            ClientSize = new Size(430, y + 60);
        }

        private void AddLabel(string text, ref int y)
        {
            Controls.Add(new Label
            {
                Text = text,
                Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, y)
            });
            y += 26;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (!decimal.TryParse(txtAmount.Text.Trim(), out decimal amount) || amount <= 0)
            {
                MessageBox.Show("Please enter a valid amount greater than 0.", "Validation",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Amount = amount;
            Description = txtDescription.Text.Trim();
            BillCode = txtBillCode.Text.Trim();
            TransactionDate = dtpDate.Value;
            Direction = rbCredit?.Checked == true ? "CREDIT" : "DEBIT";

            DialogResult = DialogResult.OK;
            Close();
        }

        private void InitializeComponent() { }
    }
}
