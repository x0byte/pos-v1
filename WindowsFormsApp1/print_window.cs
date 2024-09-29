using iText.Commons.Datastructures;
using iText.Layout;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApp1
{
    public partial class print_window : Form
    {
        private Bitmap _bitmap;

        public print_window(Bitmap bitmap, decimal grand_total, string cashier_Name)
        {
            InitializeComponent();
            _bitmap = bitmap;

            lblTotalPrice.Text = grand_total.ToString();
            lblSalesperson.Text = cashier_Name;
           

        }

        private void print_window_Load(object sender, EventArgs e)
        {
            
            
        }

        private void btnPrintAgain_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Are you sure you want to print this document?", "Print Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Proceed with printing if the user confirms
                PrintDocument printDocument = new PrintDocument();

                printDocument.PrintPage += (s, ev) =>
                {
                    ev.Graphics.DrawImage(_bitmap, 0, 0);  // Draw the bitmap to the printer graphics
                };

                // Trigger the printing process
                printDocument.Print();
            }
            else
            {
                // If the user selects "No", printing is cancelled
                MessageBox.Show("Printing cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
            } 
                

        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
