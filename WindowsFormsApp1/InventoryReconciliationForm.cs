using System;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class InventoryReconciliationForm : Form
    {
        private readonly DataGridView grid = new DataGridView();
        private readonly Button btnRefresh = new Button();
        private readonly Button btnClose = new Button();

        public InventoryReconciliationForm()
        {
            Text = "Inventory Reconciliation";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1200, 720);
            BuildUi();
            LoadReport();
        }

        private void BuildUi()
        {
            Label note = new Label
            {
                Text = "Read-only report. Do not overwrite inventory from this screen; correct differences through stocktake/manual adjustment.",
                Location = new Point(15, 12),
                AutoSize = true,
                ForeColor = Color.DarkRed
            };
            grid.SetBounds(15, 40, 1150, 585);
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            btnRefresh.Text = "Refresh";
            btnRefresh.SetBounds(940, 640, 105, 38);
            btnRefresh.Click += (s, e) => LoadReport();
            btnClose.Text = "Close";
            btnClose.SetBounds(1060, 640, 105, 38);
            btnClose.Click += (s, e) => Close();
            Controls.AddRange(new Control[] { note, grid, btnRefresh, btnClose });
        }

        private void LoadReport()
        {
            try
            {
                grid.DataSource = InventoryReconciliationManager.BuildReport();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to build reconciliation report: " + ex.Message, "Inventory Reconciliation",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
