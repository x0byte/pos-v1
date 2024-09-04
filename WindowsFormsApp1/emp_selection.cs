using MySql.Data.MySqlClient;
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
        private string connectionString = "server=127.0.0.1;database=db_stc;uid=root;pwd=;";

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
           
            string query = "SELECT emp_code FROM employee";

            // Create a new connection object
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    // Open the connection
                    connection.Open();

                    // Create a command to execute the query
                    using (MySqlCommand cmd = new MySqlCommand(query, connection))
                    {
                        // Execute the command and fetch data
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Assuming the column contains string data
                                string item = reader["emp_code"].ToString();

                                // Add the item to the ComboBox
                                cmbEmployee.Items.Add(item);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle any errors
                    MessageBox.Show("Error: " + ex.Message);
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
    }
}
