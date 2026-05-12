using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class VoidBillReasonDialog : Form
    {
        private readonly ComboBox cmbReason;

        public string SelectedReason { get; private set; }

        public VoidBillReasonDialog()
        {
            Text = "Void Bill Reason";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(430, 180);

            Label lblReason = new Label
            {
                Text = "Select reason",
                Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold),
                Location = new Point(20, 20),
                AutoSize = true
            };
            Controls.Add(lblReason);

            cmbReason = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Microsoft Sans Serif", 12F),
                Location = new Point(20, 55),
                Size = new Size(390, 34)
            };
            cmbReason.Items.AddRange(new object[]
            {
                "Rejected Bill",
                "Test Bill",
                "Wrong Items",
                "Wrong Quantity",
                "Wrong Price",
                "Wrong Discount",
                "Wrong Payment Method",
                "Customer Cancelled",
                "Duplicate Bill",
                "Cashier Mistake",
                "Other"
            });
            cmbReason.SelectedIndex = 0;
            Controls.Add(cmbReason);

            Button btnOk = new Button
            {
                Text = "OK",
                DialogResult = DialogResult.OK,
                Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold),
                Location = new Point(210, 115),
                Size = new Size(95, 40)
            };
            btnOk.Click += (s, e) => SelectedReason = cmbReason.SelectedItem?.ToString() ?? "";
            Controls.Add(btnOk);

            Button btnCancel = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Font = new Font("Microsoft Sans Serif", 11F),
                Location = new Point(315, 115),
                Size = new Size(95, 40)
            };
            Controls.Add(btnCancel);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }
    }
}
