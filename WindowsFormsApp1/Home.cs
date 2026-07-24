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
    public partial class Home : Form
    {
        private static readonly Size BaseClientSize = new Size(1106, 729);
        private const float MinimumHomeScale = 0.75F;
        private readonly bool isAdmin;

        public Home() : this(UserSession.IsAdmin)
        {
        }

        public Home(bool isAdmin)
        {
            this.isAdmin = isAdmin;
            InitializeComponent();
            ApplyScreenScale();
        }

        private void ApplyScreenScale()
        {
            Rectangle workingArea = Screen.FromControl(this).WorkingArea;
            Size frameSize = Size - ClientSize;
            int availableWidth = workingArea.Width - frameSize.Width;
            int availableHeight = workingArea.Height - frameSize.Height;

            float widthScale = availableWidth / (float)BaseClientSize.Width;
            float heightScale = availableHeight / (float)BaseClientSize.Height;
            float scale = Math.Min(widthScale, heightScale);

            if (scale >= 1F)
            {
                return;
            }

            scale = Math.Max(MinimumHomeScale, scale);

            SuspendLayout();
            Scale(new SizeF(scale, scale));
            ScaleControlFonts(this, scale);
            ClientSize = new Size(
                (int)Math.Round(BaseClientSize.Width * scale),
                (int)Math.Round(BaseClientSize.Height * scale));
            ResumeLayout(false);
            PerformLayout();
        }

        private void ScaleControlFonts(Control parent, float scale)
        {
            foreach (Control control in parent.Controls)
            {
                control.Font = new Font(
                    control.Font.FontFamily,
                    Math.Max(6F, control.Font.Size * scale),
                    control.Font.Style,
                    control.Font.Unit,
                    control.Font.GdiCharSet,
                    control.Font.GdiVerticalFont);

                if (control.HasChildren)
                {
                    ScaleControlFonts(control, scale);
                }
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            billing bl = new billing();
            bl.Show();

            // Hide the current form
            this.Hide();
        }

        private void Home_Load(object sender, EventArgs e)
        {
            btnSettings.Visible = isAdmin;
            btnPendingBills.Visible = isAdmin;
            btnUpdates.Visible = isAdmin;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            BillHistory bh = new BillHistory();
            bh.Show();
            this.Hide();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            UserSession.Clear();
            Form1 frm = new Form1();
            frm.Show();
            this.Hide();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Inventory_management im = new Inventory_management();
            im.Show();
            this.Hide();
        }

        private void btnSettings_Click(object sender, EventArgs e)
        {
            if (!isAdmin)
            {
                MessageBox.Show("Only administrators can access settings.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AppSettings settings = new AppSettings();
            settings.Show();
            this.Hide();
        }

        private void BtnPendingBills_Click(object sender, EventArgs e)
        {
            if (!UserSession.IsAdmin)
            {
                MessageBox.Show("Only administrators can access pending bills.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PendingBillsInbox inbox = new PendingBillsInbox();
            inbox.Show();
            this.Hide();
        }

        private void BtnCredit_Click(object sender, EventArgs e)
        {
            CreditHome creditHome = new CreditHome();
            creditHome.Show();
            this.Hide();
        }

        private void BtnPackaging_Click(object sender, EventArgs e)
        {
            PackagingLabelWindow packagingWindow = new PackagingLabelWindow();
            packagingWindow.Show();
            this.Hide();
        }

        private void BtnUpdates_Click(object sender, EventArgs e)
        {
            if (!UserSession.IsAdmin)
            {
                MessageBox.Show("Only administrators can access updates.", "Access Denied", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            AdminUpdatesForm updates = new AdminUpdatesForm();
            updates.ShowDialog(this);
        }
    }
}
