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
    public partial class item_scan : Form
    {
        private billing billingForm;
        private string connectionString = "server=127.0.0.1;database=db_stc;uid=root;pwd=;";


        public item_scan(billing billingForm)
        {
            InitializeComponent();

            txtBarcode.Focus();
            lblItemName.Visible = false;
            lblPrice.Visible = false;
            txtPrice.Visible = false;

            txtBarcode.TextChanged += txtBarcode_TextChanged;

            this.billingForm = billingForm;

        }

        private void item_scan_Load(object sender, EventArgs e)
        {

        }
        private async void txtBarcode_TextChanged(object sender, EventArgs e)
        {
            if (txtBarcode.Text == "")
            {
                // Show the waiting animation
                lblLoading.Visible = true;
                lblLoading.Text = "Loading...";

                // Simulate a delay to show the animation (for demo purposes)
                await Task.Delay(1000); // Adjust or remove this delay in real application

                // Perform the database query asynchronously
                await Task.Run(() =>
                {
                    string query = "SELECT item_name, retail_price FROM inventory WHERE barcode = @barcode";
                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        MySqlCommand cmd = new MySqlCommand(query, connection);
                        cmd.Parameters.AddWithValue("@barcode", txtBarcode.Text);

                        MySqlDataReader reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            // Update UI with the retrieved data
                            this.Invoke((Action)(() =>
                            {
                                lblItemName.Text = reader["item_name"].ToString();
                                txtPrice.Text = reader["retail_price"].ToString();
                                lblItemName.Visible = true;
                                lblPrice.Visible = true;
                                txtPrice.Visible = true;
                            }));
                        }
                        reader.Close();
                    }
                });

                // Hide the waiting animation
                lblLoading.Visible = false;
            }
        }

        private void clearTexts()
        {
            txtAmount.Text = string.Empty; 
            txtBarcode.Text = string.Empty;
            txtDisEach.Text = string.Empty;
            txtDisWhole.Text = string.Empty;
            txtPrice.Text = string.Empty;
            lblFinalPrice.Text = string.Empty;


            txtBarcode.Focus();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            clearTexts();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                txtDisEach.Text = string.IsNullOrEmpty(txtDisEach.Text) ? "0" : txtDisEach.Text;
                txtDisWhole.Text = string.IsNullOrEmpty(txtDisWhole.Text) ? "0" : txtDisWhole.Text;

                float retailPrice = float.Parse(txtPrice.Text);
                float amount = float.Parse(txtAmount.Text);

                float each_discount = float.Parse(txtDisEach.Text);
                float whole_discount = float.Parse(txtDisWhole.Text);

                float finalPrice = (retailPrice * amount) - (each_discount * amount) - whole_discount;

                lblFinalPrice.Text = finalPrice.ToString();



                try
                {
                    connection.Open();
                    string query = "INSERT INTO billing (ítem_name, rate, amount, discounted_price) VALUES (@name, @rate, @amount, @price)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", lblItemName.Text);
                        command.Parameters.AddWithValue("@rate", txtPrice.Text);
                        command.Parameters.AddWithValue("@amount", txtAmount.Text);
                        command.Parameters.AddWithValue("@price", lblFinalPrice.Text);
                        command.ExecuteNonQuery();
                        MessageBox.Show("Successfully Added!", " New Item", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }

                LoadBillingData();
                calculate_Total();
                clearTexts();
                GetRowCount();


            }
        }

        private void LoadBillingData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM billing";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        billingForm.dataGridBilling.DataSource = dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while fetching data: " + ex.Message);
            }
        }

        private void calculate_Total()
        {
            decimal sum = 0;

            foreach (DataGridViewRow row in billingForm.dataGridBilling.Rows)
            {
                // Ensure the row is not the new row (usually the last row in DataGridView for adding new records)
                if (row.IsNewRow) continue;

                if (row.Cells["discounted_price"].Value != null)
                {
                    if (decimal.TryParse(row.Cells["discounted_price"].Value.ToString(), out decimal value))
                    {
                        sum += value;
                    }
                }
            }

            billingForm.lblTotalPrice.Text = sum.ToString();
        }
        private void GetRowCount()
        {
            int rowCount = 0;

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = $"SELECT COUNT(*) FROM billing";

                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    rowCount = Convert.ToInt32(cmd.ExecuteScalar());
                }
            }

            billingForm.lblCount.Text = rowCount.ToString();
        }
    }
}
