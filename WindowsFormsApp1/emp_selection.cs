using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class emp_selection : Form
    {
        private readonly string preferredEmployeeCode;
        public string SelectedEmployee { get; private set; }


        public emp_selection() : this(null)
        {
        }

        public emp_selection(string preferredEmployeeCode)
        {
            InitializeComponent();
            this.preferredEmployeeCode = preferredEmployeeCode;

            LoadComboBoxData();
            cmbEmployee.Focus();

        }

        private void emp_selection_Load(object sender, EventArgs e)
        {

        }
        private void LoadComboBoxData()
        {
            cmbEmployee.Items.Clear();
            foreach (EmployeeItem employee in AppCache.Employees)
            {
                if (!string.IsNullOrWhiteSpace(employee.EmpCode))
                {
                    cmbEmployee.Items.Add(employee.EmpCode);
                }
            }

            if (!string.IsNullOrWhiteSpace(preferredEmployeeCode))
            {
                int preferredIndex = cmbEmployee.Items.IndexOf(preferredEmployeeCode);
                if (preferredIndex >= 0)
                {
                    cmbEmployee.SelectedIndex = preferredIndex;
                }
            }

            if (cmbEmployee.Items.Count == 0)
            {
                btnConfirm.Enabled = false;
                MessageBox.Show("No active employees found. Contact admin.", "Employee List Empty", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else if (cmbEmployee.SelectedIndex < 0)
            {
                cmbEmployee.SelectedIndex = 0;
            }
        }

        private void btnConfirm_Click(object sender, EventArgs e)
        {
            if (cmbEmployee.SelectedItem != null)
            {
                SelectedEmployee = cmbEmployee.SelectedItem.ToString();
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Please select an employee.");
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void cmbEmployee_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
