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
        public item_scan()
        {
            InitializeComponent();
            txtBarcode.Focus();

        }

        private void item_scan_Load(object sender, EventArgs e)
        {

        }
    }
}
