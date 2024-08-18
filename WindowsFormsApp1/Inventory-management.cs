using Microsoft.ReportingServices.Diagnostics.Internal;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class Inventory_management : Form
    {
        private string connectionString = "server=127.0.0.1;database=db_stc;uid=root;pwd=;";
        public Inventory_management()
        {
            InitializeComponent();
            LoadInventoryData();
            txtSearch.TextChanged += TxtSearch_TextChanged;

            this.dataGridInventory.CellClick += new DataGridViewCellEventHandler(this.dataGridInventory_CellClick);


        }

        private void dataGridInventory_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // Ensure the row index is valid
            {
                DataGridViewRow row = dataGridInventory.Rows[e.RowIndex];

                // Assuming the columns in the DataGridView are named "Column1", "Column2", "Column3", etc.
                txtItemName.Text = row.Cells["item_name"].Value?.ToString() ?? string.Empty;
                txtAmount.Text = row.Cells["amount"].Value?.ToString() ?? string.Empty;
                txtPrice.Text = row.Cells["retail_price"].Value?.ToString() ?? string.Empty;
                txtAddedBy.Text = row.Cells["added_by"].Value?.ToString() ?? string.Empty;
                txtKeywords.Text = row.Cells["keywords"].Value?.ToString() ?? string.Empty;
                txtBarcode.Text = row.Cells["barcode"].Value?.ToString() ?? string.Empty;
                txtCost.Text = row.Cells["cost"].Value?.ToString() ?? string.Empty;

            }
        }

        private void Inventory_management_Load(object sender, EventArgs e)
        {
            
        }
        private void LoadInventoryData()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT * FROM inventory";
                    using (MySqlDataAdapter adapter = new MySqlDataAdapter(query, conn))
                    {
                        DataTable dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        dataGridInventory.DataSource = dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while fetching data: " + ex.Message);
            }
        }
        private void TxtSearch_TextChanged(object sender, EventArgs e)
        {
            string filterExpression = txtSearch.Text.Trim();
            if (string.IsNullOrEmpty(filterExpression))
            {
                // If the search box is empty, show all rows
                (dataGridInventory.DataSource as DataTable).DefaultView.RowFilter = "";
            }
            else
            {
                // Validate input: allow only alphanumeric characters and spaces
                if (!Regex.IsMatch(filterExpression, @"^[a-zA-Z0-9\s]*$"))
                {
                    MessageBox.Show("Invalid input. Please use only letters, numbers, and spaces.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Escape special characters for the filter expression
                filterExpression = filterExpression.Replace("'", "''");

                // Apply the filter based on item_name and keywords columns
                (dataGridInventory.DataSource as DataTable).DefaultView.RowFilter =
                    string.Format("item_name LIKE '%{0}%' OR keywords LIKE '%{0}%' OR barcode LIKE '%{0}%'", filterExpression);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            txtSearch.Text = "";
        }

        private void dataGridInventory_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Home home = new Home();
            home.Show();
            this.Hide();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            string url = "https://www.helakuru.lk/keyboard";

            try
            {
                Process.Start(url);
            }
            catch (Exception ex) {
                MessageBox.Show($"Failed to open Helakuru. Error: {ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "INSERT INTO inventory (item_name, retail_price, amount, added_by, keywords, barcode, cost) VALUES (@name, @price, @amount, @added_by, @keywords, @barcode, @cost)";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@name", txtItemName.Text);
                        command.Parameters.AddWithValue("@price", txtPrice.Text);
                        command.Parameters.AddWithValue("@amount", txtAmount.Text);
                        command.Parameters.AddWithValue("@added_by", txtAddedBy.Text);
                        command.Parameters.AddWithValue("@keywords", txtKeywords.Text);
                        command.Parameters.AddWithValue("@barcode", txtBarcode.Text);
                        command.Parameters.AddWithValue("@cost", txtCost.Text);
                        command.ExecuteNonQuery();
                        MessageBox.Show("Successfully Added!", " New Item", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error: " + ex.Message);
                }
            }

            LoadInventoryData();
            clearTexts();
                
        }

        private void clearTexts()
        {
            txtItemName.Text = string.Empty;
            txtPrice.Text = string.Empty;   
            txtAmount.Text = string.Empty;
            txtAddedBy.Text = string.Empty;
            txtKeywords.Text = string.Empty;
            txtBarcode.Text = string.Empty;
            txtCost.Text = string.Empty;

        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            clearTexts();
        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to update this item?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        string sql = "UPDATE `inventory` SET `item_name`=@item_name,`retail_price`=@price,`amount`=@amount,`added_by`=@added_by,`keywords`=@keywords, `barcode`=@barcode, `cost`=@cost  WHERE id = @id";
                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@item_name", txtItemName.Text);
                        cmd.Parameters.AddWithValue("@amount", txtAmount.Text);
                        cmd.Parameters.AddWithValue("@price", txtPrice.Text);
                        cmd.Parameters.AddWithValue("@added_by", txtAddedBy.Text);
                        cmd.Parameters.AddWithValue("@keywords", txtKeywords.Text);
                        cmd.Parameters.AddWithValue("@barcode", txtBarcode.Text);
                        cmd.Parameters.AddWithValue("@cost", txtCost.Text);

                        // Add more parameters as needed
                        cmd.Parameters.AddWithValue("@id", dataGridInventory.SelectedRows[0].Cells["id"].Value);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Record updated successfully!");

                        // Refresh DataGridView
                        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM inventory", conn);
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        dataGridInventory.DataSource = dt;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            else if(dialogResult == DialogResult.No)
            {

            }
            
            LoadInventoryData();
            clearTexts();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to update this item?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                if (dataGridInventory.SelectedRows.Count > 0)
                {
                    int selectedId = Convert.ToInt32(dataGridInventory.SelectedRows[0].Cells["id"].Value);

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string sql = "DELETE FROM inventory WHERE id = @id";
                        using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", selectedId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LoadInventoryData(); // Reload data to reflect changes
                }
                else
                {
                    MessageBox.Show("Please select a row to delete.", "Delete Entry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (dialogResult == DialogResult.No) 
            {
                
            }

            LoadInventoryData();
            clearTexts();
            
        }
    }
}
