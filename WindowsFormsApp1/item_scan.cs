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


        public item_scan(billing billingForm)
        {
            InitializeComponent();

            txtBarcode.Focus();
            lblItemName.Visible = false;
            lblPrice.Visible = false;
            txtPrice.Visible = false;

            //txtBarcode.TextChanged += txtBarcode_TextChanged;
            this.txtBarcode.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtBarcode_KeyDown);

            this.billingForm = billingForm;

        }

        private void item_scan_Load(object sender, EventArgs e)
        {

        }

        private async void txtBarcode_KeyDown(object sender, KeyEventArgs e)
        {
            if (txtBarcode.Text == "") return;

            // Check if the Enter key is pressed
            if (e.KeyCode == Keys.Enter)
            {

                // Show the waiting animation
                lblLoading.Visible = true;
                lblLoading.Text = "Loading...";

                // Perform cache lookup asynchronously
                await Task.Run(() =>
                {
                    string barcode = txtBarcode.Text.Trim();
                    InventoryItem item = AppCache.Inventory.FirstOrDefault(i =>
                        !string.IsNullOrWhiteSpace(i.Barcode) &&
                        string.Equals(i.Barcode.Trim(), barcode, StringComparison.OrdinalIgnoreCase));

                    if (item != null)
                    {
                        this.Invoke((Action)(() =>
                        {
                            lblItemName.Text = item.ItemName;
                            txtPrice.Text = item.RetailPrice.ToString();
                            lblItemName.Visible = true;
                            lblPrice.Visible = true;
                            txtPrice.Visible = true;
                            txtAmount.Focus();
                        }));
                    }
                });

                // Hide the waiting animation
                lblLoading.Visible = false;

                // Clear the barcode textbox to prevent duplicate processing
                txtBarcode.Clear();
            }
        }

        private void clearTexts()
        {
            txtAmount.Text = string.Empty;
            txtBarcode.Text = string.Empty;
            txtDisEach.Text = "0";
            txtDisWhole.Text = "0";
            txtPrice.Text = string.Empty;
            lblFinalPrice.Text = string.Empty;
            lblItemName.Text = string.Empty;


            txtBarcode.Focus();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            clearTexts();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            txtDisEach.Text = string.IsNullOrEmpty(txtDisEach.Text) ? "0" : txtDisEach.Text;
            txtDisWhole.Text = string.IsNullOrEmpty(txtDisWhole.Text) ? "0" : txtDisWhole.Text;

            decimal retailPrice = decimal.Parse(txtPrice.Text);
            decimal amount = decimal.Parse(txtAmount.Text);

            decimal each_discount = decimal.Parse(txtDisEach.Text);
            decimal whole_discount = decimal.Parse(txtDisWhole.Text);

            decimal finalPrice = (retailPrice * amount) - (each_discount * amount) - whole_discount;

            lblFinalPrice.Text = finalPrice.ToString();

            billingForm.AddBillItem(lblItemName.Text, retailPrice, amount, finalPrice);
            billingForm.RefreshBillingView();
            MessageBox.Show("Successfully Added!", " New Item", MessageBoxButtons.OK, MessageBoxIcon.Information);
            clearTexts();
        }
    }
}
