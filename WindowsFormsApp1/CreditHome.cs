using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class CreditHome : Form
    {
        private const int BaseWidth = 1200;
        private const int BaseHeight = 760;
        private const int GridTop = 125;

        private readonly DataGridView dataGridAccounts = new DataGridView();
        private readonly TextBox txtSearch = new TextBox();

        public CreditHome()
        {
            BuildUi();
            Load += (s, e) => LoadAccounts();
        }

        private void BuildUi()
        {
            Text = "Credit Accounts";
            ClientSize = new Size(BaseWidth, BaseHeight);
            MinimumSize = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;

            // Back / logo (top-left — matches BillHistory)
            PictureBox logo = new PictureBox
            {
                Size = new Size(63, 62),
                Location = new Point(17, 10),
                SizeMode = PictureBoxSizeMode.StretchImage,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            try
            {
                var res = new System.ComponentModel.ComponentResourceManager(typeof(billing));
                logo.Image = (Image)res.GetObject("pictureBox1.Image");
            }
            catch { }
            logo.Click += (s, e) => BackToHome();
            Controls.Add(logo);

            Controls.Add(new Label
            {
                Text = "Credit Accounts",
                Font = new Font("Microsoft Sans Serif", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(108, 21)
            });

            // Search
            Controls.Add(new Label
            {
                Text = "Search",
                Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(BaseWidth - 835, 82),
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            });

            txtSearch.Font = new Font("Microsoft Sans Serif", 12F);
            txtSearch.Location = new Point(BaseWidth - 755, 78);
            txtSearch.Size = new Size(260, 30);
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            Controls.Add(txtSearch);

            Button btnSearch = new Button
            {
                Text = "Search",
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold),
                Size = new Size(130, 40),
                Location = new Point(BaseWidth - 475, 72),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                UseVisualStyleBackColor = false
            };
            btnSearch.Click += (s, e) => LoadAccounts(txtSearch.Text.Trim());
            Controls.Add(btnSearch);

            Button btnClear = new Button
            {
                Text = "Clear",
                BackColor = Color.IndianRed,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 12F),
                Size = new Size(110, 40),
                Location = new Point(BaseWidth - 325, 72),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                UseVisualStyleBackColor = false
            };
            btnClear.Click += (s, e) => { txtSearch.Text = ""; LoadAccounts(); };
            Controls.Add(btnClear);

            Button btnNew = new Button
            {
                Text = "+ New Account",
                BackColor = Color.SeaGreen,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold),
                Size = new Size(180, 40),
                Location = new Point(BaseWidth - 192, 72),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                UseVisualStyleBackColor = false
            };
            btnNew.Click += BtnNew_Click;
            Controls.Add(btnNew);

            // Main grid
            dataGridAccounts.Location = new Point(12, GridTop);
            dataGridAccounts.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridAccounts.Size = new Size(ClientSize.Width - 24, ClientSize.Height - GridTop - 60);
            dataGridAccounts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridAccounts.ReadOnly = true;
            dataGridAccounts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridAccounts.AllowUserToAddRows = false;
            dataGridAccounts.MultiSelect = false;
            dataGridAccounts.Font = new Font("Arial", 12);
            dataGridAccounts.RowTemplate.Height = 35;
            dataGridAccounts.BorderStyle = BorderStyle.Fixed3D;
            dataGridAccounts.CellDoubleClick += Grid_CellDoubleClick;
            Controls.Add(dataGridAccounts);

            Controls.Add(new Label
            {
                Text = "Double-click an account to view ledger and manage transactions",
                Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Italic),
                ForeColor = SystemColors.GrayText,
                AutoSize = true,
                Location = new Point(12, ClientSize.Height - 52),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            });

            Controls.Add(new Label
            {
                Text = "Developed and Maintained by BlackBox Computers\u2122",
                Font = new Font("Microsoft Sans Serif", 10.2F),
                AutoSize = true,
                Location = new Point(ClientSize.Width - 426, ClientSize.Height - 52),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            });
        }

        private void LoadAccounts(string search = null)
        {
            try
            {
                DataTable dt = CreditManager.GetAccounts(search);
                dataGridAccounts.DataSource = dt;
                FormatGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load accounts: " + ex.Message, "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FormatGrid()
        {
            if (dataGridAccounts.Columns.Count == 0) return;

            SetHidden("account_id");
            SetHidden("is_active");

            FormatCol("customer_name", "Customer Name", 260);
            FormatCol("label", "Account Label", 220);

            var colBal = dataGridAccounts.Columns["outstanding_balance"];
            if (colBal != null)
            {
                colBal.HeaderText = "Outstanding (Rs.)";
                colBal.Width = 200;
                colBal.DefaultCellStyle.Format = "N2";
                colBal.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
                colBal.DefaultCellStyle.Font = new Font("Arial", 12, FontStyle.Bold);
            }

            FormatCol("created_by", "Created By", 160);

            var colDate = dataGridAccounts.Columns["created_at"];
            if (colDate != null)
            {
                colDate.HeaderText = "Created At";
                colDate.DefaultCellStyle.Format = "yyyy-MM-dd  hh:mm tt";
                colDate.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            foreach (DataGridViewColumn col in dataGridAccounts.Columns)
                col.SortMode = DataGridViewColumnSortMode.Automatic;

            foreach (DataGridViewRow row in dataGridAccounts.Rows)
            {
                var cell = row.Cells["outstanding_balance"];
                if (cell?.Value == null || cell.Value == DBNull.Value) continue;
                if (Convert.ToDecimal(cell.Value) > 0)
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235);
            }
        }

        private void SetHidden(string colName)
        {
            var col = dataGridAccounts.Columns[colName];
            if (col != null) col.Visible = false;
        }

        private void FormatCol(string name, string header, int width,
            string format = null, DataGridViewContentAlignment? align = null)
        {
            var col = dataGridAccounts.Columns[name];
            if (col == null) return;
            col.HeaderText = header;
            col.Width = width;
            if (format != null) col.DefaultCellStyle.Format = format;
            if (align.HasValue) col.DefaultCellStyle.Alignment = align.Value;
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var cell = dataGridAccounts.Rows[e.RowIndex].Cells["account_id"];
            if (cell?.Value == null) return;
            int accountId = Convert.ToInt32(cell.Value);
            using (var detail = new CreditAccountDetail(accountId))
                detail.ShowDialog(this);
            LoadAccounts(txtSearch.Text.Trim());
        }

        private void BtnNew_Click(object sender, EventArgs e)
        {
            using (var dlg = new NewCreditAccountDialog())
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    CreditManager.CreateAccount(dlg.CustomerName, dlg.AccountLabel);
                    LoadAccounts(txtSearch.Text.Trim());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to create account: " + ex.Message, "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BackToHome()
        {
            Home home = Application.OpenForms.OfType<Home>().FirstOrDefault();
            if (home != null) home.Show();
            Close();
        }

        private void InitializeComponent()
        {
            SuspendLayout();
            ClientSize = new Size(1568, 1055);
            Name = "CreditHome";
            ResumeLayout(false);
        }
    }
}
