using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.IO;

using System.Drawing.Printing;


namespace WindowsFormsApp1
{
    public partial class billing : Form
    {
        // Connection string to your MySQL database
        private string connectionString = "server=127.0.0.1;database=db_stc;uid=root;pwd=;";
        private bool isBillPaused;
        private readonly ListBox suggestionListBox;
        private readonly System.Windows.Forms.TextBox textBox;

        
        public billing()
        {
            InitializeComponent();

            txtItemName.TextChanged += TextBox_TextChanged;
            listBoxSuggestions.Click += SuggestionListBox_Click;
            listBoxSuggestions.KeyDown += SuggestionListBox_KeyDown;

            this.dataGridBilling.CellClick += new DataGridViewCellEventHandler(this.dataGridView1_CellClick);

            dataGridBilling.Font = new Font("Arial", 14);

            checkForPauses(); 


        }
        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            string query = txtItemName.Text;
            if (string.IsNullOrWhiteSpace(query))
            {
                listBoxSuggestions.Visible = false;
                return;
            }

            List<string> suggestions = GetSuggestions(query);
            listBoxSuggestions.Items.Clear();
            if (suggestions.Count > 0)
            {
                listBoxSuggestions.Items.AddRange(suggestions.ToArray());
                listBoxSuggestions.Visible = true;
            }
            else
            {
                listBoxSuggestions.Visible = false;
            }
        }

        private void SuggestionListBox_Click(object sender, EventArgs e)
        {
            if (listBoxSuggestions.SelectedItem != null)
            {
                txtItemName.Text = listBoxSuggestions.SelectedItem.ToString();
                listBoxSuggestions.Visible = false;
            }

            LoadItemPrice(txtItemName.Text);
        }

        private List<string> GetSuggestions(string query)
        {
            List<string> suggestions = new List<string>();

            using (MySqlConnection conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT DISTINCT item_name FROM inventory WHERE item_name LIKE @query OR REPLACE(item_name, ' ', '') LIKE @query OR keywords LIKE @query";
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@query", "%" + query + "%");

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            suggestions.Add(reader.GetString("item_name"));
                        }
                    }
                }
            }

            return suggestions;
        }

        private void SuggestionListBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && listBoxSuggestions.SelectedItem != null)
            {
                txtItemName.Text = listBoxSuggestions.SelectedItem.ToString();
                listBoxSuggestions.Visible = false;
                e.Handled = true; // Mark the event as handled
                e.SuppressKeyPress = true; // Prevent the "ding" sound
            }

            LoadItemPrice(txtItemName.Text);
            loadItemCost(txtItemName.Text);
        }
        private void LoadItemPrice(string itemName)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT retail_price FROM inventory WHERE item_name = @itemName";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@itemName", itemName);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            txtRetailPrice.Text = result.ToString();
                        }
                        else
                        {
                            txtRetailPrice.Text = "Price: Not available";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while fetching the price: " + ex.Message);
            }
        }

        private void loadItemCost(string itemName)
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "SELECT cost FROM inventory WHERE item_name = @itemName";
                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@itemName", itemName);
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            lblCost.Text = result.ToString();
                        }
                        else
                        {
                            lblCost.Text = "0";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while fetching the price: " + ex.Message);
            }
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

            lblCount.Text = rowCount.ToString();
        }


        private string loadEmployeeCode()
        {
            string selectedEmployee = null;

            using (var empSelectionForm = new emp_selection())
            {

                if (empSelectionForm.ShowDialog() == DialogResult.OK)
                {
                    // Retrieve the selected employee from the emp_selection form
                    selectedEmployee = empSelectionForm.SelectedEmployee;
                }
            }

            return selectedEmployee;
        }


        private void clearTexts()
        {
            txtItemName.Text = string.Empty;
            txtAmount.Text = "1";
            txtDisEach.Text = "0";
            txtDisWhole.Text = "0";
            txtRetailPrice.Text = string.Empty;
            lblFinalPrice.Text = string.Empty;


            txtItemName.Focus();
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
                        dataGridBilling.DataSource = dataTable;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("An error occurred while fetching data: " + ex.Message);
            }

            calculate_Total();
            CalculateGrandTotal();
            GetRowCount();
        }

        private void billing_Load(object sender, EventArgs e)
        {
            LoadBillingData();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void calculate_Total()
        {
            decimal sum = 0;

            foreach (DataGridViewRow row in dataGridBilling.Rows)
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

            lblTotalPrice.Text = sum.ToString();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                txtDisEach.Text = string.IsNullOrEmpty(txtDisEach.Text) ? "0" : txtDisEach.Text;
                txtDisWhole.Text = string.IsNullOrEmpty(txtDisWhole.Text) ? "0" : txtDisWhole.Text;

                float retailPrice = float.Parse(txtRetailPrice.Text);
                float amount = float.Parse(txtAmount.Text);

                float each_discount = float.Parse(txtDisEach.Text);
                float whole_discount = float.Parse(txtDisWhole.Text);

                float finalPrice = (retailPrice * amount) - (each_discount * amount) - whole_discount;

                lblFinalPrice.Text = finalPrice.ToString();



                if (isTheSaleProfitable())
                {
                    try
                    {
                        connection.Open();
                        string query = "INSERT INTO billing (ítem_name, rate, amount, discounted_price) VALUES (@name, @rate, @amount, @price)";
                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.Parameters.AddWithValue("@name", txtItemName.Text);
                            command.Parameters.AddWithValue("@rate", txtRetailPrice.Text);
                            command.Parameters.AddWithValue("@amount", txtAmount.Text);
                            command.Parameters.AddWithValue("@price", lblFinalPrice.Text);
                            command.ExecuteNonQuery();
                            MessageBox.Show("Successfully Added!", " New Item", MessageBoxButtons.OK, MessageBoxIcon.Information);

                            clearTexts();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error: " + ex.Message);
                    }
                }

                else
                {
                    MessageBox.Show("This item cannot be added because there's an error with its price.");
                }

                LoadBillingData();
                calculate_Total();

                GetRowCount();


            }
        }

        public void authenticateInputs()
        {

        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            clearTexts();
        }



        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to checkout this order?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                ReorderBillingTable();
                LoadBillingData();

                //////////////////////////////////////////
                /*string salesperson = Microsoft.VisualBasic.Interaction.InputBox(
                "Enter the salesperson's name:",
                "Salesperson",
                "",
                -1,
                -1);*/

                string emp_code = loadEmployeeCode();

                // Check if the user clicked OK and entered a name
                if (!string.IsNullOrEmpty(emp_code))
                {
                    // Proceed with printing the bill, including the salesperson's name
                    //////////////////////////////////////////


                    string cashierName = emp_code; // Update with the actual cashier's name
                    decimal totalAmount = CalculateGrandTotal(); // Update with the actual total amount
                    decimal discountedAmount = totalAmount - decimal.Parse(lblTotalPrice.Text); // Update w ith the actual discounted amount

                    PrintReceipt(dataGridBilling, cashierName, totalAmount, discountedAmount);
                    ///////////////////////////////////////////
                    ///

                    clearTexts();
                    lblCount.Text = string.Empty;
                    lblTotalPrice.Text = string.Empty;

                    using (MySqlConnection connection = new MySqlConnection(connectionString))
                    {
                        connection.Open();
                        string query = "TRUNCATE TABLE `db_stc`.`billing`";


                        using (MySqlCommand command = new MySqlCommand(query, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }

                    LoadBillingData();

                }
                else
                {
                    MessageBox.Show("Please enter the salesperson's name to proceed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }

            }
            else if (dialogResult == DialogResult.No)
            {

            }



        }

        private void button3_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to cancel this bill?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {

                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    connection.Open();
                    string query = "TRUNCATE TABLE `db_stc`.`billing`";
                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                LoadBillingData();
                clearTexts();
                lblCount.Text = string.Empty;
                lblTotalPrice.Text = string.Empty;
            }
            else if (dialogResult == DialogResult.No)
            {

            }
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete this item?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                if (dataGridBilling.SelectedRows.Count > 0)
                {
                    int selectedId = Convert.ToInt32(dataGridBilling.SelectedRows[0].Cells["id"].Value);

                    using (MySqlConnection conn = new MySqlConnection(connectionString))
                    {
                        conn.Open();
                        string sql = "DELETE FROM billing WHERE id = @id";
                        using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                        {
                            cmd.Parameters.AddWithValue("@id", selectedId);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    LoadBillingData(); // Reload data to reflect changes
                }
                else
                {
                    MessageBox.Show("Please select a row to delete.", "Delete Entry", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            else if (dialogResult == DialogResult.No)
            {

            }

            GetRowCount();
            calculate_Total();
            ReorderBillingTable();
            LoadBillingData();
            clearTexts();

        }

        private void btnUpdate_Click(object sender, EventArgs e)
        {

            DialogResult dialogResult = MessageBox.Show("Are you sure you want to update this item?", "Confirmation", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                txtDisEach.Text = string.IsNullOrEmpty(txtDisEach.Text) ? "0" : txtDisEach.Text;
                txtDisWhole.Text = string.IsNullOrEmpty(txtDisWhole.Text) ? "0" : txtDisWhole.Text;

                int retailPrice = int.Parse(txtRetailPrice.Text);
                float amount = float.Parse(txtAmount.Text);

                float each_discount = int.Parse(txtDisEach.Text);
                float whole_discount = int.Parse(txtDisWhole.Text);

                float finalPrice = (retailPrice * amount) - (each_discount * amount) - whole_discount;

                lblFinalPrice.Text = finalPrice.ToString();


                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    try
                    {
                        conn.Open();
                        string sql = "UPDATE billing SET ítem_name=@item_name, amount=@amount, rate=@rate, discounted_price=@price WHERE id=@id";
                        MySqlCommand cmd = new MySqlCommand(sql, conn);
                        cmd.Parameters.AddWithValue("@item_name", txtItemName.Text);
                        cmd.Parameters.AddWithValue("@amount", txtAmount.Text);
                        cmd.Parameters.AddWithValue("@rate", txtRetailPrice.Text);
                        cmd.Parameters.AddWithValue("@price", lblFinalPrice.Text);
                        // Add more parameters as needed
                        cmd.Parameters.AddWithValue("@id", dataGridBilling.SelectedRows[0].Cells["id"].Value);
                        cmd.ExecuteNonQuery();
                        MessageBox.Show("Record updated successfully!");

                        // Refresh DataGridView
                        MySqlDataAdapter da = new MySqlDataAdapter("SELECT * FROM billing", conn);
                        DataTable dt = new DataTable();
                        da.Fill(dt);
                        dataGridBilling.DataSource = dt;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }

                calculate_Total();
                clearTexts();
            }
            else if (dialogResult == DialogResult.No)
            {

            }


        }

        private void dataGridBilling_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0) // Ensure the row index is valid
            {
                DataGridViewRow row = dataGridBilling.Rows[e.RowIndex];

                // Assuming the columns in the DataGridView are named "Column1", "Column2", "Column3", etc.
                txtItemName.Text = row.Cells["ítem_name"].Value?.ToString() ?? string.Empty;
                txtAmount.Text = row.Cells["amount"].Value?.ToString() ?? string.Empty;
                txtRetailPrice.Text = row.Cells["rate"].Value?.ToString() ?? string.Empty;

                txtDisEach.Text = "0";
                txtDisWhole.Text = "0";
            }
        }

        public void ReorderBillingTable()
        {
            string connectionString = "server=127.0.0.1;database=db_stc;uid=root;pwd=;";

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                using (MySqlCommand command = connection.CreateCommand())
                {
                    MySqlTransaction transaction = connection.BeginTransaction();
                    command.Transaction = transaction;

                    try
                    {
                        //Create a temporary table
                        command.CommandText = @"
                    CREATE TEMPORARY TABLE temp_table AS SELECT ítem_name, rate, amount, discounted_price FROM billing WHERE 1=0;";
                        command.ExecuteNonQuery();

                        //Copy data to the temporary table
                        command.CommandText = @"
                    INSERT INTO temp_table (ítem_name, rate, amount, discounted_price)
                    SELECT item_name, rate, amount, discounted_price
                    FROM billing
                    ORDER BY id;";
                        command.ExecuteNonQuery();

                        //Truncate the original table
                        command.CommandText = "TRUNCATE TABLE billing;";
                        command.ExecuteNonQuery();

                        //Copy data back to the original table
                        command.CommandText = @"
                    INSERT INTO billing (ítem_name, rate, amount, discounted_price)
                    SELECT item_name, rate, amount, discounted_price
                    FROM temp_table;";
                        command.ExecuteNonQuery();

                        //Drop the temporary table
                        command.CommandText = "DROP TEMPORARY TABLE temp_table;";
                        command.ExecuteNonQuery();

                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        //throw new Exception("An error occurred while reordering the billing table.", ex);
                    }
                }
            }
        }

        //////////////////////////////////////////////////////////////////////////PRINTING/////////////////////////////////////////////////////////////////////////

        public void PrintReceipt(DataGridView dataGridView, string cashierName, decimal totalAmount, decimal discountedAmount)
        {
            PrintDocument printDocument = new PrintDocument();
            printDocument.DefaultPageSettings.PaperSize = new System.Drawing.Printing.PaperSize("pprnm", 285, 5000);
            printDocument.PrintPage += (sender, e) =>
            {
                Graphics graphics = e.Graphics;
                Font font = new Font("Arial", 8);
                float fontHeight = font.GetHeight();
                int startX = 10;
                int startY = 10;
                int offsetY = 40;
                int printableWidth = e.MarginBounds.Width;

                // Define the company name and font settings
                string companyName = "Saman Trade Center";
                Font companyFont = new Font("Arial", 18, FontStyle.Bold);

                // Define the position to draw the text
                int textX = startX;
                int textY = startY;

                // Draw the company name on the graphics object
                graphics.DrawString(companyName, companyFont, Brushes.Black, textX, textY);

                // Update the offset
                offsetY = textY + companyFont.Height + 10;

                // Define the address and telephone details
                string address = "No.20, Matale road, Galewela";
                string telephone = "066 22 89 468";
                Font detailsFont = new Font("Arial", 9, FontStyle.Regular);

                // Draw the address on the graphics object
                graphics.DrawString(address, detailsFont, Brushes.Black, textX + 40, offsetY);

                // Update the offset for the telephone number
                offsetY += detailsFont.Height + 5;

                // Draw the telephone number on the graphics object
                graphics.DrawString(telephone, detailsFont, Brushes.Black, textX + 73, offsetY);

                // Update the offset
                offsetY += detailsFont.Height + 10;

                // Print Date and Time
                string date = DateTime.Now.ToShortDateString();
                string time = DateTime.Now.ToShortTimeString();
                graphics.DrawString($"Date: {date} Time: {time}", font, Brushes.Black, startX, startY + offsetY);
                offsetY += (int)fontHeight + 5;

                // Print Cashier Name
                graphics.DrawString($"Salesperson: {cashierName}", font, Brushes.Black, startX, startY + offsetY);
                offsetY += (int)fontHeight + 20;

                // Define column positions
                int idColWidth = 20;
                int itemNameColWidth = 100;
                int rateColWidth = 40;
                int qtyColWidth = 40;
                int priceColWidth = 50;

                int idColPos = startX;
                int itemNameColPos = idColPos + idColWidth + 5;
                int rateColPos = itemNameColPos + itemNameColWidth + 5;
                int qtyColPos = rateColPos + rateColWidth + 5;
                int priceColPos = qtyColPos + qtyColWidth + 5;

                // Print column headers
                graphics.DrawString("ID", font, Brushes.Black, idColPos, startY + offsetY);
                graphics.DrawString("Item Name", font, Brushes.Black, itemNameColPos, startY + offsetY);
                graphics.DrawString("Rate", font, Brushes.Black, rateColPos, startY + offsetY);
                graphics.DrawString("(kg/pcs)", font, Brushes.Black, qtyColPos, startY + offsetY);
                graphics.DrawString("Price", font, Brushes.Black, priceColPos, startY + offsetY);
                offsetY += (int)fontHeight + 5;

                // Print rows
                foreach (DataGridViewRow row in dataGridView.Rows)
                {
                    if (row.IsNewRow) continue;

                    int currentX = startX;
                    int rowHeight = (int)fontHeight + 5;
                    int currentOffsetY = offsetY;

                    // Print ID
                    graphics.DrawString(row.Cells[0].Value.ToString(), font, Brushes.Black, idColPos, startY + currentOffsetY);

                    // Print Item Name with word wrapping
                    string itemName = row.Cells[1].Value.ToString();
                    string[] itemNameLines = SplitText(itemName, graphics, font, itemNameColWidth);
                    foreach (string line in itemNameLines)
                    {
                        graphics.DrawString(line, font, Brushes.Black, itemNameColPos, startY + currentOffsetY);
                        currentOffsetY += (int)fontHeight + 2;
                    }

                    // Adjust row height if item name has multiple lines
                    rowHeight = Math.Max(rowHeight, currentOffsetY - offsetY);

                    // Print Rate
                    graphics.DrawString(row.Cells[2].Value.ToString(), font, Brushes.Black, rateColPos, startY + offsetY);

                    // Print Quantity
                    graphics.DrawString(row.Cells[3].Value.ToString(), font, Brushes.Black, qtyColPos, startY + offsetY);

                    // Print Price
                    graphics.DrawString(row.Cells[4].Value.ToString(), font, Brushes.Black, priceColPos, startY + offsetY);

                    // Move to next row
                    offsetY += rowHeight;
                }

                Font discountFont = new Font("Arial", 10, FontStyle.Bold);

                Font grandTotalFont = new Font("Arial", 10, FontStyle.Regular);

                // Print Total and Discounted Amounts
                offsetY += 20;
                graphics.DrawString($"Total Rs.: {totalAmount:N2}", font, Brushes.Black, startX, startY + offsetY);
                offsetY += (int)fontHeight + 5;
                graphics.DrawString($"Discount Rs. : {discountedAmount:N2}", discountFont, Brushes.Black, startX, startY + offsetY);
                offsetY += (int)fontHeight + 5;
                graphics.DrawString($"Grand Total Rs. : {(totalAmount - discountedAmount):N2}", grandTotalFont, Brushes.Black, startX, startY + offsetY);

                // Add Footnotes
                offsetY += 40; // Add some space before the footnotes
                string returnPolicy = "Returns accepted within 7 days with the receipt";
                string outroRemarks = "Thank you for shopping with us!";
                string softwareCompanyInfo = "POS System provided by: BlackBox Computers";
                string softwareCompanyContact = "070 1371 880";

                Font returnFont = new Font("Arial", 8, FontStyle.Bold);
                Font footnoteFont = new Font("Arial", 8, FontStyle.Regular);
                float footnoteFontHeight = footnoteFont.GetHeight();

                graphics.DrawString(outroRemarks, discountFont, Brushes.Black, startX + 22, startY + offsetY);
                offsetY += (int)footnoteFontHeight + 5;

                graphics.DrawString(returnPolicy, returnFont, Brushes.Black, startX, startY + offsetY);
                offsetY += (int)footnoteFontHeight + 5;
                offsetY += 25;

                graphics.DrawString(softwareCompanyInfo, footnoteFont, Brushes.Black, startX + 10, startY + offsetY);
                offsetY += (int)footnoteFontHeight + 5;
                graphics.DrawString(softwareCompanyContact, footnoteFont, Brushes.Black, startX + 90, startY + offsetY);
            };

            PrintPreviewDialog printPreviewDialog = new PrintPreviewDialog();
            printPreviewDialog.Document = printDocument;
            printPreviewDialog.ShowDialog();
        }

        // Helper method to split text into multiple lines
        private string[] SplitText(string text, Graphics graphics, Font font, int maxWidth)
        {
            List<string> lines = new List<string>();
            string[] words = text.Split(' ');
            StringBuilder currentLine = new StringBuilder();

            foreach (string word in words)
            {
                if (graphics.MeasureString(currentLine + word, font).Width > maxWidth)
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                }
                currentLine.Append(word + " ");
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
            }

            return lines.ToArray();
        }


        public decimal CalculateGrandTotal()
        {
            decimal grand_total = 0;

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    string query = "SELECT rate, amount FROM billing";
                    MySqlCommand cmd = new MySqlCommand(query, connection);

                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            decimal rate = reader.GetDecimal("rate");
                            decimal amount = reader.GetDecimal("amount");
                            grand_total += rate * amount;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("An error occurred: " + ex.Message);
                }
            }

            return grand_total;
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Home home = new Home();
            home.Show();
            this.Hide();
        }

        private void txtItemName_TextChanged(object sender, EventArgs e)
        {

        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            item_scan item = new item_scan(this);
            item.Show();

        }

        private bool isTheSaleProfitable()
        {
            lblCost.Text = string.IsNullOrEmpty(lblCost.Text) ? "0" : lblCost.Text;


            double minimum_price = double.Parse(lblCost.Text) * double.Parse(txtAmount.Text);
            double billed_price = double.Parse(lblFinalPrice.Text);

            if (minimum_price > 0 && billed_price < minimum_price)
            {
                string password = PromptForPassword();
                if (password == "saman123") // Replace "your_password" with the actual password
                {
                    return true; // Allow the sale to proceed
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Incorrect password. Sale cannot proceed.", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                    return false;
                }
            }

            return true; // Sale is profitable, proceed as normal
        }

        private string PromptForPassword()
        {
            string password = null;
            System.Windows.Forms.Form prompt = new System.Windows.Forms.Form()
            {
                Width = 300,
                Height = 200,
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog,
                Text = "Requires an Override Password",
                StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
            };

            System.Windows.Forms.Label textLabel = new System.Windows.Forms.Label() { Left = 20, Top = 20, Text = "Enter password:" };
            System.Windows.Forms.TextBox passwordBox = new System.Windows.Forms.TextBox() { Left = 20, Top = 50, Width = 240, UseSystemPasswordChar = true };
            System.Windows.Forms.Button confirmation = new System.Windows.Forms.Button() { Text = "OK", Left = 90, Width = 100, Top = 80, DialogResult = System.Windows.Forms.DialogResult.OK };
            System.Windows.Forms.Label details = new System.Windows.Forms.Label() { Left = 20, Top = 100, Text = "Cannot proceed without an override password because this sale isn't profitable." };

            confirmation.Click += (sender, e) => { password = passwordBox.Text; prompt.Close(); };

            prompt.Controls.Add(passwordBox);
            prompt.Controls.Add(confirmation);
            prompt.Controls.Add(textLabel);
            prompt.AcceptButton = confirmation;

            return prompt.ShowDialog() == System.Windows.Forms.DialogResult.OK ? password : null;
        }

        private void listBoxSuggestions_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void btnPauseBill_Click(object sender, EventArgs e)
        {

            DialogResult dialogResult = MessageBox.Show("Are you sure you want to make that change?  ", "Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dialogResult == DialogResult.Yes)
            {
                if (btnPauseBill.Text == "Pause this Bill")
                {
                    sendIntoPaused();
                }

                else if (btnPauseBill.Text == "Go to the Previous Bill")
                {                   
                    getThePausedIntoBilling();
                }


            }
        }

        public void checkForPauses()
        {
            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                connection.Open();

                string query = "SELECT * FROM paused_bill";

                using (MySqlDataAdapter adaptor = new MySqlDataAdapter(query, connection))
                {
                    DataTable datatable = new DataTable();
                    adaptor.Fill(datatable);


                    if (datatable.Rows.Count != 0)
                    {
                        DialogResult result = MessageBox.Show("There is another bill paused in the system. Do you want to access it ?", "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (result == DialogResult.Yes)
                        {
                            getThePausedIntoBilling();
                        }

                    }
                }

               
            }
        }

        public void getThePausedIntoBilling()
        {
            CalculateGrandTotal();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "INSERT INTO billing(ítem_name, rate, amount, discounted_price) SELECT ítem_name, rate, amount, discounted_price FROM paused_bill";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    string query1 = "TRUNCATE TABLE `db_stc`.`paused_bill`";

                    using (MySqlCommand command = new MySqlCommand(query1, connection))
                    {
                        command.ExecuteNonQuery();
                    }


                    LoadBillingData();
                    GetRowCount();

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error" + ex.Message);
                }
            }

            btnPauseBill.Text = "Pause this Bill";
            btnPauseBill.BackColor = Color.FromArgb(255, 255, 128, 0);
            btnPauseBill.ForeColor = SystemColors.ControlText;
        }

        public void sendIntoPaused()
        {
            CalculateGrandTotal();

            using (MySqlConnection connection = new MySqlConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    string query = "INSERT INTO paused_bill(ítem_name, rate, amount, discounted_price) SELECT ítem_name, rate, amount, discounted_price FROM billing";

                    using (MySqlCommand command = new MySqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    string query1 = "TRUNCATE TABLE `db_stc`.`billing`";

                    using (MySqlCommand command = new MySqlCommand(query1, connection))
                    {
                        command.ExecuteNonQuery();
                    }


                    LoadBillingData();
                    GetRowCount();

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error" + ex.Message);
                }
            }

            btnPauseBill.Text = "Go to the Previous Bill";
            btnPauseBill.BackColor = Color.Black;
            btnPauseBill.ForeColor = SystemColors.Control;
        }
    }
}
