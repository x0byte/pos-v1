using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class NewCreditAccountDialog : Form
    {
        public string CustomerName => txtName.Text.Trim();
        public string AccountLabel => txtLabel.Text.Trim();

        private readonly TextBox txtName = new TextBox();
        private readonly TextBox txtLabel = new TextBox();

        public NewCreditAccountDialog()
        {
            Text = "New Credit Account";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(420, 240);

            Controls.Add(new Label
            {
                Text = "Customer Name *",
                Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            });

            txtName.Font = new Font("Microsoft Sans Serif", 11F);
            txtName.Location = new Point(20, 46);
            txtName.Size = new Size(380, 28);
            Controls.Add(txtName);

            Controls.Add(new Label
            {
                Text = "Account Label  (e.g. Shop, Personal — leave blank if only one account)",
                Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 90)
            });

            txtLabel.Font = new Font("Microsoft Sans Serif", 11F);
            txtLabel.Location = new Point(20, 115);
            txtLabel.Size = new Size(380, 28);
            Controls.Add(txtLabel);

            Button btnCreate = new Button
            {
                Text = "Create",
                Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                Size = new Size(140, 44),
                Location = new Point(260, 178),
                UseVisualStyleBackColor = false
            };
            btnCreate.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Customer name is required.", "Validation",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                DialogResult = DialogResult.OK;
                Close();
            };

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Microsoft Sans Serif", 12F),
                Size = new Size(120, 44),
                Location = new Point(20, 178),
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(btnCreate);
            Controls.Add(btnCancel);
            AcceptButton = btnCreate;
            CancelButton = btnCancel;
        }

        private void InitializeComponent() { }
    }
}
