using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class PaymentMethodDialog : Form
    {
        // ── Public result ─────────────────────────────────────────────────────────
        public string SelectedPaymentMethod  { get; private set; } = "CASH";
        public int    SelectedCreditAccountId   { get; private set; } = 0;
        public string SelectedCreditAccountName { get; private set; } = "";

        // ── Internal state ────────────────────────────────────────────────────────
        private string currentMethod = "CASH";
        private bool accountsLoaded = false;
        private List<AccountEntry> allAccounts    = new List<AccountEntry>();
        private List<AccountEntry> filteredAccounts = new List<AccountEntry>();

        // ── Controls ──────────────────────────────────────────────────────────────
        private Button btnCash, btnCard, btnCredit, btnConfirm, btnCancel;
        private Panel  creditPanel;
        private TextBox txtSearch;
        private ListBox lstAccounts;
        private Label   lblNoAccounts;

        // ── Layout constants ──────────────────────────────────────────────────────
        private const int FormWidth       = 580;
        private const int CompactHeight   = 220;
        private const int ExpandedHeight  = 460;
        private const int MethodBtnTop    = 68;
        private const int CreditPanelTop  = 150;
        private const int CreditPanelH    = 225;

        public PaymentMethodDialog(decimal grandTotal = 0)
        {
            BuildUi(grandTotal);
            SelectMethod("CASH");
        }

        // ── UI construction ───────────────────────────────────────────────────────
        private void BuildUi(decimal grandTotal)
        {
            Text              = "Payment Method";
            StartPosition     = FormStartPosition.CenterParent;
            FormBorderStyle   = FormBorderStyle.FixedDialog;
            MaximizeBox       = false;
            MinimizeBox       = false;
            ClientSize        = new Size(FormWidth, CompactHeight);
            Font              = new Font("Microsoft Sans Serif", 10F);

            // ── Header ────────────────────────────────────────────────────────────
            Controls.Add(new Label
            {
                Text     = "How is this bill being paid?",
                Font     = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 22)
            });

            if (grandTotal > 0)
            {
                Controls.Add(new Label
                {
                    Text      = $"Rs. {grandTotal:N2}",
                    Font      = new Font("Microsoft Sans Serif", 13F, FontStyle.Bold),
                    ForeColor = Color.DodgerBlue,
                    AutoSize  = true,
                    Location  = new Point(FormWidth - 180, 22)
                });
            }

            // ── Payment method toggle buttons ─────────────────────────────────────
            btnCash   = MakeMethodBtn("Cash",   new Point(20,  MethodBtnTop));
            btnCard   = MakeMethodBtn("Card",   new Point(200, MethodBtnTop));
            btnCredit = MakeMethodBtn("Credit", new Point(380, MethodBtnTop));

            btnCash.Click   += (s, e) => SelectMethod("CASH");
            btnCard.Click   += (s, e) => SelectMethod("CARD");
            btnCredit.Click += (s, e) => SelectMethod("CREDIT");

            Controls.AddRange(new Control[] { btnCash, btnCard, btnCredit });

            // ── Credit accounts panel ─────────────────────────────────────────────
            creditPanel = new Panel
            {
                Location    = new Point(20, CreditPanelTop),
                Size        = new Size(FormWidth - 40, CreditPanelH),
                Visible     = false
            };

            creditPanel.Controls.Add(new Label
            {
                Text     = "Select Credit Account",
                Font     = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(0, 0)
            });

            creditPanel.Controls.Add(new Label
            {
                Text     = "Search by name or label:",
                Font     = new Font("Microsoft Sans Serif", 9F),
                ForeColor = Color.DimGray,
                AutoSize = true,
                Location = new Point(0, 24)
            });

            txtSearch = new TextBox
            {
                Font     = new Font("Microsoft Sans Serif", 11F),
                Location = new Point(0, 44),
                Size     = new Size(FormWidth - 40, 28)
            };
            txtSearch.TextChanged += (s, e) => RefreshList(txtSearch.Text);
            creditPanel.Controls.Add(txtSearch);

            lstAccounts = new ListBox
            {
                Font          = new Font("Microsoft Sans Serif", 11F),
                Location      = new Point(0, 80),
                Size          = new Size(FormWidth - 40, 140),
                IntegralHeight = false,
                ScrollAlwaysVisible = true
            };
            creditPanel.Controls.Add(lstAccounts);

            lblNoAccounts = new Label
            {
                Font      = new Font("Microsoft Sans Serif", 10F, FontStyle.Italic),
                ForeColor = Color.Gray,
                Location  = new Point(0, 80),
                Size      = new Size(FormWidth - 40, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Visible   = false
            };
            creditPanel.Controls.Add(lblNoAccounts);

            Controls.Add(creditPanel);

            // ── Bottom action buttons ─────────────────────────────────────────────
            btnConfirm = new Button
            {
                Text                 = "Confirm Payment",
                Font                 = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold),
                BackColor            = Color.DodgerBlue,
                ForeColor            = Color.White,
                Size                 = new Size(185, 44),
                UseVisualStyleBackColor = false
            };
            btnConfirm.Click += BtnConfirm_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Microsoft Sans Serif", 12F),
                Size = new Size(120, 44)
            };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(btnConfirm);
            Controls.Add(btnCancel);
            AcceptButton = btnConfirm;
            CancelButton = btnCancel;
        }

        private Button MakeMethodBtn(string text, Point location)
        {
            var btn = new Button
            {
                Text      = text,
                Font      = new Font("Microsoft Sans Serif", 13F, FontStyle.Bold),
                Size      = new Size(160, 62),
                Location  = location,
                FlatStyle = FlatStyle.Flat,
                Cursor    = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btn.FlatAppearance.BorderSize = 2;
            return btn;
        }

        // ── State machine ─────────────────────────────────────────────────────────
        private void SelectMethod(string method)
        {
            currentMethod = method;

            Color defaultBg    = Color.FromArgb(235, 235, 235);
            Color defaultFg    = Color.DimGray;
            Color defaultBorder = Color.Silver;
            Color activeBg     = Color.DodgerBlue;
            Color activeFg     = Color.White;

            foreach (var b in new[] { btnCash, btnCard, btnCredit })
            {
                b.BackColor = defaultBg;
                b.ForeColor = defaultFg;
                b.FlatAppearance.BorderColor = defaultBorder;
            }

            Button active = method == "CASH" ? btnCash : method == "CARD" ? btnCard : btnCredit;
            active.BackColor = activeBg;
            active.ForeColor = activeFg;
            active.FlatAppearance.BorderColor = activeBg;

            bool showCredit = method == "CREDIT";
            creditPanel.Visible = showCredit;

            if (showCredit && !accountsLoaded)
            {
                accountsLoaded = true;
                LoadAccounts();
            }

            int formH = showCredit ? ExpandedHeight : CompactHeight;
            ClientSize = new Size(FormWidth, formH);

            int btnY = formH - 60;
            btnConfirm.Location = new Point(FormWidth - 185 - 20, btnY);
            btnCancel.Location  = new Point(20, btnY);

            if (showCredit)
                txtSearch.Focus();
        }

        // ── Account loading & search ──────────────────────────────────────────────
        private void LoadAccounts()
        {
            try
            {
                DataTable dt = CreditManager.GetActiveAccountsForSelection();
                allAccounts.Clear();
                foreach (DataRow row in dt.Rows)
                {
                    allAccounts.Add(new AccountEntry
                    {
                        AccountId    = Convert.ToInt32(row["account_id"]),
                        CustomerName = row["customer_name"].ToString(),
                        Label        = row["label"].ToString(),
                        Balance      = Convert.ToDecimal(row["outstanding_balance"])
                    });
                }
                RefreshList("");
            }
            catch (Exception ex)
            {
                ShowNoAccounts("Could not load accounts: " + ex.Message);
            }
        }

        private void RefreshList(string search)
        {
            string q = (search ?? "").Trim().ToLowerInvariant();
            filteredAccounts = string.IsNullOrEmpty(q)
                ? allAccounts.ToList()
                : allAccounts.Where(a => a.SearchKey.Contains(q)).ToList();

            lstAccounts.Items.Clear();

            if (filteredAccounts.Count == 0)
            {
                ShowNoAccounts(allAccounts.Count == 0
                    ? "No credit accounts yet. Add one from the Credit Accounts screen."
                    : "No accounts match your search.");
                return;
            }

            lstAccounts.Visible   = true;
            lblNoAccounts.Visible = false;

            foreach (var a in filteredAccounts)
                lstAccounts.Items.Add(a.DisplayText);

            lstAccounts.SelectedIndex = 0;
        }

        private void ShowNoAccounts(string message)
        {
            lstAccounts.Visible     = false;
            lblNoAccounts.Text      = message;
            lblNoAccounts.Visible   = true;
        }

        // ── Confirm ───────────────────────────────────────────────────────────────
        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            if (currentMethod == "CREDIT")
            {
                if (filteredAccounts.Count == 0 || lstAccounts.SelectedIndex < 0)
                {
                    MessageBox.Show(
                        "Please select a credit account from the list.",
                        "Credit Account Required",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                AccountEntry selected = filteredAccounts[lstAccounts.SelectedIndex];
                SelectedPaymentMethod    = "CREDIT";
                SelectedCreditAccountId  = selected.AccountId;
                SelectedCreditAccountName = selected.DisplayText;
            }
            else
            {
                SelectedPaymentMethod = currentMethod;
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        // ── Account model ─────────────────────────────────────────────────────────
        private class AccountEntry
        {
            public int     AccountId    { get; set; }
            public string  CustomerName { get; set; }
            public string  Label        { get; set; }
            public decimal Balance      { get; set; }

            public string SearchKey =>
                (CustomerName + " " + Label).ToLowerInvariant();

            public string DisplayText
            {
                get
                {
                    string nameLabel = string.IsNullOrWhiteSpace(Label)
                        ? CustomerName
                        : $"{CustomerName}  ({Label})";

                    string balStr = Balance > 0
                        ? $"Rs. {Balance:N2} outstanding"
                        : Balance < 0
                            ? $"Rs. {Math.Abs(Balance):N2} in credit"
                            : "Settled";

                    return $"{nameLabel}   —   {balStr}";
                }
            }
        }

        private void InitializeComponent() { }
    }
}
