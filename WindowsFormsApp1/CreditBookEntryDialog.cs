using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class CreditBookEntryDialog : Form
    {
        public List<CreditManager.CreditTransactionEntry> Entries { get; private set; }

        private readonly TextBox txtEntries = new TextBox();
        private readonly DateTimePicker dtpYear = new DateTimePicker();

        public CreditBookEntryDialog()
        {
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "Add Book Entries";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(680, 540);

            Controls.Add(new Label
            {
                Text = "Paste one book line per row",
                Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 18)
            });

            Controls.Add(new Label
            {
                Text = "Examples: 07/12 - 20,000     |     07/31 - 10,000 Received (Auth by Saman)",
                Font = new Font("Microsoft Sans Serif", 10F),
                ForeColor = Color.DimGray,
                AutoSize = true,
                Location = new Point(20, 48)
            });

            Controls.Add(new Label
            {
                Text = "Year for MM/DD rows",
                Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(20, 82)
            });

            dtpYear.Font = new Font("Microsoft Sans Serif", 11F);
            dtpYear.Format = DateTimePickerFormat.Custom;
            dtpYear.CustomFormat = "yyyy";
            dtpYear.ShowUpDown = true;
            dtpYear.Value = DateTime.Today;
            dtpYear.Location = new Point(180, 78);
            dtpYear.Size = new Size(100, 28);
            Controls.Add(dtpYear);

            txtEntries.Font = new Font("Consolas", 12F);
            txtEntries.Multiline = true;
            txtEntries.ScrollBars = ScrollBars.Vertical;
            txtEntries.AcceptsReturn = true;
            txtEntries.AcceptsTab = false;
            txtEntries.Location = new Point(20, 120);
            txtEntries.Size = new Size(640, 330);
            Controls.Add(txtEntries);

            Button btnSave = new Button
            {
                Text = "Save Entries",
                Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold),
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                Size = new Size(160, 44),
                Location = new Point(500, 475),
                UseVisualStyleBackColor = false
            };
            btnSave.Click += BtnSave_Click;
            Controls.Add(btnSave);

            Button btnCancel = new Button
            {
                Text = "Cancel",
                Font = new Font("Microsoft Sans Serif", 12F),
                Size = new Size(120, 44),
                Location = new Point(20, 475),
                DialogResult = DialogResult.Cancel
            };
            Controls.Add(btnCancel);

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            string error;
            var entries = ParseEntries(txtEntries.Text, dtpYear.Value.Year, out error);
            if (!string.IsNullOrWhiteSpace(error))
            {
                MessageBox.Show(error, "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Entries = entries;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static List<CreditManager.CreditTransactionEntry> ParseEntries(string rawText, int defaultYear, out string error)
        {
            error = null;
            var entries = new List<CreditManager.CreditTransactionEntry>();
            string[] lines = (rawText ?? "").Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var match = Regex.Match(line, @"^(?<date>\d{1,2}/\d{1,2}(?:/\d{2,4})?|\d{4}-\d{1,2}-\d{1,2})\s*[-:]\s*(?<amount>[\d,]+(?:\.\d{1,2})?)\s*(?<note>.*)$");
                if (!match.Success)
                {
                    error = $"Line {i + 1} is not valid. Use: 07/12 - 20,000 or 07/31 - 10,000 Received";
                    return entries;
                }

                DateTime txnDate;
                if (!TryParseDate(match.Groups["date"].Value, defaultYear, out txnDate))
                {
                    error = $"Line {i + 1} has an invalid date.";
                    return entries;
                }

                decimal amount;
                if (!decimal.TryParse(match.Groups["amount"].Value.Replace(",", ""), NumberStyles.Number, CultureInfo.InvariantCulture, out amount) || amount <= 0)
                {
                    error = $"Line {i + 1} has an invalid amount.";
                    return entries;
                }

                string note = match.Groups["note"].Value.Trim();
                bool isReceived = Regex.IsMatch(note, @"\b(received|recieved|paid|payment)\b", RegexOptions.IgnoreCase);
                entries.Add(new CreditManager.CreditTransactionEntry
                {
                    TxnType = isReceived ? "RECEIVED" : "BILL",
                    Direction = isReceived ? "CREDIT" : "DEBIT",
                    Amount = amount,
                    BillCode = null,
                    TransactionDate = txnDate,
                    Description = BuildDescription(isReceived, note)
                });
            }

            if (!entries.Any())
                error = "Please enter at least one book entry.";

            return entries;
        }

        private static bool TryParseDate(string rawDate, int defaultYear, out DateTime date)
        {
            if (DateTime.TryParseExact(rawDate, "yyyy-M-d", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return true;

            string[] fullFormats = { "M/d/yyyy", "M/d/yy" };
            if (DateTime.TryParseExact(rawDate, fullFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
                return true;

            string withYear = rawDate + "/" + defaultYear.ToString(CultureInfo.InvariantCulture);
            return DateTime.TryParseExact(withYear, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
        }

        private static string BuildDescription(bool isReceived, string note)
        {
            string prefix = isReceived ? "Book payment received" : "Manual book credit entry";
            return string.IsNullOrWhiteSpace(note) ? prefix : prefix + " - " + note;
        }

        private void InitializeComponent() { }
    }
}
