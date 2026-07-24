using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public class ReturnHistoryForm : Form
    {
        private readonly DataGridView gridReturns = new DataGridView();
        private readonly DataGridView gridItems = new DataGridView();
        private readonly Button btnRefresh = new Button();
        private readonly Button btnClose = new Button();

        public ReturnHistoryForm()
        {
            Text = "Return History";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1150, 720);
            BuildUi();
            LoadReturns();
        }

        private void BuildUi()
        {
            gridReturns.SetBounds(15, 15, 1100, 360);
            gridReturns.ReadOnly = true;
            gridReturns.AllowUserToAddRows = false;
            gridReturns.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridReturns.MultiSelect = false;
            gridReturns.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridReturns.CellClick += GridReturns_CellClick;

            gridItems.SetBounds(15, 390, 1100, 220);
            gridItems.ReadOnly = true;
            gridItems.AllowUserToAddRows = false;
            gridItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            btnRefresh.Text = "Refresh";
            btnRefresh.SetBounds(890, 625, 105, 38);
            btnRefresh.Click += (s, e) => LoadReturns();
            btnClose.Text = "Close";
            btnClose.SetBounds(1010, 625, 105, 38);
            btnClose.Click += (s, e) => Close();

            Controls.AddRange(new Control[] { gridReturns, gridItems, btnRefresh, btnClose });
        }

        private void LoadReturns()
        {
            try
            {
                gridReturns.DataSource = ReturnManager.GetReturnHistory();
                gridItems.DataSource = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load return history: " + ex.Message, "Return History", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void GridReturns_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            try
            {
                object idValue = gridReturns.Rows[e.RowIndex].Cells["id"].Value;
                if (idValue == null || idValue == DBNull.Value) return;
                gridItems.DataSource = ReturnManager.GetReturnItems(Convert.ToInt64(idValue));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load return items: " + ex.Message, "Return History", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
