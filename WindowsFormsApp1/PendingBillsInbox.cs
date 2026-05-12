using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class PendingBillsInbox : Form
    {
        private const int BaseWidth = 1200;
        private const int BaseHeight = 760;
        private const int GridTop = 85;

        private readonly string connectionString = DatabaseConfig.ConnectionString;
        private readonly DataGridView dataGridPending = new DataGridView();

        private List<string> waitingStatuses = new List<string> { "pending" };
        private List<string> fallbackVisibleStatuses = new List<string>();
        private string cancelledStatus = "cancelled";

        public PendingBillsInbox()
        {
            InitializeUi();
            Load += PendingBillsInbox_Load;
        }

        private void PendingBillsInbox_Load(object sender, EventArgs e)
        {
            DetectStatusConventions();
            LoadPendingBills();
        }

        // ── UI setup (matches BillHistory style exactly) ──────────────────────────
        private void InitializeUi()
        {
            Text = "Pending Bills";
            ClientSize = new Size(BaseWidth, BaseHeight);
            MinimumSize = new Size(1000, 650);
            StartPosition = FormStartPosition.CenterScreen;
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.SizableToolWindow;

            // Home / logo icon (top-left, identical positioning to BillHistory)
            PictureBox pictureBox1 = new PictureBox
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
                pictureBox1.Image = (Image)res.GetObject("pictureBox1.Image");
            }
            catch { }
            pictureBox1.Click += (s, e) => BackToHome();
            Controls.Add(pictureBox1);

            // Title
            Label lblTitle = new Label
            {
                Text = "Pending Bills",
                Font = new Font("Microsoft Sans Serif", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(108, 21)
            };
            Controls.Add(lblTitle);

            // Refresh button (top-right, anchored)
            Button btnRefresh = new Button
            {
                Text = "\u27f3  Refresh",
                BackColor = Color.DodgerBlue,
                ForeColor = Color.White,
                Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold),
                Size = new Size(130, 40),
                Location = new Point(BaseWidth - 150, 20),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };
            btnRefresh.FlatAppearance.BorderSize = 0;
            btnRefresh.Click += (s, e) => LoadPendingBills();
            Controls.Add(btnRefresh);

            // Main grid — same settings as BillHistory's dataGridHistory
            dataGridPending.Location = new Point(12, GridTop);
            dataGridPending.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dataGridPending.Size = new Size(ClientSize.Width - 24, ClientSize.Height - GridTop - 60);
            dataGridPending.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dataGridPending.ReadOnly = true;
            dataGridPending.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dataGridPending.AllowUserToAddRows = false;
            dataGridPending.MultiSelect = false;
            dataGridPending.Font = new Font("Arial", 12);
            dataGridPending.RowTemplate.Height = 35;
            dataGridPending.BorderStyle = BorderStyle.Fixed3D;
            dataGridPending.CellDoubleClick += DataGridPending_CellDoubleClick;
            Controls.Add(dataGridPending);

            // Hint label — bottom-left (mirrors BillHistory)
            Label lblHint = new Label
            {
                Text = "Double-click a bill to view and manage it",
                Font = new Font("Microsoft Sans Serif", 10F, FontStyle.Italic),
                ForeColor = SystemColors.GrayText,
                AutoSize = true,
                Location = new Point(12, ClientSize.Height - 52),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };
            Controls.Add(lblHint);

            // Developer credit — bottom-right (mirrors BillHistory)
            Label lblDeveloper = new Label
            {
                Text = "Developed and Maintained by BlackBox Computers\u2122",
                Font = new Font("Microsoft Sans Serif", 10.2F),
                AutoSize = true,
                Location = new Point(ClientSize.Width - 426, ClientSize.Height - 52),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right
            };
            Controls.Add(lblDeveloper);
        }

        // ── Double-click → open detail dialog ────────────────────────────────────
        private void DataGridPending_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridViewRow row = dataGridPending.Rows[e.RowIndex];
            if (row.Cells["id"]?.Value == null) return;

            int pendingBillId = Convert.ToInt32(row.Cells["id"].Value);
            string sessionId = row.Cells["session_id"]?.Value?.ToString() ?? "";
            string cashierCode = row.Cells["cashier_code"]?.Value?.ToString() ?? "";
            string salespersonCode = row.Cells["salesperson_code"]?.Value?.ToString() ?? "";
            DateTime createdAt = DateTime.Now;
            if (row.Cells["created_at"]?.Value != null && row.Cells["created_at"].Value != DBNull.Value)
            {
                try { createdAt = Convert.ToDateTime(row.Cells["created_at"].Value); } catch { }
            }
            string note = row.Cells["note"]?.Value?.ToString() ?? "";

            using (PendingBillDetailView detailView = new PendingBillDetailView(pendingBillId, sessionId, cashierCode, salespersonCode, createdAt, note, cancelledStatus))
            {
                detailView.ShowDialog(this);

                if (detailView.NavigatedToBilling)
                {
                    // Billing form is now in focus — close the inbox too
                    Close();
                    return;
                }

                if (detailView.BillWasRemoved)
                    LoadPendingBills();
            }
        }

        // ── Data loading ──────────────────────────────────────────────────────────
        private void LoadPendingBills()
        {
            try
            {
                DataTable table = new DataTable();
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string inClause = string.Join(", ", waitingStatuses.Select((_, index) => "@status" + index));
                    string query =
                        "SELECT pb.id, pb.session_id, pb.cashier_code, pb.salesperson_code, pb.created_at, " +
                        "COALESCE(COUNT(pbi.id), 0) AS items_count, pb.note " +
                        "FROM pending_bill pb " +
                        "LEFT JOIN pending_bill_items pbi ON pbi.pending_bill_id = pb.id " +
                        "WHERE (pb.status IS NULL OR TRIM(CAST(pb.status AS CHAR)) = '' OR LOWER(TRIM(CAST(pb.status AS CHAR))) IN (" + inClause + ")) " +
                        "GROUP BY pb.id, pb.session_id, pb.cashier_code, pb.salesperson_code, pb.created_at, pb.note " +
                        "ORDER BY pb.created_at DESC";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        for (int i = 0; i < waitingStatuses.Count; i++)
                            command.Parameters.AddWithValue("@status" + i, waitingStatuses[i]);

                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                            adapter.Fill(table);
                    }
                }

                if (table.Rows.Count == 0 && fallbackVisibleStatuses.Count > 0)
                    table = LoadPendingBillsWithFallbackStatuses();

                dataGridPending.DataSource = table;
                FormatGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load pending bills: " + ex.Message,
                    "Pending Bills", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private DataTable LoadPendingBillsWithFallbackStatuses()
        {
            DataTable table = new DataTable();
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string inClause = string.Join(", ", fallbackVisibleStatuses.Select((_, index) => "@fallbackStatus" + index));
                string query =
                    "SELECT pb.id, pb.session_id, pb.cashier_code, pb.salesperson_code, pb.created_at, " +
                    "COALESCE(COUNT(pbi.id), 0) AS items_count, pb.note " +
                    "FROM pending_bill pb " +
                    "LEFT JOIN pending_bill_items pbi ON pbi.pending_bill_id = pb.id " +
                    "WHERE LOWER(TRIM(CAST(pb.status AS CHAR))) IN (" + inClause + ") " +
                    "GROUP BY pb.id, pb.session_id, pb.cashier_code, pb.salesperson_code, pb.created_at, pb.note " +
                    "ORDER BY pb.created_at DESC";

                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    for (int i = 0; i < fallbackVisibleStatuses.Count; i++)
                        command.Parameters.AddWithValue("@fallbackStatus" + i, fallbackVisibleStatuses[i]);

                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(command))
                        adapter.Fill(table);
                }
            }
            return table;
        }

        // ── Grid formatting (same pattern as BillHistory.FormatGrid) ─────────────
        private void FormatGrid()
        {
            if (dataGridPending.Columns.Count == 0) return;

            // Hide internal id
            var colId = dataGridPending.Columns["id"];
            if (colId != null) colId.Visible = false;

            FormatColumn("session_id", "Session", 140);
            FormatColumn("cashier_code", "Cashier", 180);
            FormatColumn("salesperson_code", "Salesperson", 180);
            FormatColumn("created_at", "Created At", 220, format: "yyyy-MM-dd  hh:mm tt");
            FormatColumn("items_count", "Items", 90, alignment: DataGridViewContentAlignment.MiddleCenter);

            var colNote = dataGridPending.Columns["note"];
            if (colNote != null)
            {
                colNote.HeaderText = "Note";
                colNote.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            foreach (DataGridViewColumn col in dataGridPending.Columns)
                col.SortMode = DataGridViewColumnSortMode.Automatic;
        }

        private void FormatColumn(string name, string header, int width,
            string format = null, DataGridViewContentAlignment? alignment = null)
        {
            var col = dataGridPending.Columns[name];
            if (col == null) return;
            col.HeaderText = header;
            col.Width = width;
            if (format != null) col.DefaultCellStyle.Format = format;
            if (alignment.HasValue) col.DefaultCellStyle.Alignment = alignment.Value;
        }

        // ── Status detection (unchanged from original) ────────────────────────────
        private void DetectStatusConventions()
        {
            try
            {
                HashSet<string> statuses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "SELECT DISTINCT LOWER(TRIM(CAST(status AS CHAR))) AS normalized_status FROM pending_bill WHERE status IS NOT NULL";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string value = reader["normalized_status"]?.ToString();
                            if (!string.IsNullOrWhiteSpace(value))
                                statuses.Add(value);
                        }
                    }
                }

                string[] waitingCandidates = { "pending", "waiting", "awaiting_admin", "awaiting", "new" };
                waitingStatuses = waitingCandidates.Where(statuses.Contains).ToList();
                if (waitingStatuses.Count == 0) waitingStatuses.Add("pending");

                string[] terminalStatuses = { "cancelled", "canceled", "inactive", "completed", "complete", "done", "printed", "closed", "in_review", "claimed", "loaded", "loaded_for_edit" };
                fallbackVisibleStatuses = statuses
                    .Where(s => !terminalStatuses.Contains(s, StringComparer.OrdinalIgnoreCase))
                    .ToList();

                if (statuses.Contains("cancelled"))       cancelledStatus = "cancelled";
                else if (statuses.Contains("canceled"))   cancelledStatus = "canceled";
                else if (statuses.Contains("inactive"))   cancelledStatus = "inactive";
                else                                       cancelledStatus = "cancelled";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to detect pending bill statuses: " + ex.Message,
                    "Pending Bills", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BackToHome()
        {
            Home homeForm = Application.OpenForms.OfType<Home>().FirstOrDefault();
            if (homeForm != null) homeForm.Show();
            Close();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(1358, 721);
            this.Name = "PendingBillsInbox";
            this.ResumeLayout(false);
        }
    }
}
