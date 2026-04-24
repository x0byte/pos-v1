using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class PaymentMethodDialog : Form
    {
        public string SelectedPaymentMethod { get; private set; } = "CASH";
        public int SelectedCreditAccountId { get; private set; } = 0;
        public string SelectedCreditAccountName { get; private set; } = "";

        private RadioButton rbCash;
        private RadioButton rbCard;
        private RadioButton rbCredit;
        private ComboBox cmbCreditAccount;
        private Label lblCreditAccount;
        private Button btnConfirm;
        private Button btnCancel;

        private List<(int AccountId, string DisplayName)> creditAccounts;

        public PaymentMethodDialog()
        {
            BuildUi();
            LoadCreditAccounts();
        }

        private void BuildUi()
        {
            Text = "Payment Method";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(440, 230);

            Controls.Add(new Label
            {
                Text = "How is this bill being paid?",
                Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 20)
            });

            rbCash = new RadioButton
            {
                Text = "Cash",
                Font = new Font("Microsoft Sans Serif", 13F),
                Location = new Point(40, 65),
                AutoSize = true,
                Checked = true
            };

            rbCard = new RadioButton
            {
                Text = "Card",
                Font = new Font("Microsoft Sans Serif", 13F),
                Location = new Point(175, 65),
                AutoSize = true
            };

            rbCredit = new RadioButton
            {
                Text = "Credit",
                Font = new Font("Microsoft Sans Serif", 13F),
                Location = new Point(305, 65),
                AutoSize = true
            };

            lblCreditAccount = new Label
            {
                Text = "Credit Account:",
                Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 110),
                Visible = false
            };

            cmbCreditAccount = new ComboBox
            {
                Font = new Font("Microsoft Sans Serif", 11F),
                Location = new Point(20, 135),
                Size = new Size(400, 30),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Visible = false
            };

            btnConfirm = new Button
            {
                Text = "Confirm",
                Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold),
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                Size = new Size(150, 42),
                Location = new Point(270, 172),
                UseVisualStyleBackColor = false
            };

            btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Microsoft Sans Serif", 12F),
                Size = new Size(120, 42),
                Location = new Point(20, 172)
            };

            rbCredit.CheckedChanged += (s, e) =>
            {
                bool creditSelected = rbCredit.Checked;
                lblCreditAccount.Visible = creditSelected;
                cmbCreditAccount.Visible = creditSelected;
                int newHeight = creditSelected ? 280 : 230;
                ClientSize = new Size(440, newHeight);
                int btnY = newHeight - 58;
                btnConfirm.Location = new Point(270, btnY);
                btnCancel.Location = new Point(20, btnY);
            };

            btnConfirm.Click += BtnConfirm_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] {
                rbCash, rbCard, rbCredit,
                lblCreditAccount, cmbCreditAccount,
                btnConfirm, btnCancel
            });

            AcceptButton = btnConfirm;
            CancelButton = btnCancel;
        }

        private void LoadCreditAccounts()
        {
            try
            {
                creditAccounts = CreditManager.GetActiveAccountsForDropdown();
                cmbCreditAccount.Items.Clear();
                foreach (var acc in creditAccounts)
                    cmbCreditAccount.Items.Add(acc.DisplayName);
                if (cmbCreditAccount.Items.Count > 0)
                    cmbCreditAccount.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not load credit accounts: " + ex.Message,
                    "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (rbCash.Checked)
            {
                SelectedPaymentMethod = "CASH";
            }
            else if (rbCard.Checked)
            {
                SelectedPaymentMethod = "CARD";
            }
            else if (rbCredit.Checked)
            {
                if (creditAccounts == null || creditAccounts.Count == 0 || cmbCreditAccount.SelectedIndex < 0)
                {
                    MessageBox.Show("Please select a credit account.\nAdd one from the Credit section on the home screen first.",
                        "Credit Account Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                SelectedPaymentMethod = "CREDIT";
                SelectedCreditAccountId = creditAccounts[cmbCreditAccount.SelectedIndex].AccountId;
                SelectedCreditAccountName = creditAccounts[cmbCreditAccount.SelectedIndex].DisplayName;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private void InitializeComponent() { }
    }
}
