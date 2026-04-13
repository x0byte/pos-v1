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
        public string SelectedEmployee { get; private set; }


        public emp_selection()
        {
            InitializeComponent();

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
            MessageBox.Show("Please enter the salesmen's name", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void cmbEmployee_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
